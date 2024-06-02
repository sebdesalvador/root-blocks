namespace RootBlocks.Persistence.EntityFramework.Exceptions;

public class UnableToDeserializeDomainEventException : Exception
{
    public Guid OutboxItemId { get; }

    public UnableToDeserializeDomainEventException( Guid outboxItemId )
        : base( $"The event from outbox item ID '{outboxItemId}' could not be interpreted." )
        => OutboxItemId = outboxItemId;
}
