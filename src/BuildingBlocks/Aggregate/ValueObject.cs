namespace BuildingBlocks.Aggregate;

[ SuppressMessage(
    category: "Sonar",
    checkId: "S4035: Seal class 'ValueObject' or implement 'IEqualityComparer<T>' instead",
    Justification = "Identities are not meant to be extended through inheritence."
) ]
public abstract class ValueObject : IEquatable< ValueObject >
{
    #region Interface Implementations

    public bool Equals( ValueObject other )
    {
        if ( ReferenceEquals( this, other ) )
            return true;

        using var thisValues = GetAtomicValues().GetEnumerator();
        using var otherValues = other.GetAtomicValues().GetEnumerator();

        while ( thisValues.MoveNext() && otherValues.MoveNext() )
        {
            if ( ReferenceEquals( thisValues.Current, null ) ^ ReferenceEquals( otherValues.Current, null ) )
                return false;

            if ( thisValues.Current != null && !thisValues.Current.Equals( otherValues.Current ) )
                return false;
        }

        return !thisValues.MoveNext() && !otherValues.MoveNext();
    }

    #endregion

    #region Overrides

    public override bool Equals( object? obj ) => obj is ValueObject valueObject && Equals( valueObject );

    public override int GetHashCode() => GetAtomicValues().Select( x => x != null ? x.GetHashCode() : 0 )
                                                          .Aggregate( ( x, y ) => x ^ y );

    #endregion

    #region Public Methods

    protected abstract IEnumerable< object > GetAtomicValues();

    public ValueObject GetClone() => MemberwiseClone() as ValueObject
                                  ?? throw new CannotCloneNullValueObjectException( GetType() );

    #endregion

    #region Operators

    public static bool operator ==( ValueObject a, ValueObject b ) => a.Equals( b );
    public static bool operator !=( ValueObject a, ValueObject b ) => !( a == b );

    #endregion
}
