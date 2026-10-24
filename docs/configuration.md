# Configuration reference

This page lists every registration method, base class you must derive from, and external setting, grouped by package. Follow the links for details.

## Service registrations

| Method | Package / namespace | Registers | Lifetime |
|--------|---------------------|-----------|----------|
| `AddMessaging()` | `RootBlocks.Messaging.MediatR.Extensions` | `IEventPublisher` → `EventPublisher` | Scoped |
| `AddPersistence( connectionString, invariantName )` | `RootBlocks.Persistence.Extensions` | `IConnectionFactory` for any registered ADO.NET provider | Singleton |
| `AddSqlClientPersistence( connectionString )` | `RootBlocks.Persistence.SqlClient.Extensions` | `IConnectionFactory` for SQL Server | Singleton |
| `AddNpgsqlPersistence( connectionString )` | `RootBlocks.Persistence.Npgsql.Extensions` | `IConnectionFactory` for PostgreSQL | Singleton |
| `AddMySqlClientPersistence( connectionString )` | `RootBlocks.Persistence.MySqlClient.Extensions` | `IConnectionFactory` for MySQL | Singleton |
| `AddOraclePersistence( connectionString )` | `RootBlocks.Persistence.Oracle.Extensions` | `IConnectionFactory` for Oracle | Singleton |
| `AddSQLitePersistence( connectionString )` | `RootBlocks.Persistence.SQLite.Extensions` | `IConnectionFactory` for SQLite | Singleton |
| `AddOutboxListener()` | `RootBlocks.Persistence.EntityFramework.Extensions` | `OutboxListener` hosted service (SQL Server only) | Hosted |
| `AddLogContext()` | `RootBlocks.Logging.Serilog.Extensions` | `ILogContext` → Serilog `LogContext` | Singleton |
| `AddDefaultLogging( [applicationName] )` | `RootBlocks.Logging.Serilog.Extensions` | `AddLogContext()` plus a preset Serilog logger | n/a |

| `OpenApiOptions` method | Package / namespace | Adds |
|-------------------------|---------------------|------|
| `AddJsonPatchExamples()` | `RootBlocks.AspNetCore.OpenApi.Extensions` | `JsonPatchExampleTransformer` (operation transformer) |

## Registrations you write yourself

RootBlocks provides abstract base classes rather than registering them for you, because each one needs your concrete `DbContext`:

```csharp
services.AddDbContext< Context >( o => o.UseSqlServer( connectionString ) );
services.AddScoped< IUnitOfWork, UnitOfWork >();         // your subclass of RootBlocks' UnitOfWork
services.AddScoped< IPostRepository, PostRepository >(); // one per aggregate root

// Only with the outbox listener:
services.AddScoped< IDatabaseContextWithOutbox >( sp => sp.GetRequiredService< Context >() );
services.AddOutboxListener();

// MediatR, scanning the assemblies that contain your request and notification handlers:
services.AddMediatR( cfg => cfg.RegisterServicesFromAssembly( typeof( SomeCommand ).Assembly ) );

// Optional: an IExecutionContext to stamp correlation IDs on published events.
services.AddScoped< IExecutionContext, MyExecutionContext >();
```

## Base classes to derive from

| Base class | Required? | Purpose |
|------------|-----------|---------|
| `DatabaseContext` or `DatabaseContextWithOutbox` | Yes, with EF | Your `DbContext`. Call `base.OnModelCreating` after applying configurations. |
| `UnitOfWork` | Yes, with EF | Gives the unit of work your concrete context. |
| `EntityConfiguration<,>` and its siblings | Recommended | Per-entity EF mapping. |
| `DesignTimeDatabaseContextFactory<T>` | For `dotnet ef` | Builds the context from `appsettings*.json` at design time. |

## Settings and environment variables

| Setting | Read by | Notes |
|---------|---------|-------|
| `ConnectionStrings:DefaultConnection` | `DesignTimeDatabaseContextFactory<T>` | Read from `appsettings.json`, then `appsettings.{ASPNETCORE_ENVIRONMENT}.json`, then `appsettings.Local.json`, then environment variables (`ConnectionStrings__DefaultConnection`). At runtime, you pass the connection string to the registration methods yourself. |
| `ASPNETCORE_ENVIRONMENT` | `DesignTimeDatabaseContextFactory<T>` | Selects the environment-specific `appsettings` file. |
| `AzureAnalyticsWorkspaceId`, `AzureAnalyticsAuthenticationId`, `AzureAnalyticsLogName` | `AddDefaultLogging` | Azure Log Analytics sink. |
| `AZURE_FUNCTIONS_ENVIRONMENT` | `AddDefaultLogging` | Becomes the `EnvironmentName` log property. |

## MSBuild properties

| Property | Set by | Why |
|----------|--------|-----|
| `InterceptorsNamespaces` += `Microsoft.AspNetCore.OpenApi.Generated` | `RootBlocks.AspNetCore.OpenApi` (`buildTransitive`) | Prevents CS9137 when `GenerateDocumentationFile` is on. Set it yourself if you reference the project instead of the package. See [OpenAPI](packages/openapi.md#xml-comments-and-the-cs9137-build-error). |

## Database defaults

| Default | Value | Where to change it |
|---------|-------|--------------------|
| Schema | `dbo` | `DatabaseContext( options, defaultSchema )`. Not configurable on `DatabaseContextWithOutbox`. |
| Table names | The CLR type name, such as `Post` | Call `builder.ToTable(...)` after `base.Configure` |
| Outbox table | `Outbox` | Not configurable |
| Concurrency column | `RowVersion` (shadow `byte[]`) | Override `Configure` |
| Soft-delete column | `DeletedOn` (shadow `DateTime?`) | Not configurable |
| Enumeration `Name` max length | 256 (`Key`: 512) | Override `Configure` |
