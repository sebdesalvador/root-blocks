namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Publishes the messages of the outbox of <typeparamref name="TContext" /> that were not published when they
/// were committed, then purges the processed ones older than <see cref="OutboxOptions.Retention" />.
/// Register one per context with <see cref="Extensions.ServiceCollectionExtensions.AddOutbox{TContext}" />.
/// </summary>
/// <remarks>
/// <para>
/// It does not tick at a fixed rate. After each run it sleeps until the earliest message in the table falls due
/// (a retry whose backoff runs out, or a lock that expires), and <see cref="OutboxSignal" /> wakes it earlier
/// when this process leaves a message behind. <see cref="OutboxOptions.PollInterval" /> only caps the sleep, so
/// that messages left behind by other hosts are found too.
/// </para>
/// <para>
/// Several hosts may run it against the same table: each message is claimed with a conditional update before it
/// is published, so only one of them publishes it. Delivery is still at least once (a host can crash between
/// publishing and recording it), so event handlers must be idempotent.
/// </para>
/// </remarks>
public class OutboxDispatcher< TContext > : BackgroundService
    where TContext : DbContext
{
    #region Fields

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger< OutboxDispatcher< TContext > > _logger;
    private readonly OutboxSignal? _signal;
    private readonly object _gate = new();
    private DateTime? _wakeAt;
    private DateTime _sleepingUntil = DateTime.MaxValue;
    private TaskCompletionSource? _wakeUp;

    #endregion

    #region Constructors

    /// <summary>Creates a dispatcher that nothing wakes early: it sleeps until a message falls due.</summary>
    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxDispatcher< TContext > > logger
    )
        : this( scopeFactory, timeProvider, options, logger, null ) { }

    /// <summary>
    /// Creates a dispatcher that <paramref name="signal" /> wakes when this process leaves a message behind.
    /// </summary>
    [ ActivatorUtilitiesConstructor ]
    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxDispatcher< TContext > > logger,
        OutboxSignal? signal
    )
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
        _signal = signal;
    }

    #endregion

    #region Overrides

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        using var subscription = _signal?.Subscribe( typeof( TContext ), WakeAt );

        while ( !stoppingToken.IsCancellationRequested )
        {
            DateTime next;

            try
            {
                var batchWasFull = await DispatchBatchAsync( stoppingToken );
                await PurgeProcessedAsync( stoppingToken );
                next = batchWasFull ? UtcNow() : await NextDueAsync( stoppingToken );
            }
            catch ( Exception e ) when ( !stoppingToken.IsCancellationRequested )
            {
                _logger.LogError( e, "Dispatching the {Context} outbox failed", typeof( TContext ).FullName );
                next = UtcNow() + _options.PollInterval;
            }

            try
            {
                await SleepUntilAsync( next, stoppingToken );
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
    public Task DispatchPendingAsync( CancellationToken cancellationToken = default ) => DispatchBatchAsync( cancellationToken );

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

    /// <summary>Dispatches one batch and says whether it was full, in which case more may be waiting.</summary>
    private async Task< bool > DispatchBatchAsync( CancellationToken cancellationToken )
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

        return candidates.Count == _options.BatchSize;
    }

    /// <summary>
    /// When the earliest message still to publish falls due: when its lock or backoff runs out, or now if it is
    /// not locked. At the latest one <see cref="OutboxOptions.PollInterval" /> from now, so that messages written
    /// by other hosts are not missed.
    /// </summary>
    private async Task< DateTime > NextDueAsync( CancellationToken cancellationToken )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService< TContext >();
        var now = UtcNow();
        var maxAttempts = _options.MaxAttempts;
        var earliest = await context.Set< OutboxMessage >()
                                    .Where( m => m.ProcessedOn == null && m.Attempts < maxAttempts )
                                    .MinAsync( m => ( DateTime? )( m.LockedUntil ?? now ), cancellationToken );
        var latest = now + _options.PollInterval;
        return earliest is { } due && due < latest ? due : latest;
    }

    /// <summary>
    /// Sleeps until <paramref name="until" />, or until <see cref="WakeAt" /> asks for an earlier time, whichever
    /// comes first.
    /// </summary>
    private async Task SleepUntilAsync( DateTime until, CancellationToken cancellationToken )
    {
        while ( true )
        {
            TaskCompletionSource wakeUp;

            lock ( _gate )
            {
                if ( _wakeAt is { } wakeAt && wakeAt < until )
                    until = wakeAt;

                _wakeAt = null;
                _sleepingUntil = until;
                _wakeUp = wakeUp = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
            }

            try
            {
                var delay = until - UtcNow();

                if ( delay <= TimeSpan.Zero )
                    return;

                using var timer = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
                var elapsed = Task.Delay( delay, _timeProvider, timer.Token );

                if ( await Task.WhenAny( elapsed, wakeUp.Task ) == elapsed )
                {
                    await elapsed;
                    return;
                }

                timer.Cancel();
            }
            finally
            {
                lock ( _gate )
                {
                    _sleepingUntil = DateTime.MaxValue;
                    _wakeUp = null;
                }
            }
        }
    }

    /// <summary>
    /// Asks for a run at <paramref name="dueAt" />: wakes the sleep if it would end later, otherwise remembers it
    /// for the next one.
    /// </summary>
    private void WakeAt( DateTime dueAt )
    {
        lock ( _gate )
        {
            if ( _wakeAt is null || dueAt < _wakeAt )
                _wakeAt = dueAt;

            if ( dueAt < _sleepingUntil )
                _wakeUp?.TrySetResult();
        }
    }

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
