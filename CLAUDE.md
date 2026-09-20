# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

RootBlocks is a collection of .NET NuGet packages providing DDD (Domain-Driven Design) building blocks to eliminate boilerplate and let developers focus on business logic. It targets `netstandard2.1` for the core package and `net9.0` for integration packages.

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
| `RootBlocks.AspNetCore.Swashbuckle` | Swagger/OpenAPI support |
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
- **Api** — Controllers, request models, DI composition root

### Coding Conventions

- Use `#region` blocks to group class members (Properties, Constructors, Overrides, etc.) — this is consistent across the codebase.
- `Usings.cs` files in each project carry global `using` statements.
- All datetimes are UTC.
- Entity IDs are never database-generated (`ValueGeneratedNever()`); they are assigned by the domain layer on construction.
- `Enumeration` entries must be declared as `public static readonly` fields on the subclass — `GetAll<T>()` reflects over them.