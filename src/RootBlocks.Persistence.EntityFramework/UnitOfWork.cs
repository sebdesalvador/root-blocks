namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Registers aggregates with a <see cref="EntityFramework.DatabaseContext" /> and commits them with their
/// domain events.
/// </summary>
/// <remarks>
/// When the context maps the outbox (<see cref="Extensions.ModelBuilderExtensions.ApplyOutbox" />), each domain
/// event is written to it in the same <c>SaveChanges</c> as the change that raised it, then published straight
/// after the commit and marked processed. A failed publish never throws: it is recorded on the message and
/// <see cref="OutboxDispatcher{TContext}" /> retries it. Without the outbox, events are published after the save,
/// and a failure is lost.
/// </remarks>
public abstract class UnitOfWork : IUnitOfWork
{
    private readonly DatabaseContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly OutboxPublisher _outboxPublisher;
    private IDbContextTransaction? _transaction;
    private List< DomainEvent >? _localOutbox;
    private readonly List< OutboxMessage > _pendingMessages = [ ];

    public DatabaseContext DatabaseContext => _context;

    /// <summary>
    /// Creates a unit of work whose outbox, if the context maps one, uses the default
    /// <see cref="OutboxOptions" />. Use the constructor taking an <see cref="EntityFramework.OutboxPublisher" />
    /// to apply the configured ones.
    /// </summary>
    protected UnitOfWork( DatabaseContext context, IEventPublisher eventPublisher )
        : this( context,
                eventPublisher,
                new OutboxPublisher( eventPublisher,
                                     TimeProvider.System,
                                     Microsoft.Extensions.Options.Options.Create( new OutboxOptions() ),
                                     NullLogger< OutboxPublisher >.Instance ) ) { }

