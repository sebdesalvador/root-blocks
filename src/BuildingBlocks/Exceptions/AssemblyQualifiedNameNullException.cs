namespace BuildingBlocks.Exceptions;

public class AssemblyQualifiedNameNullException : Exception
{
    public Type Type { get; }

    public AssemblyQualifiedNameNullException( Type type, Exception? innerException = null )
        : base( $"Assembly qualified name of type {type.Name} is null or empty.", innerException )
        => Type = type;
}
