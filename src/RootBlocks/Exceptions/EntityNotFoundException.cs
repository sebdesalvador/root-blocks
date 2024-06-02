namespace RootBlocks.Exceptions;

public class EntityNotFoundException< T > : EntityException< T >
{
    private const string DEFAULT_ERROR_MESSAGE = "Cannot find entity.";

    private const string ERROR_MESSAGE_WITH_TYPE_AND_ID =
        "Cannot find '{{Entity}}' where {{PropertyName}} = '{{PropertyValue}}'.";

    public EntityNotFoundException( Exception? innerException = null )
        : base( DEFAULT_ERROR_MESSAGE, innerException ) { }

    public EntityNotFoundException( string propertyName, int propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue.ToString(), innerException ) { }

    public EntityNotFoundException( string propertyName, string propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue, innerException ) { }

    public EntityNotFoundException( string propertyName, Identity propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue.ToString(), innerException ) { }
}
