namespace BuildingBlocks.Logging.Serilog;

public class LogContext : ILogContext
{
    public IDisposable AddProperty( string name, object? value )
        => global::Serilog.Context.LogContext.PushProperty( name, value );
}
