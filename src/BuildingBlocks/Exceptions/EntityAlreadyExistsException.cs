namespace BuildingBlocks.Exceptions;

public class EntityAlreadyExistsException< T > : EntityException< T >
{
    private const string DEFAULT_ERROR_MESSAGE = "Entity already exists.";

    private const string ERROR_MESSAGE_WITH_TYPE_AND_ID =
        "'{{Entity}}' where {{PropertyName}} = '{{PropertyValue}}' already exists.";

    public EntityAlreadyExistsException( Exception? innerException = null )
        : base( DEFAULT_ERROR_MESSAGE, innerException ) { }

    public EntityAlreadyExistsException( string propertyName, int propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue.ToString(), innerException ) { }

    public EntityAlreadyExistsException( string propertyName, string propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue, innerException ) { }

    public EntityAlreadyExistsException( string propertyName, Identity propertyValue, Exception? innerException = null )
        : base( ERROR_MESSAGE_WITH_TYPE_AND_ID, propertyName, propertyValue.ToString(), innerException ) { }
}
