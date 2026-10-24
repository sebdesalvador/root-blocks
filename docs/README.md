# RootBlocks documentation

Start with [Getting started](getting-started.md) if you are new to RootBlocks. It builds one aggregate from the domain model to an HTTP endpoint.

## Guides

- [Getting started](getting-started.md)
- [The Blogs sample](sample.md)

## Core library (`RootBlocks`)

- [Domain model](core/domain-model.md): `Entity<T>`, `IAggregateRoot`, `ValueObject`, `Enumeration`, `DomainEvent`, repositories, the unit of work and domain exceptions.
- [Utilities](core/utilities.md): pagination, extension methods, `ValidationResult`, `Retry`, and the logging, messaging and execution-context abstractions.

## Packages

- [Entity Framework persistence](packages/entity-framework.md): `DatabaseContext`, entity configurations, `UnitOfWork`, soft delete, concurrency and the outbox.
- [Connection factory and database drivers](packages/connection-factory.md): `IConnectionFactory` with SQL Server, PostgreSQL, MySQL, Oracle and SQLite.
- [Messaging with MediatR](packages/messaging-mediatr.md): publishing domain events in-process.
- [Serialization](packages/serialization.md): round-tripping domain events with `System.Text.Json`, and JSON Patch for MVC.
- [Logging with Serilog](packages/logging-serilog.md): `ILogContext` and a preset logging setup.
- [OpenAPI](packages/openapi.md): transformers for the built-in OpenAPI document.

## Reference

- [Configuration reference](configuration.md)
- [Target frameworks](target-frameworks.md)
- [Contributing](contributing.md)
