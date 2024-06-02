namespace RootBlocks.Persistence.EntityFramework.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException()
        : base( "Concurrency exception." ) { }
}
