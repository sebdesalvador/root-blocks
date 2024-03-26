namespace BuildingBlocks.Serialization.Newtonsoft.Json.Converters;

public class IdentityConverter< T > : JsonConverter< T >
    where T : Identity
{
    public override T? ReadJson(
        JsonReader reader,
        Type objectType,
        T? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        ArgumentNullException.ThrowIfNull( reader );
        ArgumentNullException.ThrowIfNull( serializer );

        if ( reader.TokenType == JsonToken.Null )
            return null;

        if ( !Guid.TryParse( reader.Value?.ToString(), out var value ) )
            throw new FormatException( "The JSON value is not in a supported Guid format." );

        var identity = Activator.CreateInstance< T >();
        var setter = ReflectionHelper.GetSetterForProperty< T, Guid >( x => x.Value );
        setter?.Invoke( identity, value );
        return identity;
    }

    public override void WriteJson( JsonWriter writer, T? value, JsonSerializer serializer )
        => writer.WriteValue( value?.Value ?? Guid.Empty );
}
