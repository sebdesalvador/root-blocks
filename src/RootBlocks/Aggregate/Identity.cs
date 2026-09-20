namespace RootBlocks.Aggregate;

/// <summary>
/// Base class for all identities.
/// </summary>
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

    #region Public Methods

    /// <summary>
    /// Builds an identity from its string form.
    /// </summary>
    /// <typeparam name="T">The concrete identity type to build.</typeparam>
    /// <param name="value">The candidate value, expected to be a GUID. May be <c>null</c>.</param>
    /// <param name="result">The identity when the value parsed; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> when the value parsed as a GUID; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Route and query binding in minimal APIs looks for a <c>TryParse</c> on the parameter type
    /// itself, which a base class cannot supply because it has to return the concrete type. Declare
    /// the two-line opt-in on each identity and let it delegate here:
    /// <code>
    /// public class BlogId : Identity
    /// {
    ///     public static bool TryParse( string? value, IFormatProvider? provider, out BlogId result )
    ///         => TryCreate( value, out result );
    /// }
    /// </code>
    /// </remarks>
    public static bool TryCreate< T >( string? value, out T result )
        where T : Identity
    {
        if ( Guid.TryParse( value, out var guid ) )
        {
            result = guid.ToIdentity< T >();
            return true;
        }

        result = null!;
        return false;
    }

    #endregion

    #region Overrides

    public override bool Equals( object? obj ) => obj is Identity identity && Equals( identity );
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

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
