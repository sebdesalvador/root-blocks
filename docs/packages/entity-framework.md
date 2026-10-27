# Entity Framework persistence

Package: `RootBlocks.Persistence.EntityFramework` (`net10.0`)

EF Core base classes for the domain model: a base `DbContext`, entity configurations, a unit of work that commits aggregates with their domain events, soft delete, optimistic concurrency, and a transactional outbox that works per context and per schema.

```bash
dotnet add package RootBlocks.Persistence.EntityFramework
```

## `DatabaseContext`

Derive your context from `DatabaseContext`. On every save it:

- sets `CreatedOn` and `LastModifiedOn` on entities (`IDatable`);
- turns the deletion of an entity that has a `DeletedOn` column into an update of that column (soft delete);
- detaches `Enumeration` instances, which are reference data and never written.

When the model is built it sets the default schema (`dbo`, or the one passed to `DatabaseContext( options, defaultSchema )`), stores every `DateTime` as UTC, and adds `DeletedOn` to unique indexes of soft-deletable entities.

```csharp
public sealed class Context( DbContextOptions options ) : DatabaseContext( options, "sales" )
{
    public DbSet< Order > Orders { get; init; } = null!;

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.ApplyConfigurationsFromAssembly( Assembly.GetExecutingAssembly() );
        modelBuilder.ApplyOutbox();       // optional, see "The outbox"
        base.OnModelCreating( modelBuilder ); // after your configurations
    }
}
```

Call `base.OnModelCreating` last: it can only adjust the entities that are already in the model.

## Entity configurations

| Base class | Maps |
|------------|------|
| `EntityConfiguration< TEntity, TId >` | Table named after the type, `Id` key never generated, `CreatedOn`/`LastModifiedOn`, a `RowVersion` concurrency token, `DomainEvents` ignored |
| `SoftDeletableEntityConfiguration< TEntity, TId >` | The above plus a `DeletedOn` shadow column, its index and a query filter hiding deleted rows |
| `ValueObjectConfiguration< T >` | A value object in its own table with a shadow `Guid` key |
| `SoftDeletableValueObjectConfiguration< T >` | The above with `DeletedOn` |
| `EnumerationConfiguration< T >` | An `Enumeration` table with a unique `Name` (256), seeded from `GetAll< T >()` |
| `TranslatableEnumerationConfiguration< T >` | The above plus `Key` (512) |
| `DerivedEntityConfiguration< TEntity, TBase >` | A subtype in its own table (TPT) |

## `UnitOfWork`

Derive one per context and register it as `IUnitOfWork`:

```csharp
public class UnitOfWork( Context context, IEventPublisher eventPublisher, OutboxPublisher outboxPublisher )
    : RootBlocks.Persistence.EntityFramework.UnitOfWork( context, eventPublisher, outboxPublisher );
```

