namespace BuildingBlocks.Aggregate;

[ SuppressMessage(
    category: "Sonar",
    checkId: "S4035: Seal class 'Identity' or implement 'IEqualityComparer<T>' instead",
    Justification = "Identities are not meant to be extended through inheritence."
) ]
public abstract class Identity : IEquatable< Identity >, IComparable< Identity >
{
    #region Properties

    public Guid Value { get; } = Guid.NewGuid();

    #endregion

    #region Interface Implementations

    public int CompareTo( Identity other ) => Value.CompareTo( other.Value );
    public bool Equals( Identity other ) => !ReferenceEquals( other, null ) && Value.Equals( other.Value );

    #endregion

    #region Overrides

    public override bool Equals( object? obj ) => obj is Identity identity && Equals( identity );
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{GetType().Name} [Id={Value}]";

    #endregion

    #region Operators

    public static bool operator ==( Identity a, Identity b ) => a.Equals( b );
    public static bool operator !=( Identity a, Identity b ) => !( a == b );
    public static bool operator <( Identity a, Identity b ) => a.CompareTo( b ) < 0;
    public static bool operator <=( Identity a, Identity b ) => a.CompareTo( b ) <= 0;
    public static bool operator >( Identity a, Identity b ) => a.CompareTo( b ) > 0;
    public static bool operator >=( Identity a, Identity b ) => a.CompareTo( b ) >= 0;

    #endregion
}
