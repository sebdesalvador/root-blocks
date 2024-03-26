namespace BuildingBlocks.Serialization.Converters;

public class DomainEventConverter< T > : JsonConverter< T >
    where T : DomainEvent
{
    public override T Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        using var jsonDocument = JsonDocument.ParseValue( ref reader );
        var rootElement = jsonDocument.RootElement;
        var assemblyQualifiedTypeName = rootElement.GetProperty( "AssemblyQualifiedTypeName" ).GetString();
        Type? eventType = null;

        if ( !string.IsNullOrWhiteSpace( assemblyQualifiedTypeName ) )
            eventType = Type.GetType( assemblyQualifiedTypeName );

        if ( eventType is null )
        {
            var typeName = rootElement.GetProperty( "TypeName" ).GetString();
            eventType = AppDomain.CurrentDomain
                                 .GetAssemblies()
                                 .SelectMany( a => a.GetTypes() )
                                 .First( t => t.Name == typeName );
        }

        if ( eventType is null )
            throw new InvalidOperationException( "Event type not found." );

        // Create an instance of the event type
        var domainEvent = ( T )Activator.CreateInstance( eventType, true )!;

        // Set properties using reflection
        foreach ( var propertyInfo in eventType.GetProperties() )
        {
            if ( !rootElement.TryGetProperty( propertyInfo.Name, out var valueElement ) )
                throw new DeserializationException( jsonDocument );

            // Get the method info for the GetSetterForProperty method that takes a PropertyInfo as argument
            var getSetterForPropertyMethodInfo = ReflectionHelper.GetSetterForPropertyFromPropertyInfo;

            // Make the method generic with types obtained from PropertyInfo
            var getSetterForPropertyGenericMethod = getSetterForPropertyMethodInfo.MakeGenericMethod(
                propertyInfo.DeclaringType ?? throw new InvalidOperationException( "Declaring type not found." ),
                propertyInfo.PropertyType
            );

            // Invoke the method to get the setter action
            // Passing the property name as argument
            var actionDelegate = getSetterForPropertyGenericMethod.Invoke( null, [ propertyInfo ] )
                              ?? throw new InvalidOperationException( "Setter not found." );

            // Cast the delegate to the appropriate type and use it
            // Since we do not know the property type at compile time, we use dynamic
            var setterDelegate = ( Delegate )actionDelegate;

            var value = JsonSerializer.Deserialize( valueElement.GetRawText(), propertyInfo.PropertyType, options );
            setterDelegate.DynamicInvoke( domainEvent, value );
        }

        return domainEvent;
    }

    public override void Write( Utf8JsonWriter writer, T value, JsonSerializerOptions options )
    {
        writer.WriteStartObject();

        foreach ( var prop in value.GetType().GetProperties() )
        {
            var propValue = prop.GetValue( value );
            writer.WritePropertyName( prop.Name );
            JsonSerializer.Serialize( writer, propValue, prop.PropertyType, options );
        }

        writer.WriteEndObject();
    }
}
