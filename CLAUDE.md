# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

RootBlocks is a collection of .NET NuGet packages providing DDD (Domain-Driven Design) building blocks to eliminate boilerplate and let developers focus on business logic. It targets `netstandard2.1` for the core package and the database drivers, and `net10.0` for the integration packages, the sample and the tests. The SDK is pinned in `global.json`.

## Commands

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test test/RootBlocks.Tests/

# Pack a specific NuGet package
dotnet pack src/RootBlocks/RootBlocks.csproj --configuration Release

# Run the sample application
dotnet run --project samples/Blogs/src/Blogs.Api/
```

Versioning is handled automatically by **MinVer** using git tags prefixed with `v` (e.g. `v1.2.3`). NuGet packages are published to nuget.org on GitHub Release events.

## Architecture

### Package Structure

| Package | Purpose |
|---------|---------|
| `RootBlocks` | Core DDD abstractions — all other packages depend on this |
| `RootBlocks.Persistence.EntityFramework` | EF Core implementation with outbox, soft-delete, concurrency |
| `RootBlocks.Persistence.*` | DB-specific drivers (SqlClient, MySqlClient, Oracle, SQLite, Npgsql) |
| `RootBlocks.Messaging.MediatR` | MediatR implementation of `IEventPublisher` |
| `RootBlocks.Serialization` / `.Newtonsoft.Json` | Domain event serialization |
| `RootBlocks.Logging.Serilog` | Serilog integration |
| `RootBlocks.AspNetCore.OpenApi` | OpenAPI document transformers for the built-in `AddOpenApi()` — UI-agnostic; the sample pairs it with Scalar |
| `samples/Blogs` | Reference implementation using all packages together |

### Core DDD Primitives (`src/RootBlocks/Aggregate/`)

- **`Identity`** — Abstract base for strongly-typed IDs (wraps a `Guid`). Subclass with `new()` constraint so EF can instantiate them.
- **`Entity<T>`** — Abstract base for entities; holds `DomainEvents`, `CreatedOn`, `LastModifiedOn`, and an `Id` of type `T : Identity`.
- **`IAggregateRoot`** — Marker interface; only aggregate roots are registered with the `UnitOfWork`.
- **`ValueObject`** — Structural equality via `GetAtomicValues()`.
- **`Enumeration`** — GUID-based alternative to C# enums with `GetAll<T>()`, `FromValue<T>()`, `FromDisplayName<T>()`.
- **`DomainEvent`** — Abstract base carrying `Id`, `CausationId`, `CorrelationId`, `OccurredOn`, and type metadata. Implements `INotification` (MediatR).

### Helpers (`src/RootBlocks/`)

- **`Extensions/`** — Extension methods over BCL types: `CollectionExtensions`, `DateTimeExtensions`, `StringExtensions`, `ValidationExtensions`, `GuidExtensions` (`Guid.ToIdentity<T>()`). Only add one here when it is not already in the BCL — don't reimplement `Take`, `Any` or `FirstOrDefault`.
- **`Validation/ValidationResult`** — Immutable success/failure pair returned by `ValidateRequired`.
- **`Resilience/Retry`** — Minimal retry helper with linear backoff. Reach for Polly instead when jitter, circuit breaking or per-exception policies are needed.

### OpenAPI (`src/RootBlocks.AspNetCore.OpenApi/`)

Transformers for the built-in `Microsoft.AspNetCore.OpenApi` generation — no Swashbuckle dependency, so any UI works.

- **`IdentitySchemaTransformer`** — Renders `Identity` subclasses in **body schemas** as `{"type":"string","format":"uuid"}`. Without it the generator sees the custom JSON converter, cannot infer a shape and emits an empty schema, which client generators read as `any`.
- **`IdentityParameterTransformer`** — Does the same for `Identity` **route and query parameters**. These never reach a schema transformer: a parameter bound through `TryParse` is described from its binding source, so the generator settles for a bare string. Both are registered by `AddStronglyTypedIds()`.
- **`JsonPatchExampleTransformer`** — Attaches a worked example to `application/json-patch+json` bodies.
- **`OpenApiOptionsExtensions`** — One extension per transformer, each named for what it does, so consumers take only what they want. There is deliberately no umbrella method: a new transformer gets its own named extension rather than silently joining a grab-bag.

  ```csharp
  builder.Services.AddOpenApi( o =>
  {
      o.AddStronglyTypedIds();
      o.AddJsonPatchExamples();
  } );
  ```

The package ships `buildTransitive/*.props` opting consumers into the `Microsoft.AspNetCore.OpenApi.Generated` interceptors namespace; without it, any project with `GenerateDocumentationFile` fails to compile with CS9137. Projects referencing this one by **project** reference (the sample, the tests) must set `InterceptorsNamespaces` themselves, since buildTransitive assets only flow through package references.

### Persistence Layer (`src/RootBlocks.Persistence.EntityFramework/`)

- **`DatabaseContext`** — Base EF `DbContext` that auto-sets `CreatedOn`/`LastModifiedOn`, detaches `Enumeration` entries from the tracker, handles soft-delete (sets `DeletedOn` instead of removing rows), and applies UTC datetime converters.
- **`DatabaseContextWithOutbox`** — Extends `DatabaseContext` with a `DbSet<OutboxItem>` for the outbox pattern.
- **`UnitOfWork`** — Implements the UoW pattern. On `Commit`/`CommitAsync`: saves domain events to the outbox (or local list), persists, then publishes via `IEventPublisher`. Throws `ConcurrencyException` if rows were expected to change but didn't.
- **`EntityConfiguration<TEntity, TIdentity>`** — Base EF configuration that maps `Id` via `IdentityToGuidConverter`, sets the concurrency token (`RowVersion`), ignores `DomainEvents`, and auto-names the table to the entity type name.

### Key Patterns Used in the Sample (`samples/Blogs/`)

The Blogs sample demonstrates the canonical project layout:
- **Core** — domain model (aggregates, value objects, domain events, repository interfaces, read-model DTOs and query interfaces)
- **Application** — CQRS commands and handlers (MediatR), no infrastructure references
- **Infrastructure** — EF configurations, repository/UoW implementations, query implementations (raw SQL), DI registrations
- **Api** — Minimal API endpoint groups, request models, DI composition root

The Api layer uses **minimal APIs, not MVC**: no controllers, no `AddControllers()`, no `MapControllers()`. Each aggregate gets an `Endpoints/XEndpoints.cs` with a `MapXEndpoints()` extension building a `MapGroup`, and `public static` handler methods.

Two consequences worth knowing:
- Handlers are `public`, not `private`, because the compiler only emits XML doc comments for visible members and .NET 10 lifts those into the OpenAPI document. Making them private silently strips every `summary` from the document.
- Minimal APIs do not consult `TypeConverter`, so an `Identity` used as a route or query parameter needs a `TryParse`. Each id type declares a two-line one delegating to `Identity.TryCreate<T>`; without it the build fails with `ASP0020`.

JSON Patch uses `Microsoft.AspNetCore.JsonPatch.SystemTextJson` — the Newtonsoft input formatter was MVC-only.

### Coding Conventions

- Use `#region` blocks to group class members (Properties, Constructors, Overrides, etc.) — this is consistent across the codebase.
- `Usings.cs` files in each project carry global `using` statements.
- All datetimes are UTC.
- Entity IDs are never database-generated (`ValueGeneratedNever()`); they are assigned by the domain layer on construction.
- `Enumeration` entries must be declared as `public static readonly` fields on the subclass — `GetAll<T>()` reflects over them.