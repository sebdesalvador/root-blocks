namespace BuildingBlocks;

public static class ReflectionHelper
{
    private const BindingFlags DECLARED_ONLY_LOOKUP = BindingFlags.Public
                                                    | BindingFlags.NonPublic
                                                    | BindingFlags.Instance
                                                    | BindingFlags.Static
                                                    | BindingFlags.DeclaredOnly;

    public static MethodInfo GetSetterForPropertyFromPropertyInfo
        => typeof( ReflectionHelper )
          .GetMethods( BindingFlags.Public | BindingFlags.Static )
          .Single( m => m.Name == "GetSetterForProperty"
                     && m.GetParameters().Length == 1
                     && m.GetParameters()[ 0 ].ParameterType == typeof( PropertyInfo ) );

    public static Action< T, TValue > GetSetterForProperty< T, TValue >( Expression< Func< T, TValue > > selector )
        where T : class
    {
        var expression = selector.Body;
        var propertyInfo = expression.NodeType == ExpressionType.MemberAccess
            ? ( PropertyInfo )( ( MemberExpression )expression ).Member
            : null;

        if ( propertyInfo is null )
            throw new InvalidOperationException( $"Property not found on type {typeof( T ).FullName}." );

        var setter = GetSetterForProperty< T, TValue >( propertyInfo );
        return setter;
    }

    public static Action< T, TValue > GetSetterForProperty< T, TValue >( string propertyName )
        where T : class
    {
        // Attempt to find the property in the class definition.
        var propertyInfo = typeof( T ).GetProperty( propertyName, DECLARED_ONLY_LOOKUP );

        if ( propertyInfo == null )
            throw new InvalidOperationException( $"Property {propertyName} not found on type {typeof( T ).FullName}." );

        var setter = GetSetterForProperty< T, TValue >( propertyInfo );
        return setter;
    }

    public static Action< T, TValue > GetSetterForProperty< T, TValue >( PropertyInfo prop )
    {
        var setter = prop.GetSetMethod( nonPublic: true );

        if ( setter is not null )
            return ( obj, value ) => setter.Invoke( obj, [ value ] );

        var backingField = prop.DeclaringType?.GetField( $"<{prop.Name}>k__BackingField", DECLARED_ONLY_LOOKUP );

        if ( backingField is null )
            throw new InvalidOperationException(
                $"Could not find a way to set {prop.DeclaringType?.FullName}.{prop.Name}. Try adding a private setter."
            );

        return ( obj, value ) => backingField.SetValue( obj, value );
    }
}
