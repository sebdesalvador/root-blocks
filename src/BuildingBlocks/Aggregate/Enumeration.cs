namespace BuildingBlocks.Aggregate;

[ SuppressMessage(
    category: "Sonar",
    checkId: "S4035: Seal class 'Enumeration' or implement 'IEqualityComparer<T>' instead",
    Justification = "Enumerations are not meant to be compared by value but by ID."
) ]
[ SuppressMessage( "ReSharper", "ConvertToPrimaryConstructor" ) ]
public abstract class Enumeration : IEquatable< Enumeration >, IComparable< Enumeration >
{
    #region Properties

    public string Name { get; }
    public Guid Id { get; }

    #endregion

    #region Constructors

    protected Enumeration( Guid id, string name ) => ( Id, Name ) = ( id, name );

    #endregion

    #region Interface Implementations

    public int CompareTo( Enumeration other ) => Id.CompareTo( other.Id );

    public bool Equals( Enumeration other ) => !ReferenceEquals( other, null ) && Id.Equals( other.Id );

    #endregion

    #region Overrides

    public override bool Equals( object? obj ) => obj is Enumeration enumeration && Equals( enumeration );
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => $"{Name} [Id={Id}]";

    #endregion

    #region Public Methods

    public static IEnumerable< T > GetAll< T >()
        where T : Enumeration
    {
        var fields = typeof( T ).GetFields( BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly );
        return fields.Select( f => f.GetValue( null ) ).Cast< T >();
    }

    public static T FromValue< T >( Guid value )
        where T : Enumeration
    {
        var matchingItem = Parse< T, Guid >( value, "value", item => item.Id == value );
        return matchingItem;
    }

    public static T FromDisplayName< T >( string displayName )
        where T : Enumeration
    {
        var matchingItem = Parse< T, string >( displayName, "display name", item => item.Name == displayName );
        return matchingItem;
    }

    #endregion

    #region Operators

    public static bool operator ==( Enumeration a, Enumeration b ) => a.Equals( b );
    public static bool operator !=( Enumeration a, Enumeration b ) => !( a == b );
    public static bool operator <( Enumeration a, Enumeration b ) => a.CompareTo( b ) < 0;
    public static bool operator <=( Enumeration a, Enumeration b ) => a.CompareTo( b ) <= 0;
    public static bool operator >( Enumeration a, Enumeration b ) => a.CompareTo( b ) > 0;
    public static bool operator >=( Enumeration a, Enumeration b ) => a.CompareTo( b ) >= 0;

    #endregion

    #region Private Methods

    private static T Parse< T, TK >( TK value, string description, Func< T, bool > predicate )
        where T : Enumeration
    {
        var matchingItem = GetAll< T >().FirstOrDefault( predicate );

        if ( matchingItem == null )
            throw new InvalidOperationException( $"'{value}' is not a valid {description} in {typeof( T )}" );

        return matchingItem;
    }

    #endregion
}
