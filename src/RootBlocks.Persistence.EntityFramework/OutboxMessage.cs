namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// A domain event waiting to be published, stored in the same transaction as the change that raised it. Mapped
/// by <see cref="Extensions.ModelBuilderExtensions.ApplyOutbox" />.
/// </summary>
public class OutboxMessage
{
    #region Properties

    /// <summary>The event's <see cref="DomainEvent.Id" />.</summary>
    public Guid Id { get; private set; }

    /// <summary>The assembly-qualified name of the event's type.</summary>
    public string Type { get; private set; } = null!;

    /// <summary>The event serialized as JSON.</summary>
    public string Payload { get; private set; } = null!;

    public DateTime OccurredOn { get; private set; }

    /// <summary>When the event was published; <c>null</c> until then.</summary>
    public DateTime? ProcessedOn { get; private set; }

    /// <summary>How many times publishing the event failed.</summary>
    public int Attempts { get; private set; }

    /// <summary>The exception of the last failed attempt.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Until when whoever claimed the message may process it undisturbed. After a failure, the time of the
    /// next retry.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// For EF Core. Protected rather than private so that contexts using lazy-loading proxies can materialize it.
    /// </summary>
    protected OutboxMessage() { }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates the message of <paramref name="domainEvent" />, locked until <paramref name="lockedUntil" /> so
    /// that no dispatcher takes it while its unit of work is still publishing it.
    /// </summary>
    public static OutboxMessage For( DomainEvent domainEvent, DateTime lockedUntil )
    {
        ArgumentNullException.ThrowIfNull( domainEvent );

        var type = domainEvent.GetType();
        return new OutboxMessage
        {
            Id = domainEvent.Id,
            Type = type.AssemblyQualifiedName ?? throw new AssemblyQualifiedNameNullException( type ),
            Payload = OutboxSerializer.Serialize( domainEvent ),
            OccurredOn = domainEvent.OccurredOn,
            LockedUntil = lockedUntil
        };
    }

    /// <summary>
    /// Reads the event back, with the same <see cref="DomainEvent.Id" />, dates and properties it was stored with.
    /// </summary>
    public DomainEvent ToEvent() => OutboxSerializer.Deserialize( Type, Payload );

    public void MarkProcessed( DateTime now )
    {
        ProcessedOn = now;
        LockedUntil = null;
    }

    /// <summary>
    /// Records a failed attempt and holds the message back for an exponentially growing delay.
    /// </summary>
    public void MarkFailed( Exception exception, DateTime now, OutboxOptions options )
    {
        ArgumentNullException.ThrowIfNull( exception );
        ArgumentNullException.ThrowIfNull( options );

        Attempts++;
        LastError = exception.ToString();
        var delay = options.FirstRetryDelay * Math.Pow( 2, Math.Min( Attempts - 1, 30 ) );
        LockedUntil = now + ( delay < options.MaxRetryDelay ? delay : options.MaxRetryDelay );
    }

    #endregion
}
