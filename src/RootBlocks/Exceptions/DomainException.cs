namespace RootBlocks.Exceptions;

public abstract class DomainException : Exception
{
    public Enumeration ExceptionCode { get; }

    protected DomainException( Enumeration exceptionCode, Exception? innerException = null )
        : base( $"{exceptionCode.Name}", innerException )
        => ExceptionCode = exceptionCode;
}
