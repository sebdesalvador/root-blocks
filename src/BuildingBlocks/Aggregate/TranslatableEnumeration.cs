namespace BuildingBlocks.Aggregate;

public abstract class TranslatableEnumeration( Guid id, string key, string name ) : Enumeration( id, name )
{
    public string Key { get; } = key;
}
