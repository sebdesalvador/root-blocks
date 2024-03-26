namespace BuildingBlocks.Aggregate;

public abstract class DomainEvent : INotification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? CausationId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
    public string TypeName { get; private set; }
    public string AssemblyQualifiedTypeName { get; private set; }

    protected DomainEvent()
    {
        TypeName = GetType().Name;
        AssemblyQualifiedTypeName = GetType().AssemblyQualifiedName
                                 ?? typeof( DomainEvent ).AssemblyQualifiedName
                                 ?? throw new AssemblyQualifiedNameNullException( GetType() );
    }

    public void SetCorrelationIds( Guid? causationId, Guid correlationId )
    {
        CausationId = causationId;
        CorrelationId = correlationId;
    }
}
