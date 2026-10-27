namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Settings of the transactional outbox, bound from the <c>Outbox</c> configuration section by
/// <see cref="Extensions.ServiceCollectionExtensions.AddOutbox{TContext}" />.
/// </summary>
public class OutboxOptions
{
    #region Constants

    public const string SECTION = "Outbox";

    #endregion

    #region Properties

    /// <summary>How often <see cref="OutboxDispatcher{TContext}" /> looks for messages to publish.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds( 5 );

    /// <summary>
    /// How long a message is left to whoever claimed it, the committing unit of work included, before a
    /// dispatcher may take it over.
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes( 1 );

    /// <summary>How long the first retry waits. Each further failure doubles the wait.</summary>
    public TimeSpan FirstRetryDelay { get; set; } = TimeSpan.FromSeconds( 5 );

    /// <summary>The longest wait between two retries.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes( 15 );

    /// <summary>
    /// After this many failures a message is no longer retried and is left for a human to look at. With the
    /// default delays that is about a day of retries.
    /// </summary>
    public int MaxAttempts { get; set; } = 100;

    /// <summary>How long processed messages are kept before they are purged.</summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays( 7 );

    /// <summary>How many messages a dispatcher picks up per tick.</summary>
    public int BatchSize { get; set; } = 20;

    #endregion
}
