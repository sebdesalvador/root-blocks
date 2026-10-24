# RootBlocks

RootBlocks is a set of .NET packages that provide the building blocks of Domain-Driven Design (DDD). It covers entities, aggregate roots, value objects, enumeration classes, domain events, a unit of work with an outbox, and persistence helpers for Entity Framework Core. You write the business rules and RootBlocks handles the repetitive parts.

```csharp
public class Post : Entity< PostId >, IAggregateRoot
{
    public string Title { get; private set; }
    public DateTime? PublishedOn { get; private set; }

    public Post( string title ) : base( PostId.New() ) => Title = title;

    public void Publish()
    {
        PublishedOn = DateTime.UtcNow;
        AddDomainEvent( new PostPublished( Id ) );
    }
}

// In a command handler
var post = await posts.GetByIdAsync( postId, cancellationToken );
post.Publish();
unitOfWork.RegisterDirty( post );
await unitOfWork.CommitAsync( cancellationToken ); // saves, then publishes PostPublished
```

## Packages

| Package | Target | What it gives you |
|---------|--------|-------------------|
| [`RootBlocks`](docs/core/domain-model.md) | `netstandard2.1` | Core DDD types (`Entity<T>`, `IAggregateRoot`, `ValueObject`, `Enumeration`, `DomainEvent`), repository and unit-of-work abstractions, pagination, exceptions and small helpers. Every other package depends on it. |
| [`RootBlocks.Persistence.EntityFramework`](docs/packages/entity-framework.md) | `net10.0` | EF Core base `DbContext`, entity configurations, `UnitOfWork`, soft delete, optimistic concurrency and the transactional outbox. |
| [`RootBlocks.Persistence`](docs/packages/connection-factory.md) | `netstandard2.1` | An `IConnectionFactory` for raw ADO.NET or Dapper queries (the read side of CQRS). Use it with one of the drivers below. |
| [`RootBlocks.Persistence.SqlClient`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | SQL Server driver (`Microsoft.Data.SqlClient`). |
| [`RootBlocks.Persistence.Npgsql`](docs/packages/connection-factory.md#drivers) | `net10.0` | PostgreSQL driver (`Npgsql`). |
| [`RootBlocks.Persistence.MySqlClient`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | MySQL driver (`MySql.Data`). |
| [`RootBlocks.Persistence.Oracle`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | Oracle driver (`Oracle.ManagedDataAccess`). |
| [`RootBlocks.Persistence.SQLite`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | SQLite driver (`System.Data.SQLite`). |
| [`RootBlocks.Messaging.MediatR`](docs/packages/messaging-mediatr.md) | `net10.0` | An `IEventPublisher` that dispatches domain events through MediatR. |
| [`RootBlocks.Serialization`](docs/packages/serialization.md) | `net10.0` | A `System.Text.Json` converter that round-trips domain events, private setters included. |
| [`RootBlocks.Serialization.Newtonsoft.Json`](docs/packages/serialization.md#rootblocksserializationnewtonsoftjson) | `net10.0` | Provides the Newtonsoft JSON Patch input formatter for MVC apps. |
| [`RootBlocks.Logging.Serilog`](docs/packages/logging-serilog.md) | `net10.0` | A Serilog implementation of `ILogContext`, plus a preset logging setup for apps that have no `appsettings.json`. |
| [`RootBlocks.AspNetCore.OpenApi`](docs/packages/openapi.md) | `net10.0` | Transformers for the built-in `Microsoft.AspNetCore.OpenApi` document, such as JSON Patch examples. Works with any UI. |

See [Target frameworks](docs/target-frameworks.md) for what each target means for your project.

## Installation

Install the packages you need from NuGet:

```bash
dotnet add package RootBlocks
dotnet add package RootBlocks.Persistence.EntityFramework
dotnet add package RootBlocks.Messaging.MediatR
dotnet add package RootBlocks.Persistence.SqlClient   # or .Npgsql, .MySqlClient, .Oracle, .SQLite
```

RootBlocks does not ship an identifier type. `Entity<T>` accepts any `T : IEquatable<T>`, such as `Guid`, `int` or `string`. For strongly-typed IDs like `PostId`, use the separate [StronglyTypedIdentifier](https://github.com/sebdesalvador/StronglyTypedIdentifier) library, which also provides the EF Core and ASP.NET Core integration for those IDs. The examples in these docs use it.

## Documentation

- [Getting started](docs/getting-started.md): build a working aggregate, from the domain model to the API, step by step.
- Core library
  - [Domain model](docs/core/domain-model.md): entities, aggregate roots, value objects, enumerations, domain events and domain exceptions.
  - [Utilities](docs/core/utilities.md): pagination, extension methods, validation, retry and the other abstractions.
- Packages
  - [Entity Framework persistence](docs/packages/entity-framework.md)
  - [Connection factory and database drivers](docs/packages/connection-factory.md)
  - [Messaging with MediatR](docs/packages/messaging-mediatr.md)
  - [Serialization](docs/packages/serialization.md)
  - [Logging with Serilog](docs/packages/logging-serilog.md)
  - [OpenAPI](docs/packages/openapi.md)
- [Configuration reference](docs/configuration.md): every registration method and setting in one place.
- [Target frameworks](docs/target-frameworks.md)
- [The Blogs sample](docs/sample.md): a full reference application.
- [Contributing](docs/contributing.md): building, testing, versioning and releasing.

## Sample

[`samples/Blogs`](samples/Blogs) is a minimal API that uses every package together. It has three aggregates (`Person`, `Blog`, `Post`), MediatR commands, EF Core for writes and Dapper for reads. See [the sample guide](docs/sample.md).

```bash
dotnet run --project samples/Blogs/src/Blogs.Api/
```

## License

[MIT](LICENSE) © Sébastien de Salvador
