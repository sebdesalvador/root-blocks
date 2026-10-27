# Utilities

Package: `RootBlocks` · Target: `netstandard2.1`

Besides the [domain model](domain-model.md), the core package includes small helpers that most applications end up writing themselves. A helper is only added when the BCL does not already provide it, so there is no wrapper around `Any`, `Take` or `FirstOrDefault`.

## Pagination

Namespace: `RootBlocks.Pagination`

### `Pagination`

`Pagination` describes which page to fetch and how to sort it.

| Property | Default | Notes |
|----------|---------|-------|
| `uint PageIndex` | `1` | Starts at **1**. Setting it to `0` throws `ArgumentOutOfRangeException`. |
| `uint PageSize` | `5` | Must be at least 1. |
| `string? SortColumn` | `null` | The property to sort by. Your query decides how to interpret it. |
| `SortDirection SortDirection` | `Ascending` | `Ascending` or `Descending`. |

```csharp
var page = new Pagination( pageIndex: 2, pageSize: 20, sortColumn: "Title", SortDirection.Descending );
var offset = ( page.PageIndex - 1 ) * page.PageSize;
```

### `PagedResult<T>`

`PagedResult<T>` holds one page of items together with the total number of items, so clients can render page controls.

```csharp
var (posts, total) = await postQueries.FindPostsAsync( searchTerm, tags, pageIndex, pageSize, ct );
return TypedResults.Ok( new PagedResult< PostDto >( posts, total, new Pagination( pageIndex, pageSize ) ) );
```

| Property | Description |
|----------|-------------|
| `IEnumerable<T> Items` | The items on this page. |
| `uint TotalNumberOfItems` | The number of matching items across all pages. |
| `Pagination Pagination` | The request that produced this page. |
| `int TotalNumberOfPages` | Computed as `ceil(TotalNumberOfItems / PageSize)`. |

## Extension methods

Namespace: `RootBlocks.Extensions`

### `CollectionExtensions` (on `IEnumerable<T>?`)

| Method | Description |
|--------|-------------|
| `IsNullOrEmpty()` | Returns `true` for `null` or an empty sequence. It uses `Count` on collections, so it does not enumerate them. |
| `HasItems()` | The opposite of `IsNullOrEmpty()`. |
| `ForEach( action )` | Runs `action` on each item. Does nothing on `null`, and throws if `action` is `null`. |
| `WhereNotNull()` | Removes `null` elements and returns an empty sequence for a `null` source. |
| `ToSafeList()` | Same as `ToList()`, except that `null` gives an empty list. |

### `StringExtensions` (on `string?`)

| Method | Description |
|--------|-------------|
| `IsNullOrEmpty()` / `IsNotNullOrEmpty()` | Wraps `string.IsNullOrEmpty`. The nullable annotations let the compiler know the string is non-null afterwards. |
| `Truncate( maxLength )` | Cuts the string to at most `maxLength` characters. Returns `""` for `null`, and throws if `maxLength < 0`. |

### `DateTimeExtensions` (on `DateTime`)

Every method keeps the input's `DateTimeKind`. The library convention is that domain datetimes are UTC.

| Method | Description |
|--------|-------------|
| `IsBetween( start, end )` | Inclusive on both ends. |
| `StartOfDay()` / `EndOfDay()` | Returns 00:00:00.0000000 or 23:59:59.9999999 on the same date. |
| `GetAge( asOf = null )` | Whole years between the two dates. `asOf` defaults to today in UTC. |
| `IsWeekend()` | `true` for Saturday and Sunday. |
| `NextBusinessDay()` | The next weekday, keeping the time of day. Public holidays are not taken into account. |

### `ValidationExtensions`

