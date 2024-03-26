namespace BuildingBlocks.Exceptions;

public class NotAModuleException : Exception
{
    private const string DEFAULT_ERROR_MESSAGE = "The current assembly doesn't appear to be a module.";

    public NotAModuleException( Exception? innerException = null )
        : base( DEFAULT_ERROR_MESSAGE, innerException ) { }
}
