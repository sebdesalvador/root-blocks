namespace BuildingBlocks.Aggregate.Abstractions;

public interface IEntity
{
    IReadOnlyCollection< DomainEvent > DomainEvents { get; }
    void ClearDomainEvents();
}
