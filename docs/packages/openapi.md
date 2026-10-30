# OpenAPI

Package: `RootBlocks.AspNetCore.OpenApi` (`net10.0`)

Transformers for the OpenAPI document that ASP.NET Core generates itself with `Microsoft.AspNetCore.OpenApi` and `AddOpenApi()`. There is no Swashbuckle dependency, so the document works with any UI: Scalar, Swagger UI, Redoc or none.

```bash
dotnet add package RootBlocks.AspNetCore.OpenApi
```

## Registering

Each transformer has its own extension method on `OpenApiOptions`, named for what it does. Take only the ones you want:

```csharp
using RootBlocks.AspNetCore.OpenApi.Extensions;

builder.Services.AddOpenApi( o =>
{
    o.AddJsonPatchExamples();
} );

var app = builder.Build();
app.MapOpenApi();   // serves /openapi/v1.json
```

There is deliberately no `AddRootBlocks()` umbrella method: a new transformer gets its own named method, so upgrading the package never changes your document behind your back.

| Method | Transformer | Kind |
|--------|-------------|------|
| `AddJsonPatchExamples()` | `JsonPatchExampleTransformer` | Operation |

Strongly-typed IDs are not handled here. [StronglyTypedIdentifier.AspNetCore](https://github.com/sebdesalvador/StronglyTypedIdentifier) provides `AddStronglyTypedIdentifiers()` for that, and the two combine in the same `AddOpenApi` call.

## JSON Patch examples

A JSON Patch body is an array of operations, and the schema ASP.NET Core generates for `JsonPatchDocument< T >` says little about what to send. `AddJsonPatchExamples()` attaches this example to the `application/json-patch+json` request body of every operation that accepts one:

```json
[
  { "op": "replace", "path": "/name", "value": "New Name" },
  { "op": "replace", "path": "/description", "value": "New Description" }
]
```

The transformer looks only at the media type. Declare it on the endpoint, or the body is described as `application/json` and left alone:

```csharp
using System.Net.Mime;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

group.MapPatch( "/{id}", UpdatePost )
     .Accepts< JsonPatchDocument< Post > >( MediaTypeNames.Application.JsonPatch );
```

The example is the same for every operation; it shows the shape of a patch, not the properties of your type. An operation without a JSON Patch body is not touched.

## XML comments and the CS9137 build error

In .NET 10, `Microsoft.AspNetCore.OpenApi` includes a source generator that lifts your XML doc comments (`<summary>`, `<param>`, `<returns>`) into the document. It works through C# interceptors, and a project that sets `GenerateDocumentationFile` fails to compile with **CS9137** unless the generator's namespace is opted in:

```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

The package sets this for you through a `buildTransitive` props file, so a project that gets `RootBlocks.AspNetCore.OpenApi` through a **package** reference, directly or transitively, needs nothing more. A project that references it as a **project** (as the sample and the tests in this repository do) does not receive `buildTransitive` assets and must add the property itself.

Two more things decide whether your comments reach the document:

- **Members must be visible.** The compiler only emits XML documentation for accessible members. With minimal APIs, write endpoint handlers as `public static` methods rather than lambdas or private methods, or their `summary` silently disappears from the document.
- **`GenerateDocumentationFile` must be on** in the project that declares the handlers. Add `<NoWarn>$(NoWarn);CS1591</NoWarn>` if you do not want a warning for every undocumented public member.

```csharp
public static class PostEndpoints
{
    public static RouteGroupBuilder MapPostEndpoints( this IEndpointRouteBuilder app )
    {
        var group = app.MapGroup( "/post" ).WithTags( "Post" );
        group.MapGet( "/{id}", GetPost ).WithName( nameof( GetPost ) );
        return group;
    }

    /// <summary>
    /// Gets a post by its ID.
    /// </summary>
    /// <param name="id">The ID of the post.</param>
    /// <returns>The post, or a 404 if it does not exist.</returns>
    public static async Task< Results< Ok< PostDto >, NotFound > > GetPost( PostId id, IPostQueries queries, CancellationToken ct )
        => ...;
}
```

## Adding a UI

The package does not serve a UI. The [Blogs sample](../sample.md) uses [Scalar](https://github.com/scalar/scalar):

```csharp
app.MapOpenApi();
app.MapScalarApiReference();   // Scalar.AspNetCore, served at /scalar
```
