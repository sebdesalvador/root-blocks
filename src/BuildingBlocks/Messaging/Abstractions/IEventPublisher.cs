namespace BuildingBlocks.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default );
}
