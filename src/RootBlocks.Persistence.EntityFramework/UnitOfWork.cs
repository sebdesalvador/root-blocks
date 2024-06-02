namespace RootBlocks.Persistence.EntityFramework;

public abstract class UnitOfWork : IUnitOfWork
{
    private readonly DatabaseContext _context;
    private readonly IEventPublisher _eventPublisher;
    private IDbContextTransaction? _transaction;
    private List< DomainEvent >? _localOutbox;

    public DatabaseContext DatabaseContext => _context;

    protected UnitOfWork( DatabaseContext context, IEventPublisher eventPublisher )
    {
        _context = context;
        _eventPublisher = eventPublisher;
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
    }

    public virtual async Task CommitTransactionAsync()
    {
        if ( _transaction != null )
        {
            await _transaction.CommitAsync();
            _transaction = null;
        }
    }

    public virtual void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction = null;
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
        _localOutbox = domainEntities
                      .SelectMany( ee => ( ( IEntity )ee.Entity ).DomainEvents )
                      .ToList();
        domainEntities.ForEach( ee => ( ( IEntity )ee.Entity ).ClearDomainEvents() );

        if ( _context is DatabaseContextWithOutbox outbox )
            _localOutbox.ForEach( e => outbox.Add( new OutboxItem( e ) ) );
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