`RegisterNew`, `RegisterClean`, `RegisterDirty` and `RegisterDeleted` attach aggregate roots to the context. `Commit`/`CommitAsync` collect the domain events of every tracked entity, save, and throw `ConcurrencyException` when rows were expected to change and none did (EF's own `DbUpdateConcurrencyException` covers a stale `RowVersion`). What happens to the events depends on whether the context maps the outbox:

- **With the outbox** they are written to it in the same `SaveChanges` and published after the commit. See below.
- **Without it** they are published through `IEventPublisher` after the save. A failed publish throws to the caller after the change is committed, and the event is lost.

`BeginTransaction(Async)`, `CommitTransaction(Async)` and `RollbackTransaction` wrap several commits in one database transaction. With the outbox, events are published only once that transaction commits, and dropped if it rolls back.

The two-argument constructor `UnitOfWork( context, eventPublisher )` still works; its outbox then uses the default `OutboxOptions` and does not log.

## The outbox

The transactional outbox guarantees that an event is published if, and only if, the change that raised it is committed. Each context gets its own outbox table in its own schema, so a modular monolith with one context per module gets one outbox per module, each with its own dispatcher.

### How it works

1. **Commit.** In the same `SaveChanges` as the change, the unit of work writes one `OutboxMessage` per domain event and clears the events from the entities. The message is locked for `LockDuration` so that no dispatcher takes it while the request is still publishing it.
2. **Publish straight away.** After the commit, the unit of work publishes each event and marks its message processed. A failure, a timeout included, is never thrown to the caller, since the change is already committed: it increments `Attempts`, stores the exception in `LastError`, and holds the message back for an exponential delay (`FirstRetryDelay`, doubling up to `MaxRetryDelay`) by setting `LockedUntil`. The outcome is saved with `CancellationToken.None`. Only the caller's own cancellation stops publishing; it then throws `OperationCanceledException`, and what is left is published by the dispatcher.
3. **Dispatch the rest.** `OutboxDispatcher< TContext >` polls every `PollInterval`. It takes up to `BatchSize` messages, oldest first, that are not processed, not locked and under `MaxAttempts`. It claims each one with a conditional `UPDATE` on `LockedUntil` and skips it if another host got there first, then publishes it and records the outcome as above. Then it deletes the messages processed more than `Retention` ago. An exception is logged and the dispatcher carries on at the next tick.

No database feature beyond plain SQL is needed: no SQL Server Service Broker, so it runs on Azure SQL, Azure SQL Edge and containers.

### Delivery is at least once

An event can be published more than once: a host can crash after publishing and before recording it, or a publish can outlast `LockDuration` and be taken over by a dispatcher. **Event handlers must be idempotent.** Each event keeps its `DomainEvent.Id` across retries, so a handler can use it to detect a duplicate.

### Setting it up

```csharp
// The context: map the outbox into the context's default schema.
protected override void OnModelCreating( ModelBuilder modelBuilder )
{
    modelBuilder.ApplyConfigurationsFromAssembly( Assembly.GetExecutingAssembly() );
    modelBuilder.ApplyOutbox();
    base.OnModelCreating( modelBuilder );
}

// The composition root: one AddOutbox per context.
services.AddDbContext< SalesContext >( o => o.UseSqlServer( connectionString ) );
services.AddOutbox< SalesContext >( configuration );
services.AddDbContext< BillingContext >( o => o.UseSqlServer( connectionString ) );
services.AddOutbox< BillingContext >( configuration );
```

`ApplyOutbox()` works on any `ModelBuilder`, not only in a `DatabaseContext`. It maps an `Outbox` table (pass another name as its argument) into the model's default schema, so `HasDefaultSchema( "billing" )` gives `billing.Outbox`. Add a migration afterwards.

`AddOutbox< TContext >( configuration )` registers `OutboxOptions` from the `Outbox` section, `TimeProvider.System` (unless one is registered), the scoped `OutboxPublisher` and the `OutboxDispatcher< TContext >` hosted service. An `IEventPublisher` must be registered too, for instance with `AddMessaging()`.

### Options

```json
{
  "Outbox": {
    "PollInterval": "00:00:05",
    "LockDuration": "00:01:00",
    "FirstRetryDelay": "00:00:05",
    "MaxRetryDelay": "00:15:00",
    "MaxAttempts": 100,
    "Retention": "7.00:00:00",
    "BatchSize": 20
  }
}
```

| Option | Default | Meaning |
|--------|---------|---------|
| `PollInterval` | 5 s | How often the dispatcher looks for messages. |
| `LockDuration` | 1 min | How long a claimed message is left to whoever claimed it, the committing request included. Keep it above your slowest handler. |
| `FirstRetryDelay` | 5 s | Wait before the first retry; each further failure doubles it. |
| `MaxRetryDelay` | 15 min | The longest wait between two retries. |
| `MaxAttempts` | 100 | After this many failures a message is no longer retried and waits for a human. With the default delays that is about a day. |
| `Retention` | 7 days | How long processed messages are kept. |
| `BatchSize` | 20 | Messages per dispatcher tick. |

The options are shared by every context. Time comes from the registered `TimeProvider`, so tests can replace it with `FakeTimeProvider`.

### The table

| Column | Type | |
|--------|------|-|
| `Id` | `uniqueidentifier` | The event's `Id`; primary key. |
| `Type` | `nvarchar(512)` | The event's assembly-qualified type name. Read back ignoring the assembly version, so rows written before a deployment are still read after it. |
| `Payload` | `nvarchar(max)` | The event as JSON, by its runtime type. |
| `OccurredOn` | `datetime2` | The event's `OccurredOn`. |
| `ProcessedOn` | `datetime2`, null | When it was published. |
| `Attempts` | `int` | Failed publications. |
| `LastError` | `nvarchar(max)`, null | The last exception. |
| `LockedUntil` | `datetime2`, null | Until when it is claimed, or when it is retried next. |

An index on (`ProcessedOn`, `OccurredOn`) serves the dispatcher.

To find messages that gave up: `SELECT * FROM sales.Outbox WHERE ProcessedOn IS NULL AND Attempts >= 100`. To retry one, set its `Attempts` back to 0.

### What an event must look like

The payload is written with `System.Text.Json`. Private setters and a private parameterless constructor are used when the event is read back, so the usual shape of a domain event works, and `Id`, `OccurredOn`, `CausationId` and `CorrelationId` keep their values. A property that has no setter at all must be a constructor parameter of the same name, or it is lost.

## Design-time factory

`DesignTimeDatabaseContextFactory< T >` builds the context for `dotnet ef` from `ConnectionStrings:DefaultConnection` in `appsettings.json`, `appsettings.{ASPNETCORE_ENVIRONMENT}.json`, `appsettings.Local.json` and the environment, using SQL Server.

```csharp
public class DesignTimeContextFactory : DesignTimeDatabaseContextFactory< Context >
{
    protected override Context InitializeContext( DbContextOptions< Context > options ) => new( options );
}
```

## Obsolete API

These still compile, with a warning, and will be removed in a later version. See [Upgrading to 0.6](../upgrading-to-0.6.md).

| Obsolete | Use instead |
|----------|-------------|
| `DatabaseContextWithOutbox` | `DatabaseContext` and `modelBuilder.ApplyOutbox()`. It now maps the new outbox. |
| `IDatabaseContextWithOutbox` | Nothing: it only served `AddOutboxListener`. |
| `AddOutboxListener()` | `AddOutbox< TContext >( configuration )`. It now registers an `OutboxDispatcher` for the context registered as `IDatabaseContextWithOutbox`. |
| `OutboxItem`, `OutboxItemConfiguration`, `DomainEventToJsonConverter` | `OutboxMessage`. |
| `NoCompatibleDatabaseContextFoundException`, `UnableToDeserializeDomainEventException` | Nothing: no longer thrown. |