| Method | Description |
|--------|-------------|
| `string?.IsValidEmail()` | Checks that the value parses as one `MailAddress` with no display name. It checks the format only, not whether the address exists. |
| `string?.IsValidPhoneNumber()` | Checks that the value has 10 to 15 digits (the E.164 bounds). Separators are ignored. |
| `int.IsInRange( min, max )`, `decimal.IsInRange( min, max )` | Inclusive on both ends. |
| `T?.ValidateRequired( fieldName )` | Returns a [`ValidationResult`](#validationresult): success when the value is not `null`, otherwise `"{fieldName} is required"`. |

## `ValidationResult`

Namespace: `RootBlocks.Validation`

`ValidationResult` is an immutable result that is either a success or a failure with a message.

```csharp
var result = command.Title.ValidateRequired( nameof( command.Title ) );

if ( !result.IsValid )
    return TypedResults.BadRequest( result.ErrorMessage );
```

`ValidationResult.Success()` always returns the same shared instance. `ValidationResult.Failure( message )` requires a non-empty message.

## Retry

Namespace: `RootBlocks.Resilience`

`Retry` is a small helper that retries an operation with a linear backoff. The wait before attempt *n + 1* is `delay × n`.

```csharp
var response = await Retry.ExecuteAsync(
    ct => httpClient.GetStringAsync( url, ct ),
    maxAttempts: 3,                        // total attempts, including the first
    delay: TimeSpan.FromMilliseconds( 500 ), // waits 500 ms, then 1 s
    cancellationToken: cancellationToken );
```

- Every exception is retried until the attempts run out. The last exception is then rethrown unchanged.
- In `ExecuteAsync`, an `OperationCanceledException` caused by your token stops immediately and is not retried.
- `Execute` is the synchronous version. It blocks the thread with `Thread.Sleep` while it waits.
- `maxAttempts` defaults to `Retry.DefaultMaxAttempts` (3) and `delay` defaults to one second.

Use [Polly](https://github.com/App-vNext/Polly) instead when you need jitter, circuit breakers or policies that depend on the exception type.

## Cross-cutting abstractions

These interfaces are defined in the core package so that the domain and application layers can depend on them. Infrastructure packages provide the implementations.

| Interface | Namespace | Implemented by |
|-----------|-----------|----------------|
| `IEventPublisher` | `RootBlocks.Messaging.Abstractions` | [`RootBlocks.Messaging.MediatR`](../packages/messaging-mediatr.md) |
| `ILogContext` | `RootBlocks.Logging.Abstractions` | [`RootBlocks.Logging.Serilog`](../packages/logging-serilog.md) |
| `IExecutionContext` | `RootBlocks.Abstractions` | Your application |
| `IExecutionContextCache` | `RootBlocks.Abstractions` | Your application |

### `IEventPublisher`

```csharp
Task PublishAsync( DomainEvent domainEvent, CancellationToken cancellationToken = default );
```

The unit of work calls this for every domain event after a successful commit.

### `ILogContext`

```csharp
using ( logContext.AddProperty( "PostId", postId ) )
{
    logger.LogInformation( "Publishing post" ); // the log entry carries PostId
}
```

`ILogContext` adds a property to every log entry written inside the `using` scope.

### `IExecutionContext` and `IExecutionContextCache`

`IExecutionContext` carries information about the current request: the caller's `Authorization`, a `CausationId`, a `CorrelationId` and a dictionary of extra `Parameters`. If you register an implementation, the MediatR `EventPublisher` copies the causation and correlation IDs onto each domain event before publishing it. This lets you trace an event back to the request that caused it.

`IExecutionContextCache` creates contexts and looks them up again by ID. Use it when the context has to survive past the request scope, for example when a background worker processes the outbox.

RootBlocks does not ship an implementation of either interface. The usual approach is a scoped service filled in by middleware from the incoming headers.

## `OutboxItem`

Namespace: `RootBlocks.Messaging`

Obsolete since 0.6. `OutboxItem` wrapped a `DomainEvent` for the 0.5 outbox table. The outbox now stores `OutboxMessage` rows from the EF package. See [The outbox](../packages/entity-framework.md#the-outbox).

## `ReflectionHelper`

Namespace: `RootBlocks`

`ReflectionHelper.GetSetterForProperty<T, TValue>( ... )` returns an `Action<T, TValue>` that can assign a property even when it has a private setter or only an auto-property backing field. The [domain event converter](../packages/serialization.md) uses it to rebuild events with private setters. The method accepts a property expression, a property name or a `PropertyInfo`.
