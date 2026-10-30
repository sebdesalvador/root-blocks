# Connection factory and database drivers

Packages: `RootBlocks.Persistence` (`netstandard2.1`) and one driver package per database.

The read side of CQRS rarely needs an ORM: a query wants a connection, some SQL and a DTO. `RootBlocks.Persistence` gives you an `IConnectionFactory` that hands out open-ready `DbConnection`s for one connection string, so your query classes can use raw ADO.NET or [Dapper](https://github.com/DapperLib/Dapper) without knowing which database they talk to. The driver packages register the ADO.NET provider and the factory in one call.

```bash
dotnet add package RootBlocks.Persistence.SqlClient   # pulls in RootBlocks.Persistence
```

## Drivers

| Package | Target | Registration | ADO.NET provider | Invariant name |
|---------|--------|--------------|------------------|----------------|
| `RootBlocks.Persistence.SqlClient` | `netstandard2.1` | `AddSqlClientPersistence( connectionString )` | [`Microsoft.Data.SqlClient`](https://www.nuget.org/packages/Microsoft.Data.SqlClient) | `Microsoft.Data.SqlClient` |
| `RootBlocks.Persistence.Npgsql` | `net10.0` | `AddNpgsqlPersistence( connectionString )` | [`Npgsql`](https://www.nuget.org/packages/Npgsql) | `Npgsql` |
| `RootBlocks.Persistence.MySqlClient` | `netstandard2.1` | `AddMySqlClientPersistence( connectionString )` | [`MySql.Data`](https://www.nuget.org/packages/MySql.Data) | `MySql.Data.MySqlClient` |
| `RootBlocks.Persistence.Oracle` | `netstandard2.1` | `AddOraclePersistence( connectionString )` | [`CYQ.Data.Oracle`](https://www.nuget.org/packages/CYQ.Data.Oracle) (exposes `Oracle.ManagedDataAccess.Client`) | `Oracle.ManagedDataAccess.Client` |
| `RootBlocks.Persistence.SQLite` | `netstandard2.1` | `AddSQLitePersistence( connectionString )` | [`System.Data.SQLite`](https://www.nuget.org/packages/System.Data.SQLite) | `System.Data.SQLite` |

Each method lives in the driver's `Extensions` namespace (for example `RootBlocks.Persistence.SqlClient.Extensions`). It registers the provider's `DbProviderFactory` under its invariant name with `DbProviderFactories.RegisterFactory`, then calls `AddPersistence` with that name. The provider package comes in as a dependency, so you do not need to reference it yourself.

## Registering

```csharp
using RootBlocks.Persistence.SqlClient.Extensions;

var connectionString = builder.Configuration.GetConnectionString( "DefaultConnection" )!;
builder.Services.AddSqlClientPersistence( connectionString );
```

`IConnectionFactory` is registered as a **singleton**: it holds only the connection string and the provider factory, and every call to `CreateConnection()` returns a new connection. Connection pooling is left to the ADO.NET provider as usual.

### Any other ADO.NET provider

`AddPersistence( connectionString, invariantName )` in `RootBlocks.Persistence.Extensions` works with any provider registered in `DbProviderFactories`. Register the provider first, then the factory:

```csharp
using System.Data.Common;
using RootBlocks.Persistence.Extensions;

DbProviderFactories.RegisterFactory( "FirebirdSql.Data.FirebirdClient", FirebirdClientFactory.Instance );
services.AddPersistence( connectionString, "FirebirdSql.Data.FirebirdClient" );
```

The provider must be registered before `AddPersistence` is called: the factory looks the provider up in its constructor and `DbProviderFactories.GetFactory` throws an `ArgumentException` for an unknown name.

### One factory per application

`AddPersistence` registers a single `IConnectionFactory`. Calling it twice, for instance for two databases, leaves the last registration in charge. If you need connections to several databases, wrap a `ConnectionFactory` per database in your own types:

```csharp
public sealed class ReportingConnectionFactory( string connectionString )
    : ConnectionFactory( connectionString, "Microsoft.Data.SqlClient" );

services.AddSingleton( new ReportingConnectionFactory( reportingConnectionString ) );
```

## Using it

```csharp
public interface IConnectionFactory
{
    string ConnectionString { get; }
    DbConnection CreateConnection();
}
```

`CreateConnection()` returns a closed connection with its connection string set. Dapper opens and closes it for you; with plain ADO.NET call `OpenAsync()` yourself. Dispose the connection when you are done.

```csharp
public sealed class PostQueries( IConnectionFactory connectionFactory ) : IPostQueries
{
    public async Task< PostDto? > GetPostAsync( Guid id, CancellationToken cancellationToken = default )
    {
        const string sql = """
                           SELECT p.Id, p.Title, p.Content, p.PublishedOn
                             FROM Post p
                            WHERE p.Id = @Id
                           """;

        await using var connection = connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync< PostDto >(
            new CommandDefinition( sql, new { Id = id }, cancellationToken: cancellationToken )
        );
    }
}
```

The query interface (`IPostQueries`) and its DTOs belong to your domain or application layer; the implementation, with its SQL, belongs to infrastructure. The [Blogs sample](../sample.md) follows that split, with multi-result queries for pages and totals.

`CreateConnection()` throws `InvalidOperationException` if the provider returns no connection, which in practice means a misconfigured provider.

## Writes go through EF

The connection factory is meant for reads. Writes go through aggregates, the [unit of work](entity-framework.md#unitofwork) and EF Core, so that domain rules run and domain events are raised. Nothing stops you from executing an `UPDATE` through a raw connection, but it bypasses all of that, including the [outbox](entity-framework.md#the-outbox).

## Target frameworks

The base package and every driver except Npgsql target `netstandard2.1`, so a read-side project can stay on .NET Standard. `RootBlocks.Persistence.Npgsql` is the exception: it targets `net10.0`, so a read side on PostgreSQL must too. See [Target frameworks](../target-frameworks.md).
