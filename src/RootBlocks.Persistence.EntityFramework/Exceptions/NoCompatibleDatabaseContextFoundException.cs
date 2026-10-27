namespace RootBlocks.Persistence.EntityFramework.Exceptions;

[ Obsolete( "No longer thrown." ) ]
public class NoCompatibleDatabaseContextFoundException : Exception
{
    public NoCompatibleDatabaseContextFoundException()
        : base( "No compatible DatabaseContext were found." ) { }
}