    /// <summary>
    /// Creates a unit of work that publishes the context's outbox through <paramref name="outboxPublisher" />,
    /// registered by <see cref="Extensions.ServiceCollectionExtensions.AddOutbox{TContext}" />.
    /// </summary>
    protected UnitOfWork( DatabaseContext context, IEventPublisher eventPublisher, OutboxPublisher outboxPublisher )
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _outboxPublisher = outboxPublisher;
    }

    public virtual void RegisterNew( IAggregateRoot aggregateRoot )
    {
        ArgumentNullException.ThrowIfNull( aggregateRoot );

        if ( IsRegistered( aggregateRoot ) )
            return;

        _context.Add( aggregateRoot );
    }

    public virtual void RegisterClean( IAggregateRoot aggregateRoot )
    {
        ArgumentNullException.ThrowIfNull( aggregateRoot );

        if ( IsRegistered( aggregateRoot ) )
            return;

        _context.Attach( aggregateRoot );
        _context.Entry( aggregateRoot ).State = EntityState.Unchanged;
    }

    public virtual void RegisterDirty( IAggregateRoot aggregateRoot )
    {
        ArgumentNullException.ThrowIfNull( aggregateRoot );

        if ( !IsRegistered( aggregateRoot ) )
            return;

        _context.Update( aggregateRoot );
    }

    public virtual void RegisterDeleted( IAggregateRoot aggregateRoot )
    {
        ArgumentNullException.ThrowIfNull( aggregateRoot );

        if ( !IsRegistered( aggregateRoot ) )
            return;

        _context.Remove( aggregateRoot );
    }

    public virtual void Commit()
    {
        SaveDomainEventsToOutbox();
        var numberOfChanges = _context.ChangeTracker
                                      .Entries()
                                      .Count( x => x.State
                                                  is EntityState.Modified
                                                  or EntityState.Added
                                                  or EntityState.Deleted );
        var affectedRows = _context.SaveChanges();

        if ( numberOfChanges > 0 && affectedRows == 0 )
            throw new ConcurrencyException();

        Task.Run( () => PublishDomainEventsIfNecessaryAsync() ).GetAwaiter().GetResult();
        Task.Run( () => PublishOutboxIfCommittedAsync() ).GetAwaiter().GetResult();
    }

    public virtual async Task CommitAsync( CancellationToken cancellationToken = default )
    {
        SaveDomainEventsToOutbox();
        var numberOfChanges = _context.ChangeTracker
                                      .Entries()
                                      .Count( x => x.State
                                                  is EntityState.Modified
                                                  or EntityState.Added
                                                  or EntityState.Deleted );
        var affectedRows = await _context.SaveChangesAsync( cancellationToken );

        if ( numberOfChanges > 0 && affectedRows == 0 )
            throw new ConcurrencyException();

        await PublishDomainEventsIfNecessaryAsync( cancellationToken );
        await PublishOutboxIfCommittedAsync( cancellationToken );
    }

    public virtual void Rollback()
    {
        var changedEntries = _context.ChangeTracker
                                     .Entries()
                                     .Where( x => x.State != EntityState.Unchanged )
                                     .ToList();
        changedEntries.ForEach( e =>
        {
            switch ( e.State )
            {
                case EntityState.Modified:
                    e.CurrentValues.SetValues( e.OriginalValues );
                    e.State = EntityState.Unchanged;
                    break;
                case EntityState.Added:
                    e.State = EntityState.Detached;
                    break;
                case EntityState.Deleted:
                    e.State = EntityState.Unchanged;
                    break;
            }
        } );
    }

    public virtual void BeginTransaction() => _transaction = _context.Database.BeginTransaction();

    public virtual async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();

    public virtual void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction = null;
        Task.Run( () => PublishOutboxIfCommittedAsync() ).GetAwaiter().GetResult();
    }

    public virtual async Task CommitTransactionAsync()
    {
        if ( _transaction != null )
        {
            await _transaction.CommitAsync();
            _transaction = null;
        }

        await PublishOutboxIfCommittedAsync();
    }

    public virtual void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction = null;
        DiscardPendingMessages();
    }

    private bool IsRegistered( IAggregateRoot aggregateRoot )
    {
        return _context.ChangeTracker
                       .Entries()
                       .Any( ee => ee.Entity is IAggregateRoot trackedAggregateRoot
                                && trackedAggregateRoot.GetType() == aggregateRoot.GetType()
                                && trackedAggregateRoot == aggregateRoot );
    }

    private void SaveDomainEventsToOutbox()
    {
        var domainEntities = _context.ChangeTracker
                                     .Entries()
                                     .Where( ee => ee.Entity is IEntity entity
                                                && entity.DomainEvents.Any() )
                                     .ToList();
        var domainEvents = domainEntities
                          .SelectMany( ee => ( ( IEntity )ee.Entity ).DomainEvents )
                          .ToList();
        domainEntities.ForEach( ee => ( ( IEntity )ee.Entity ).ClearDomainEvents() );

        if ( _context.Model.FindEntityType( typeof( OutboxMessage ) ) is null )
        {
            _localOutbox = domainEvents;
            return;
        }

        var lockedUntil = _outboxPublisher.UtcNow() + _outboxPublisher.Options.LockDuration;
        var messages = domainEvents.Select( e => OutboxMessage.For( e, lockedUntil ) ).ToList();
        _context.AddRange( messages );
        _pendingMessages.AddRange( messages );
    }

    /// <summary>
    /// Publishes the messages saved by the last commits once they are in the database for good: straight away,
    /// or when the unit of work's own transaction commits. A transaction opened on the context directly is not
    /// seen; its messages are left to the dispatcher once their lock runs out.
    /// </summary>
    private async Task PublishOutboxIfCommittedAsync( CancellationToken cancellationToken = default )
    {
        if ( _pendingMessages.Count == 0 || _context.Database.CurrentTransaction is not null )
            return;

        var messages = _pendingMessages.ToList();
        _pendingMessages.Clear();
        await _outboxPublisher.PublishAsync( _context, messages, cancellationToken );
    }

    /// <summary>
    /// Forgets the messages of a rolled back transaction: their rows are gone, so the tracker must not try to
    /// update them.
    /// </summary>
    private void DiscardPendingMessages()
    {
        _pendingMessages.ForEach( m => _context.Entry( m ).State = EntityState.Detached );
        _pendingMessages.Clear();
    }

    private async Task PublishDomainEventsIfNecessaryAsync( CancellationToken cancellationToken = default )
    {
        if ( _localOutbox is not null )
        {
            foreach ( var domainEvent in _localOutbox )
                await _eventPublisher.PublishAsync( domainEvent, cancellationToken );

            _localOutbox = null;
        }
    }
}
