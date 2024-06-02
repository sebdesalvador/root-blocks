namespace RootBlocks.Messaging;

public class OutboxItem
{
    public Guid Id { get; }
    public DomainEvent Event { get; }

    public OutboxItem( DomainEvent @event )
    {
        Id = @event.Id;
        Event = @event;
    }
}
