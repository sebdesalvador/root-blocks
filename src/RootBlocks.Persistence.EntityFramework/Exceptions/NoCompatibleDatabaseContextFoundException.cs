namespace RootBlocks.Persistence.EntityFramework.Exceptions;

public class NoCompatibleDatabaseContextFoundException : Exception
{
    public NoCompatibleDatabaseContextFoundException()
        : base( "No compatible DatabaseContext were found." ) { }
}
