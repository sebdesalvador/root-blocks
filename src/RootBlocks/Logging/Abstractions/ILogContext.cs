namespace RootBlocks.Logging.Abstractions;

public interface ILogContext
{
    IDisposable AddProperty( string name, object? value );
}
