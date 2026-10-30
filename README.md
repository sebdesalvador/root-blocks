# RootBlocks

[![NuGet](https://img.shields.io/nuget/v/RootBlocks.svg?label=RootBlocks)](https://www.nuget.org/packages/RootBlocks)
[![publish](https://github.com/sebdesalvador/root-blocks/actions/workflows/publish.yml/badge.svg)](https://github.com/sebdesalvador/root-blocks/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

RootBlocks is a set of .NET packages that provide the building blocks of Domain-Driven Design (DDD). It covers entities, aggregate roots, value objects, enumeration classes, domain events, a unit of work with a transactional outbox, and persistence helpers for Entity Framework Core and raw SQL. You write the business rules and RootBlocks handles the repetitive parts.

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
await unitOfWork.CommitAsync( cancellationToken ); // saves the post and PostPublished together, then publishes it
```

## Features

- **Domain model**: `Entity<T>` with domain events and audit dates, `IAggregateRoot`, `ValueObject` with structural equality, `Enumeration` classes, `DomainEvent` with causation and correlation IDs, and domain exceptions.
- **Unit of work for EF Core**: commits aggregates, collects their domain events, and checks optimistic concurrency with a `RowVersion`.
- **Transactional outbox**: events are saved in the same transaction as the change, published straight after, and retried with exponential backoff by a background dispatcher. One outbox per context, so each module of a modular monolith has its own. Plain SQL only: no Service Broker, so it runs on Azure SQL and in containers.
- **Soft delete, UTC dates and auditing** handled by the base `DbContext` and entity configurations.
- **CQRS read side**: an `IConnectionFactory` for Dapper or ADO.NET, with drivers for SQL Server, PostgreSQL, MySQL, Oracle and SQLite.
- **Integrations**: MediatR for in-process events, Serilog for log context, and transformers for the built-in ASP.NET Core OpenAPI document.

## Packages

| Package | Target | What it gives you |
|---------|--------|-------------------|
| [`RootBlocks`](docs/core/domain-model.md) | `netstandard2.1` | Core DDD types (`Entity<T>`, `IAggregateRoot`, `ValueObject`, `Enumeration`, `DomainEvent`), repository and unit-of-work abstractions, pagination, exceptions and small helpers. Every other package depends on it. |
| [`RootBlocks.Persistence.EntityFramework`](docs/packages/entity-framework.md) | `net10.0` | EF Core base `DbContext`, entity configurations, `UnitOfWork`, soft delete, optimistic concurrency and the transactional outbox. |
| [`RootBlocks.Persistence`](docs/packages/connection-factory.md) | `netstandard2.1` | An `IConnectionFactory` for raw ADO.NET or Dapper queries (the read side of CQRS). Use it with one of the drivers below. |
| [`RootBlocks.Persistence.SqlClient`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | SQL Server driver (`Microsoft.Data.SqlClient`). |
| [`RootBlocks.Persistence.Npgsql`](docs/packages/connection-factory.md#drivers) | `net10.0` | PostgreSQL driver (`Npgsql`). |
| [`RootBlocks.Persistence.MySqlClient`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | MySQL driver (`MySql.Data`). |
| [`RootBlocks.Persistence.Oracle`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | Oracle driver (`Oracle.ManagedDataAccess.Client`, from `CYQ.Data.Oracle`). |
| [`RootBlocks.Persistence.SQLite`](docs/packages/connection-factory.md#drivers) | `netstandard2.1` | SQLite driver (`System.Data.SQLite`). |
| [`RootBlocks.Messaging.MediatR`](docs/packages/messaging-mediatr.md) | `net10.0` | An `IEventPublisher` that dispatches domain events through MediatR. |
| [`RootBlocks.Serialization`](docs/packages/serialization.md) | `net10.0` | A `System.Text.Json` converter that round-trips domain events, private setters included. |
| [`RootBlocks.Serialization.Newtonsoft.Json`](docs/packages/serialization.md#rootblocksserializationnewtonsoftjson) | `net10.0` | The Newtonsoft JSON Patch input formatter for MVC apps. Minimal APIs do not need it. |
| [`RootBlocks.Logging.Serilog`](docs/packages/logging-serilog.md) | `net10.0` | A Serilog implementation of `ILogContext`, plus a preset logging setup for apps that have no `appsettings.json`. |
| [`RootBlocks.AspNetCore.OpenApi`](docs/packages/openapi.md) | `net10.0` | Transformers for the built-in `Microsoft.AspNetCore.OpenApi` document, such as JSON Patch examples. Works with any UI. |

See [Target frameworks](docs/target-frameworks.md) for what each target means for your project.

## Installation

Install the packages you need from [NuGet](https://www.nuget.org/packages?q=RootBlocks):

```bash
dotnet add package RootBlocks
dotnet add package RootBlocks.Persistence.EntityFramework
dotnet add package RootBlocks.Messaging.MediatR
dotnet add package RootBlocks.Persistence.SqlClient   # or .Npgsql, .MySqlClient, .Oracle, .SQLite
```

RootBlocks does not ship an identifier type. `Entity<T>` accepts any `T : IEquatable<T>`, such as `Guid`, `int` or `string`. For strongly-typed IDs like `PostId`, use the separate [StronglyTypedIdentifier](https://github.com/sebdesalvador/StronglyTypedIdentifier) library, which also provides the EF Core and ASP.NET Core integration for those IDs. The examples in these docs use it.

Then wire it up in your composition root:

```csharp
builder.Services
       .AddMediatR( cfg => cfg.RegisterServicesFromAssembly( typeof( PublishPostCommand ).Assembly ) )
       .AddMessaging()                                     // IEventPublisher through MediatR
       .AddSqlClientPersistence( connectionString )        // IConnectionFactory for queries
       .AddDbContext< Context >( o => o.UseSqlServer( connectionString ) )
       .AddOutbox< Context >( builder.Configuration )      // when the context calls modelBuilder.ApplyOutbox()
       .AddScoped< IUnitOfWork, UnitOfWork >()
       .AddScoped< IPostRepository, PostRepository >();
```

[Getting started](docs/getting-started.md) builds this up step by step.

## Upgrading from 0.5

Version 0.6 rewrote the outbox: it is now per context, retried, and free of Service Broker. The old API still compiles with obsolete warnings. See [Upgrading to 0.6](docs/upgrading-to-0.6.md) for the migration.

## Documentation

- [Getting started](docs/getting-started.md): build a working aggregate, from the domain model to the API, step by step.
- Core library
  - [Domain model](docs/core/domain-model.md): entities, aggregate roots, value objects, enumerations, domain events and domain exceptions.
  - [Utilities](docs/core/utilities.md): pagination, extension methods, validation, retry and the other abstractions.
- Packages
  - [Entity Framework persistence](docs/packages/entity-framework.md): `DatabaseContext`, configurations, `UnitOfWork` and the outbox.
  - [Connection factory and database drivers](docs/packages/connection-factory.md)
  - [Messaging with MediatR](docs/packages/messaging-mediatr.md)
  - [Serialization](docs/packages/serialization.md)
  - [Logging with Serilog](docs/packages/logging-serilog.md)
  - [OpenAPI](docs/packages/openapi.md)
- [Configuration reference](docs/configuration.md): every registration method and setting in one place.
- [Upgrading to 0.6](docs/upgrading-to-0.6.md)
- [Target frameworks](docs/target-frameworks.md)
- [The Blogs sample](docs/sample.md): a full reference application.
- [Contributing](docs/contributing.md): building, testing, versioning and releasing.

## Sample

[`samples/Blogs`](samples/Blogs) is a minimal API that uses every package together. It has three aggregates (`Person`, `Blog`, `Post`), MediatR commands, EF Core with the outbox for writes and Dapper for reads, and a Scalar UI over the OpenAPI document. See [the sample guide](docs/sample.md).

```bash
dotnet run --project samples/Blogs/src/Blogs.Api/
```

## Building from source

```bash
dotnet build RootBlocks.slnx
dotnet test     # the EF tests need Docker for SQL Server
```

The SDK is pinned in [`global.json`](global.json). See [Contributing](docs/contributing.md) for conventions, versioning and releases.

## License

[MIT](LICENSE) © Sébastien de Salvador
