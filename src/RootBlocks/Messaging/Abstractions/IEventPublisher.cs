namespace RootBlocks.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default );
}
