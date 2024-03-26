namespace BuildingBlocks.Exceptions;

public class CannotCloneNullValueObjectException : Exception
{
    public Type Type { get; }

    public CannotCloneNullValueObjectException( Type type, Exception? innerException = null )
        : base( $"Cannot clone null value object of type {type.Name}.", innerException )
        => Type = type;
}
