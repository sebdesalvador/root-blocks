# Upgrading to 0.6

0.6 rewrites the transactional outbox of `RootBlocks.Persistence.EntityFramework`. Events are now published once at commit and recorded, retried with backoff when they fail, and dispatched by a polling `OutboxDispatcher< TContext >` per context instead of the SQL Server Service Broker listener. See [The outbox](packages/entity-framework.md#the-outbox).

## Breaking changes

- **The outbox table has new columns** (`Type`, `Payload`, `OccurredOn`, `ProcessedOn`, `Attempts`, `LastError`, `LockedUntil`) and loses `Event`. Every context that maps the outbox needs a migration; see below.
- **`OutboxListener` is removed**, with the Service Broker code behind it. Nothing runs `ALTER DATABASE ... SET ENABLE_BROKER` any more.
- **`DatabaseContextWithOutbox.Outbox` and `IDatabaseContextWithOutbox.Outbox` are `DbSet< OutboxMessage >`**, no longer `DbSet< OutboxItem >`.
- **Processed messages are kept, then purged**, instead of being deleted as soon as they are published. Code that read the table to find pending events must filter on `ProcessedOn IS NULL`.
- **Events are no longer published twice.** 0.5 published them at commit and again from the listener.
- **A failed publish at commit no longer throws** when the context maps the outbox: it is recorded on the message and retried. Without the outbox, it still throws as before.
- **With the unit of work's own transaction** (`BeginTransaction`), events are published when `CommitTransaction` runs, no longer at each `Commit`, and are dropped on `RollbackTransaction`.
- The package no longer depends on `Microsoft.Windows.Compatibility`; it now depends on `Microsoft.Extensions.Options.ConfigurationExtensions`.

Obsolete, still working with a warning: `DatabaseContextWithOutbox`, `IDatabaseContextWithOutbox`, `AddOutboxListener()` (now registers a dispatcher), `OutboxItem`, `OutboxItemConfiguration`, `DomainEventToJsonConverter`, `NoCompatibleDatabaseContextFoundException`, `UnableToDeserializeDomainEventException`.

## Steps

1. **Map the outbox.** Derive the context from `DatabaseContext` and call `modelBuilder.ApplyOutbox()` before `base.OnModelCreating`. Leaving it on `DatabaseContextWithOutbox` also works for now; it maps the same table.

   ```csharp
   public sealed class Context( DbContextOptions options ) : DatabaseContext( options )
   {
       protected override void OnModelCreating( ModelBuilder modelBuilder )
       {
           modelBuilder.ApplyConfigurationsFromAssembly( Assembly.GetExecutingAssembly() );
           modelBuilder.ApplyOutbox();
           base.OnModelCreating( modelBuilder );
       }
   }
   ```

2. **Pass the outbox publisher to the unit of work**, so it uses the configured options and logs failures:

   ```csharp
   public class UnitOfWork( Context context, IEventPublisher eventPublisher, OutboxPublisher outboxPublisher )
       : RootBlocks.Persistence.EntityFramework.UnitOfWork( context, eventPublisher, outboxPublisher );
   ```

3. **Register the dispatcher** in place of the listener, once per context:

   ```csharp
   // services.AddScoped< IDatabaseContextWithOutbox >( sp => sp.GetRequiredService< Context >() );
   // services.AddOutboxListener();
   services.AddOutbox< Context >( builder.Configuration );
   ```

   Optionally add an `Outbox` section to the configuration; every option has a default.

4. **Add a migration** for each context: `dotnet ef migrations add Outbox`. EF renames `Event` to `Payload` and adds the other columns and the index. Rows left by 0.5 cannot be read by 0.6, and were already published in-process when they were committed, so delete them first by adding this at the top of `Up`, with the context's schema:

   ```csharp
   migrationBuilder.Sql( "DELETE FROM [dbo].[Outbox];" );
   ```

   The Blogs sample's [`Outbox` migration](../samples/Blogs/src/Blogs.Infrastructure/Migrations/20261027085355_Outbox.cs) is a worked example.

5. **Make event handlers idempotent.** Delivery is at least once; use `DomainEvent.Id` to spot a duplicate.

## Coming from a hand-written outbox

An application that copied the outbox into its own code (an `OutboxMessage` entity mapped to an `Outbox` table with the columns above, `Type` at 512 characters, and an index on `ProcessedOn`, `OccurredOn`) can switch without changing its tables:

1. Replace its `OutboxMessageConfiguration` with `modelBuilder.ApplyOutbox()` in each module's context.
2. Replace its unit of work with a subclass of `UnitOfWork` taking the `OutboxPublisher`, as in step 2.
3. Replace its `AddOutbox< TContext >()` with RootBlocks' `AddOutbox< TContext >( configuration )`, which binds the same `Outbox` section.
4. Delete its outbox code, then run `dotnet ef migrations add` for each context. The model snapshot changes the entity's CLR type name; the generated migration should be empty, which confirms the tables already match. Rows already in the tables are read as they are: the payload format is the same `System.Text.Json` output.
