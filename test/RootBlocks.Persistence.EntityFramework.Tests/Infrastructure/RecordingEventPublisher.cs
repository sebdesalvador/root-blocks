namespace RootBlocks.Persistence.EntityFramework.Tests.Infrastructure;

/// <summary>
/// Stands in for the event handlers: records what was published, and fails or stalls on demand.
/// </summary>
public class RecordingEventPublisher : IEventPublisher
{
    #region Fields

    private readonly ConcurrentQueue< DomainEvent > _published = new();

    #endregion

    #region Properties

    public IReadOnlyCollection< DomainEvent > Published => _published.ToArray();

    /// <summary>When set, every publish throws what it returns.</summary>
    public Func< Exception >? Failure { get; set; }

    /// <summary>Runs before each publish, for instance to stall or to race a dispatcher.</summary>
    public Func< DomainEvent, CancellationToken, Task >? BeforePublish { get; set; }

    #endregion

    #region Interface Implementations

    public async Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default )
    {
        if ( BeforePublish is not null )
            await BeforePublish( domainEvent, cancellationToken );

        if ( Failure is not null )
            throw Failure();

        _published.Enqueue( domainEvent );
    }

    #endregion
}
