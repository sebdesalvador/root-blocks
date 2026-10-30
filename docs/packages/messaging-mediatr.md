# Messaging with MediatR

Package: `RootBlocks.Messaging.MediatR` (`net10.0`)

The unit of work publishes domain events through `IEventPublisher`, an interface from the core package. This package implements it with [MediatR](https://github.com/jbogard/MediatR), so domain events are dispatched in-process to ordinary `INotificationHandler<T>`s.

```bash
dotnet add package RootBlocks.Messaging.MediatR
```

The package depends on MediatR 12.4, which is licensed under Apache 2.0. MediatR 13 and later are commercially licensed, so upgrading MediatR in your own application is a licensing decision as well as a technical one.

## Registering

```csharp
using RootBlocks.Messaging.MediatR.Extensions;

services.AddMessaging();   // IEventPublisher → EventPublisher, scoped

// MediatR itself, scanning the assemblies that hold your handlers
services.AddMediatR( cfg => cfg.RegisterServicesFromAssembly( typeof( PublishPostCommand ).Assembly ) );
```

`AddMessaging()` registers only the publisher. It does not register MediatR, because the assemblies to scan are yours to choose. If a notification handler lives in an assembly the scan does not cover, the event is published to nobody and no error is raised.

## Handling an event

`DomainEvent` implements MediatR's `INotification`, so a handler is a normal notification handler:

```csharp
public sealed class SendNewsletterWhenPostPublished( INewsletter newsletter )
    : INotificationHandler< PostPublished >
{
    public Task Handle( PostPublished notification, CancellationToken cancellationToken )
        => newsletter.AnnounceAsync( notification.PostId, notification.Id, cancellationToken );
}
```

MediatR runs every handler of the event one after the other, in the publisher's DI scope. An exception in a handler stops the remaining handlers and propagates to the publisher. What happens next depends on the unit of work:

- **With the [outbox](entity-framework.md#the-outbox)**, the failure is recorded on the outbox row and the whole event is published again later, to every handler. This is why handlers must be **idempotent**: use `notification.Id`, which stays the same across retries, to recognise an event you have already handled.
- **Without the outbox**, the exception reaches the caller of `Commit`/`CommitAsync` after the change has been saved, and the event is not published again.

## Correlation IDs

`EventPublisher` has two constructors. If an `IExecutionContext` is registered, DI picks the one that takes it, and the publisher copies the context's `CausationId` and `CorrelationId` onto each event (`DomainEvent.SetCorrelationIds`) just before publishing it. Without one, the event keeps whatever IDs it was raised with.

```csharp
services.AddScoped< IExecutionContext, HttpExecutionContext >(); // your implementation
```

RootBlocks does not ship an `IExecutionContext`; see [Utilities](../core/utilities.md#iexecutioncontext-and-iexecutioncontextcache) for what it carries.

Keep in mind that the outbox dispatcher publishes from a background service, in a scope it creates itself. An `IExecutionContext` resolved there has not been filled in by any request, and the publisher will copy its values over the IDs stored with the event. If your implementation cannot tell it is outside a request, either have it return the event's own IDs in that case or leave correlation to the code that raises the event.

## Logging

Every publish writes an `Information` entry, `Publishing {Event} event ({EventId})`, through `ILogger< EventPublisher >`, with the event's `TypeName` and `Id`.

## Using another bus

`IEventPublisher` has a single method:

```csharp
Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default );
```

To send events to a broker instead of, or as well as, in-process handlers, implement it yourself and register it as `IEventPublisher` in place of `AddMessaging()`. The unit of work and the outbox only ever talk to the interface.
