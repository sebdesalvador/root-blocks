namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Polls the outbox of <typeparamref name="TContext" /> and publishes the messages that were not published when
/// they were committed, then purges the processed ones older than <see cref="OutboxOptions.Retention" />.
/// Register one per context with <see cref="Extensions.ServiceCollectionExtensions.AddOutbox{TContext}" />.
/// </summary>
/// <remarks>
/// Several hosts may run it against the same table: each message is claimed with a conditional update before it
/// is published, so only one of them publishes it. Delivery is still at least once (a host can crash between
/// publishing and recording it), so event handlers must be idempotent.
/// </remarks>
public class OutboxDispatcher< TContext > : BackgroundService
    where TContext : DbContext
{
    #region Fields

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger< OutboxDispatcher< TContext > > _logger;

    #endregion

    #region Constructors

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxDispatcher< TContext > > logger
    )
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    #endregion

    #region Overrides

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                await DispatchPendingAsync( stoppingToken );
                await PurgeProcessedAsync( stoppingToken );
            }
            catch ( Exception e ) when ( !stoppingToken.IsCancellationRequested )
            {
                _logger.LogError( e, "Dispatching the {Context} outbox failed", typeof( TContext ).FullName );
            }

            try
            {
                await Task.Delay( _options.PollInterval, _timeProvider, stoppingToken );
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                return;
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Publishes up to <see cref="OutboxOptions.BatchSize" /> messages, oldest first, that are not processed,
    /// not locked and under <see cref="OutboxOptions.MaxAttempts" />, claiming each one first.
    /// </summary>
    public async Task DispatchPendingAsync( CancellationToken cancellationToken = default )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService< TContext >();
        var publisher = scope.ServiceProvider.GetRequiredService< OutboxPublisher >();
        var now = UtcNow();
        var maxAttempts = _options.MaxAttempts;
        var candidates = await context.Set< OutboxMessage >()
                                      .Where( m => m.ProcessedOn == null
                                                && m.Attempts < maxAttempts
                                                && ( m.LockedUntil == null || m.LockedUntil < now ) )
                                      .OrderBy( m => m.OccurredOn )
                                      .Select( m => m.Id )
                                      .Take( _options.BatchSize )
                                      .ToListAsync( cancellationToken );

        foreach ( var id in candidates )
        {
            var message = await ClaimAsync( context, id, cancellationToken );

            if ( message is not null )
                await publisher.PublishAsync( context, [ message ], cancellationToken );
        }
    }

    /// <summary>Deletes the messages processed more than <see cref="OutboxOptions.Retention" /> ago.</summary>
    public async Task PurgeProcessedAsync( CancellationToken cancellationToken = default )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService< TContext >();
        var processedBefore = UtcNow() - _options.Retention;
        await context.Set< OutboxMessage >()
                     .Where( m => m.ProcessedOn != null && m.ProcessedOn < processedBefore )
                     .ExecuteDeleteAsync( cancellationToken );
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Locks the message for <see cref="OutboxOptions.LockDuration" /> if nobody else holds it, and returns it;
    /// returns <c>null</c> when another dispatcher, or the committing unit of work, got there first.
    /// </summary>
    private async Task< OutboxMessage? > ClaimAsync( TContext context, Guid id, CancellationToken cancellationToken )
    {
        var claimedAt = UtcNow();
        DateTime? lockedUntil = claimedAt + _options.LockDuration;
        var claimed = await context.Set< OutboxMessage >()
                                   .Where( m => m.Id == id
                                             && m.ProcessedOn == null
                                             && ( m.LockedUntil == null || m.LockedUntil < claimedAt ) )
                                   .ExecuteUpdateAsync( s => s.SetProperty( m => m.LockedUntil, lockedUntil ),
                                                        cancellationToken );

        if ( claimed != 1 )
            return null;

        return await context.Set< OutboxMessage >().SingleAsync( m => m.Id == id, cancellationToken );
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    #endregion
}
