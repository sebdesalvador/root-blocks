namespace BuildingBlocks.Serialization.Newtonsoft.Json.Converters;

/// <summary>
/// A converter that only converts the standard, primitive Exception fields and traverses inner
/// exceptions up to specified depth.
/// </summary>
public class AzureAnalyticsExceptionJsonConverter : JsonConverter
{
    public override bool CanConvert( Type objectType )
        => typeof( Exception ).IsAssignableFrom( objectType );

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    ) => throw new NotImplementedException();

    public override void WriteJson( JsonWriter writer, object? value, JsonSerializer serializer )
    {
        var exception = ( Exception )value;
        var parsedException = ParseException( 0, 5, exception );
        parsedException.WriteTo( writer );
    }

    private JObject ParseException( int currentDepth, int maxDepth, Exception exception )
    {
        var o = new JObject();
        var includedProperties = new[]
        {
            "Message",
            "StackTrace",
            "Source",
            "Handled"
        };

        foreach ( var property in exception.GetType().GetProperties().Where( p => includedProperties.Contains( p.Name ) ) )
            o.Add( property.Name, JToken.FromObject( property.GetValue( exception ) ) );

        if ( exception.InnerException != null && currentDepth < maxDepth )
            o.Add( "InnerException", ParseException( currentDepth + 1, maxDepth, exception.InnerException ) );

        return o;
    }
}
