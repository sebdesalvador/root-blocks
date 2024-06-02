namespace RootBlocks.Exceptions;

public abstract class EntityException< T > : Exception
{
    private const string DEFAULT_ERROR_MESSAGE = "Entity exception.";

    public EntityException( string message = DEFAULT_ERROR_MESSAGE, Exception? innerException = null )
        : base( message, innerException ) { }

    public EntityException( string message, string propertyName, int propertyValue, Exception? innerException = null )
        : base(
            message.Replace( "{{Entity}}", typeof( T ).FullName )
                   .Replace( "{{PropertyName}}", propertyName )
                   .Replace( "{{PropertyValue}}", propertyValue.ToString() ),
            innerException
        ) { }

    public EntityException( string message, string propertyName, string propertyValue, Exception? innerException = null )
        : base(
            message.Replace( "{{Entity}}", typeof( T ).FullName )
                   .Replace( "{{PropertyName}}", propertyName )
                   .Replace( "{{PropertyValue}}", propertyValue ),
            innerException
        ) { }

    public EntityException( string message, string propertyName, Identity propertyValue, Exception? innerException = null )
        : base(
            message.Replace( "{{Entity}}", typeof( T ).FullName )
                   .Replace( "{{PropertyName}}", propertyName )
                   .Replace( "{{PropertyValue}}", propertyValue.Value.ToString() ),
            innerException
        ) { }
}
