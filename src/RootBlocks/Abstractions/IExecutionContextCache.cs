namespace RootBlocks.Abstractions;

public interface IExecutionContextCache
{
    IExecutionContext Initialize( string authorization,
        Guid? causationId,
        Guid correlationId,
        Dictionary< string, object >? parameters = null );

    IExecutionContext GetContext( string contextId );
}
