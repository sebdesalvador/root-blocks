namespace BuildingBlocks.Serialization.Exceptions;

public class DeserializationException : Exception
{
    public JsonDocument JsonDocument { get; }

    public DeserializationException( JsonDocument jsonDocument, Exception? innerException = null )
        : base( "Unable to deserialize JSON.", innerException )
        => JsonDocument = jsonDocument;
}
