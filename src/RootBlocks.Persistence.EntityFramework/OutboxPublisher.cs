namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Publishes outbox messages and records the outcome on each of them. A failure, a timeout included, is logged
/// and kept for a retry, never thrown: the change that raised the event is already committed. Only the caller's
/// own cancellation stops it.
/// </summary>
public class OutboxPublisher
{
    #region Fields

    private readonly IEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger< OutboxPublisher > _logger;
    private readonly OutboxSignal? _signal;

    #endregion

    #region Properties

    public OutboxOptions Options { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a publisher that wakes no dispatcher: what it leaves behind waits for the dispatcher's next poll.
    /// </summary>
    public OutboxPublisher(
        IEventPublisher eventPublisher,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxPublisher > logger
    )
        : this( eventPublisher, timeProvider, options, logger, null ) { }

    /// <summary>
    /// Creates a publisher that tells <paramref name="signal" /> when a message it leaves behind falls due, so
    /// that the dispatcher retries it then.
    /// </summary>
    [ ActivatorUtilitiesConstructor ]
    public OutboxPublisher(
        IEventPublisher eventPublisher,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxPublisher > logger,
        OutboxSignal? signal
    )
    {
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
        _signal = signal;
        Options = options.Value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Publishes <paramref name="messages" /> one by one, marks each one processed or failed, then saves
    /// <paramref name="context" />. If any is left unpublished, the dispatcher is told when it falls due.
    /// </summary>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was cancelled. What was published until then is still recorded.
    /// </exception>
    public async Task PublishAsync(
        DbContext context,
        IReadOnlyCollection< OutboxMessage > messages,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( messages );

        try
        {
            foreach ( var message in messages )
                await PublishAsync( message, cancellationToken );
        }
        finally
        {
            var saved = await SaveAsync( context, messages );
            NotifyLeftBehind( context, messages, saved );
        }
    }

    internal DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    #endregion

    #region Private Methods

    private async Task PublishAsync( OutboxMessage message, CancellationToken cancellationToken )
    {
        try
        {
            await _eventPublisher.PublishAsync( message.ToEvent(), cancellationToken );
            message.MarkProcessed( UtcNow() );
        }
        catch ( Exception e ) when ( !cancellationToken.IsCancellationRequested )
        {
            _logger.LogWarning( e,
                                "Publishing outbox message {MessageId} ({Type}) failed; it will be retried",
                                message.Id,
                                message.Type );
            message.MarkFailed( e, UtcNow(), Options );
        }
    }

    /// <summary>
    /// Saves the outcome whatever the caller's token says. Should that fail, the messages stay locked until
    /// <see cref="OutboxOptions.LockDuration" /> runs out, then a dispatcher publishes them again.
    /// </summary>
    private async Task< bool > SaveAsync( DbContext context, IReadOnlyCollection< OutboxMessage > messages )
    {
        try
        {
            await context.SaveChangesAsync( CancellationToken.None );
            return true;
        }
        catch ( Exception e )
        {
            _logger.LogError( e,
                              "Recording the outcome of {Count} outbox message(s) failed; they will be published again",
                              messages.Count );

            foreach ( var message in messages )
                context.Entry( message ).State = EntityState.Detached;

            return false;
        }
    }

    /// <summary>
    /// Tells the dispatcher when the earliest message left unpublished falls due: its retry time, or the end of
    /// its lock when it was never tried. When the outcome could not be saved, the table still holds the lock
    /// taken before publishing, which runs out at the latest one <see cref="OutboxOptions.LockDuration" /> from now.
    /// </summary>
    private void NotifyLeftBehind( DbContext context, IReadOnlyCollection< OutboxMessage > messages, bool saved )
    {
        if ( _signal is null )
            return;

        var leftBehind = messages.Where( m => m.ProcessedOn is null ).ToList();

        if ( leftBehind.Count == 0 )
            return;

        var now = UtcNow();
        var dueAt = saved ? leftBehind.Min( m => m.LockedUntil ?? now ) : now + Options.LockDuration;
        _signal.Notify( context, dueAt );
    }

    #endregion
}
