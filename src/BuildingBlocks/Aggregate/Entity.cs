namespace BuildingBlocks.Aggregate;

[ SuppressMessage(
    category: "Sonar",
    checkId: "S4035: Seal class 'Entity' or implement 'IEqualityComparer<T>' instead.",
    Justification = "Entities are not meant to be compared by value but by ID."
) ]
public abstract class Entity< T > : IEntity, IDatable, IEquatable< Entity< T > >
    where T : Identity, new()
{
    #region Fields

    private readonly List< DomainEvent > _domainEvents = [ ];

    #endregion

    #region Properties

    public T Id { get; } = new();
    public virtual DateTime CreatedOn { get; set; }
    public virtual DateTime LastModifiedOn { get; set; }
    public IReadOnlyCollection< DomainEvent > DomainEvents => _domainEvents.AsReadOnly();

    #endregion

    #region Interface Implementations

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals( Entity< T > other ) => !ReferenceEquals( other, null ) && Id.Equals( other.Id );

    #endregion

    #region Overrides

    public override bool Equals( object? obj ) => obj is Entity< T > entity && Equals( entity );
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => $"{GetType().Name} [Id={Id.Value}]";

    #endregion

    #region Operators

    public static bool operator ==( Entity< T > a, Entity< T > b ) => a.Equals( b );
    public static bool operator !=( Entity< T > a, Entity< T > b ) => !( a == b );

    #endregion

    #region Public Methods

    protected void AddDomainEvent( DomainEvent @event ) => _domainEvents.Add( @event );

    #endregion
}
