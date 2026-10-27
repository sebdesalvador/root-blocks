namespace RootBlocks.Persistence.EntityFramework.Internal;

/// <summary>
/// Serializes outbox events with System.Text.Json, by their runtime type so that no property of a subclass is
/// lost. <see cref="DomainEvent" /> and most events keep their state behind private setters and constructors,
/// which System.Text.Json ignores: they are used here so that an event reads back with its own
/// <see cref="DomainEvent.Id" /> and <see cref="DomainEvent.OccurredOn" /> rather than new ones.
/// </summary>
internal static class OutboxSerializer
{
    #region Fields

    private static readonly JsonSerializerOptions OPTIONS = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { IncludeNonPublicMembers } }
    };

    #endregion

    #region Public Methods

    public static string Serialize( DomainEvent domainEvent )
        => JsonSerializer.Serialize( domainEvent, domainEvent.GetType(), OPTIONS );

    public static DomainEvent Deserialize( string typeName, string payload )
    {
        var type = ResolveType( typeName );

        if ( !typeof( DomainEvent ).IsAssignableFrom( type ) )
            throw new SerializationException( $"'{typeName}' is not a domain event." );

        return JsonSerializer.Deserialize( payload, type, OPTIONS ) as DomainEvent
            ?? throw new SerializationException( $"Unable to deserialize to '{typeName}'." );
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Finds the type ignoring assembly versions, so that messages written before a deployment are still read
    /// after it.
    /// </summary>
    private static Type ResolveType( string typeName )
        => Type.GetType( typeName,
                         name => AppDomain.CurrentDomain
                                          .GetAssemblies()
                                          .FirstOrDefault( a => a.GetName().Name == name.Name )
                              ?? Assembly.Load( new AssemblyName( name.Name! ) ),
                         null,
                         throwOnError: true )!;

    private static void IncludeNonPublicMembers( JsonTypeInfo typeInfo )
    {
        if ( typeInfo.Kind != JsonTypeInfoKind.Object || !typeof( DomainEvent ).IsAssignableFrom( typeInfo.Type ) )
            return;

        if ( typeInfo.CreateObject is null
          && typeInfo.Type.GetConstructor( BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes ) is { } constructor )
            typeInfo.CreateObject = () => constructor.Invoke( null );

        foreach ( var property in typeInfo.Properties )
        {
            if ( property.Set is not null || property.AttributeProvider is not PropertyInfo propertyInfo )
                continue;

            // A private setter is only visible from the type that declares it.
            var setter = propertyInfo.DeclaringType?
                                     .GetProperty( propertyInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )?
                                     .GetSetMethod( nonPublic: true );

            if ( setter is not null )
                property.Set = ( target, value ) => setter.Invoke( target, [ value ] );
        }
    }

    #endregion
}
