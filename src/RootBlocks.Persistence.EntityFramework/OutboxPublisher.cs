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

    #endregion

    #region Properties

    public OutboxOptions Options { get; }

    #endregion

    #region Constructors

    public OutboxPublisher(
        IEventPublisher eventPublisher,
        TimeProvider timeProvider,
        IOptions< OutboxOptions > options,
        ILogger< OutboxPublisher > logger
    )
    {
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
        Options = options.Value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Publishes <paramref name="messages" /> one by one, marks each one processed or failed, then saves
    /// <paramref name="context" />.
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
            await SaveAsync( context, messages );
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
    private async Task SaveAsync( DbContext context, IReadOnlyCollection< OutboxMessage > messages )
    {
        try
        {
            await context.SaveChangesAsync( CancellationToken.None );
        }
        catch ( Exception e )
        {
            _logger.LogError( e,
                              "Recording the outcome of {Count} outbox message(s) failed; they will be published again",
                              messages.Count );

            foreach ( var message in messages )
                context.Entry( message ).State = EntityState.Detached;
        }
    }

    #endregion
}
