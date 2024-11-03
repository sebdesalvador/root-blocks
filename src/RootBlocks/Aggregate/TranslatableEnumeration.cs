namespace RootBlocks.Aggregate;

/// <summary>
/// Base class for translatable enumerations.
/// </summary>
/// <param name="id">The ID of the enumeration.</param>
/// <param name="key">The key of the enumeration.</param>
/// <param name="name">The name of the enumeration.</param>
public abstract class TranslatableEnumeration( Guid id, string key, string name ) : Enumeration( id, name )
{
    public string Key { get; } = key;
}
