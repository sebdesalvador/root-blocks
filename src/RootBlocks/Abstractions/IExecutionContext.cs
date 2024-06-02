namespace RootBlocks.Abstractions;

public interface IExecutionContext
{
    string Authorization { get; }
    Guid? CausationId { get; }
    Guid CorrelationId { get; }
    IDictionary< string, object > Parameters { get; }

    void Initialize(
        string authorization,
        Guid? causationId,
        Guid correlationId,
        Dictionary< string, object >? parameters = null
    );
}
