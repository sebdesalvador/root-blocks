namespace RootBlocks.Persistence.EntityFramework.Exceptions;

[ Obsolete( "No longer thrown: an event that cannot be read is recorded as a failed attempt on its OutboxMessage." ) ]
public class UnableToDeserializeDomainEventException : Exception
{
    public Guid OutboxItemId { get; }

    public UnableToDeserializeDomainEventException( Guid outboxItemId )
        : base( $"The event from outbox item ID '{outboxItemId}' could not be interpreted." )
        => OutboxItemId = outboxItemId;
}
