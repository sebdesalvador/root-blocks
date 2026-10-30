# Logging with Serilog

Package: `RootBlocks.Logging.Serilog` (`net10.0`)

Two things, which you can use separately:

- [`AddLogContext()`](#ilogcontext) implements the core `ILogContext` with Serilog's `LogContext`, so application code can enrich log entries without referencing Serilog.
- [`AddDefaultLogging()`](#adddefaultlogging) is a preset Serilog setup for hosts that have no `appsettings.json` to configure Serilog from, such as Azure Functions.

```bash
dotnet add package RootBlocks.Logging.Serilog
```

## `ILogContext`

`ILogContext` lives in the core package (`RootBlocks.Logging.Abstractions`), so your application layer can depend on it without a reference to Serilog:

```csharp
public interface ILogContext
{
    IDisposable AddProperty( string name, object? value );
}
```

Register the Serilog implementation in the composition root:

```csharp
using RootBlocks.Logging.Serilog.Extensions;

builder.Services.AddLogContext();   // ILogContext → LogContext, singleton
```

Then add properties for the duration of a `using` scope:

```csharp
public sealed class PublishPostCommandHandler( ILogContext logContext, ILogger< PublishPostCommandHandler > logger, ... )
    : IRequestHandler< PublishPostCommand >
{
    public async Task Handle( PublishPostCommand request, CancellationToken cancellationToken )
    {
        using ( logContext.AddProperty( "PostId", request.PostId ) )
        {
            logger.LogInformation( "Publishing post" );   // carries PostId
            ...
        }
    }
}
```

`AddProperty` calls `Serilog.Context.LogContext.PushProperty`. The property flows across `await`s inside the scope, and it only reaches log entries if the logger is enriched from the log context: add `.Enrich.FromLogContext()` to your Serilog configuration, or `"Enrich": [ "FromLogContext" ]` in `appsettings.json`.

## `AddDefaultLogging`

```csharp
services.AddDefaultLogging();                     // application name inferred from the caller's namespace
services.AddDefaultLogging( "Contoso.Billing" );  // or given explicitly
```

It calls `AddLogContext()`, then plugs this Serilog logger into `Microsoft.Extensions.Logging`:

| Setting | Value |
|---------|-------|
| Minimum level | `Warning` |
| Override for the application's namespace | `Information` |
| Overrides for `Azure.Core` and `Azure.Messaging.EventHubs` | `Error` |
| Enrichers | `FromLogContext`, `ApplicationName` (the application name), `EnvironmentName` (from `AZURE_FUNCTIONS_ENVIRONMENT`, or `NoEnvironment`) |
| Sink: Azure Log Analytics | Workspace, key and log name from the `AzureAnalyticsWorkspaceId`, `AzureAnalyticsAuthenticationId` and `AzureAnalyticsLogName` environment variables |
| Sink: rolling file | `..\..\LogFiles\AppLog_.log`, one file a day and on size limit, 3 files kept, shared, flushed every second |

The application name doubles as the namespace that logs at `Information`, so pass your root namespace. Without an argument, the method walks the call stack and takes the namespace of the first caller outside the package. That works when you call it from your own `Program` or `Startup`, but it is reflection on the stack; prefer the explicit overload.

### When to use it

The preset is opinionated. It is built for Azure-hosted apps, in particular Azure Functions on Windows: the file path is relative to the app's folder in App Service's `D:\home\site\wwwroot` layout, and the only remote sink is Log Analytics. There is no console sink. On Linux the backslashes in the file path are not separators, so the log file lands in the working directory under a literal `..\..\LogFiles\AppLog_*.log` name.

For an ASP.NET Core application, configure Serilog from `appsettings.json` instead, and register only `AddLogContext()` if you want `ILogContext`. The [Blogs sample](../sample.md) does this:

```csharp
builder.Host.UseSerilog( ( context, _, configuration ) =>
    configuration.ReadFrom.Configuration( context.Configuration ) );
```

## Environment variables

| Variable | Used for |
|----------|----------|
| `AzureAnalyticsWorkspaceId` | Log Analytics workspace ID. |
| `AzureAnalyticsAuthenticationId` | Log Analytics shared key. Keep it in a secret store, not in source control. |
| `AzureAnalyticsLogName` | The custom log table name. |
| `AZURE_FUNCTIONS_ENVIRONMENT` | The `EnvironmentName` log property. |

All are read only by `AddDefaultLogging`.
