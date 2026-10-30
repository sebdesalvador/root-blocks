# Serialization

Two small packages:

- [`RootBlocks.Serialization`](#rootblocksserialization) round-trips domain events with `System.Text.Json`, private setters included.
- [`RootBlocks.Serialization.Newtonsoft.Json`](#rootblocksserializationnewtonsoftjson) provides the Newtonsoft JSON Patch input formatter for **MVC** applications.

Neither is needed for the outbox: `RootBlocks.Persistence.EntityFramework` has its own serializer for that (see [What an event must look like](entity-framework.md#what-an-event-must-look-like)).

## `RootBlocks.Serialization`

Package: `RootBlocks.Serialization` (`net10.0`)

```bash
dotnet add package RootBlocks.Serialization
```

Domain events are usually immutable: private setters, `init` accessors and a private parameterless constructor for tools. `System.Text.Json` cannot rebuild such a type on its own. `DomainEventConverter< T >` can, which makes it useful whenever you store or send events yourself, for example to a message broker or an event store.

### Writing

The converter writes every public property of the event's **runtime** type, by name, using your `JsonSerializerOptions` for the values. That includes the base `DomainEvent` properties, among them `TypeName` and `AssemblyQualifiedTypeName`:

```json
{
  "PostId": "0b8a5e8e-8f4f-4b8e-9a3c-1f0f2b5c6d7e",
  "Id": "1b15c0ad-278a-494b-9b5c-41aa57a66482",
  "CausationId": null,
  "CorrelationId": "2029b400-38b5-484e-bd8e-57bd8e6a5e77",
  "OccurredOn": "2026-10-27T09:30:00Z",
  "TypeName": "PostPublished",
  "AssemblyQualifiedTypeName": "Blogs.Core.Aggregates.PostAggregate.PostPublished, Blogs.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
}
```

### Reading

To read an event back, the converter:

1. looks up the type from `AssemblyQualifiedTypeName`, and failing that, by `TypeName` among the types of every loaded assembly;
2. creates an instance with its parameterless constructor, which may be private;
3. assigns every public property from the JSON through [`ReflectionHelper`](../core/utilities.md#reflectionhelper), which reaches private setters, `init` accessors and the backing field of a get-only auto-property.

Because it resolves the type from the JSON, the converter can read events polymorphically: an event serialized as `PostPublished` can be read back through a `DomainEventConverter< DomainEvent >`.

Every public property must be present in the JSON, or a `DeserializationException` is thrown. Its `JsonDocument` property holds the input, for logging.

### Using it

Put the converter on the event type:

```csharp
[ JsonConverter( typeof( DomainEventConverter< PostPublished > ) ) ]
public sealed class PostPublished : DomainEvent
{
    private PostPublished() { }

    public PostPublished( PostId postId ) => PostId = postId;

    public PostId PostId { get; private set; }
}

var json = JsonSerializer.Serialize( postPublished );
var copy = JsonSerializer.Deserialize< PostPublished >( json );
```

Or register it in the options, which keeps the domain model free of serialization attributes. `System.Text.Json` uses a `JsonConverter< T >` only for values whose declared type is exactly `T`, so register `DomainEventConverter< DomainEvent >` to handle any event declared as `DomainEvent`:

```csharp
var options = new JsonSerializerOptions
{
    Converters = { new DomainEventConverter< DomainEvent >() }
};

string json = JsonSerializer.Serialize< DomainEvent >( domainEvent, options );
DomainEvent? @event = JsonSerializer.Deserialize< DomainEvent >( json, options );   // a PostPublished again
```

### Things to know

- The type lookup by `TypeName` scans every type of every loaded assembly. It is a fallback for when the assembly-qualified name no longer matches, typically after the assembly version changed, and is slow on a cold start. Two event types with the same short name in different namespaces resolve to whichever is found first.
- Property names are written as declared. `JsonSerializerOptions.PropertyNamingPolicy` applies to nested values, not to the event's own properties.
- Every value is assigned through reflection, so the converter is not suitable for trimmed or Native AOT applications.

## `RootBlocks.Serialization.Newtonsoft.Json`

Package: `RootBlocks.Serialization.Newtonsoft.Json` (`net10.0`)

```bash
dotnet add package RootBlocks.Serialization.Newtonsoft.Json
```

`Microsoft.AspNetCore.JsonPatch` historically worked only with Newtonsoft.Json. In an **MVC** application that otherwise uses `System.Text.Json`, the usual fix is to add just the Newtonsoft JSON Patch input formatter, so that only `application/json-patch+json` bodies go through Newtonsoft. `NewtonsoftJsonPatchInputFormatterHelper.GetJsonPatchInputFormatter()` builds that formatter for you:

```csharp
using RootBlocks.Serialization.Newtonsoft.Json;

builder.Services.AddControllers( options =>
{
    options.InputFormatters.Insert( 0, NewtonsoftJsonPatchInputFormatterHelper.GetJsonPatchInputFormatter() );
} );
```

```csharp
[ HttpPatch( "{id}" ) ]
public IActionResult Patch( Guid id, [ FromBody ] JsonPatchDocument< PostPatch > patch ) { ... }
```

Insert it first so it is tried before the default `System.Text.Json` formatter.

### Minimal APIs: you do not need this package

Input formatters are an MVC feature, and minimal APIs never use them. Since .NET 10, use [`Microsoft.AspNetCore.JsonPatch.SystemTextJson`](https://www.nuget.org/packages/Microsoft.AspNetCore.JsonPatch.SystemTextJson) instead: its `JsonPatchDocument< T >` binds directly as an endpoint parameter. The [Blogs sample](../sample.md) does this:

```csharp
group.MapPatch( "/{id}", UpdatePost )
     .Accepts< JsonPatchDocument< Post > >( MediaTypeNames.Application.JsonPatch );
```

The same package also works in MVC, so a new application has little reason to bring in Newtonsoft for JSON Patch at all.
