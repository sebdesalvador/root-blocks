namespace RootBlocks.Messaging;

[ Obsolete( "Replaced by OutboxMessage in RootBlocks.Persistence.EntityFramework, which stores the event's type and "
          + "payload separately and tracks its publication." ) ]
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
