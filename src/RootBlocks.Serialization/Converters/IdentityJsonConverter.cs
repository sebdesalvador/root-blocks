namespace RootBlocks.Serialization.Converters;

public class IdentityJsonConverter< T > : JsonConverter< T >
    where T : Identity
{
    public override bool CanConvert( Type typeToConvert ) => true;

    public override T Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( !reader.TryGetGuid( out var value ) )
            throw new FormatException( "The JSON value is not in a supported Guid format." );

        return value.ToIdentity< T >();
    }

    public override void Write( Utf8JsonWriter writer, T value, JsonSerializerOptions options )
        => writer.WriteStringValue( value.Value );
}
