# The Blogs sample

[`samples/Blogs`](../samples/Blogs) is a reference application that uses RootBlocks from the domain model through to the HTTP API. When you are unsure how two pieces fit together, look here first.

## Running it

You need the .NET 10 SDK and a SQL Server instance. The API reads its connection string from `Blogs.Api/appsettings.Development.json`, and migrations read it from `Blogs.Infrastructure/appsettings.json`. Both point to a `blogs` database on `localhost` as `sa`. Change them to suit your setup, or override them with `ConnectionStrings__DefaultConnection` (or an `appsettings.Local.json` next to the Infrastructure one for migrations).

```bash
# create the database from the migrations (run from the repository root)
dotnet ef database update --project samples/Blogs/src/Blogs.Infrastructure

# run the API
dotnet run --project samples/Blogs/src/Blogs.Api/
```

Then open:

- `/scalar` for the Scalar API reference UI
- `/openapi/v1.json` for the raw OpenAPI document
- `/health` for the health check

The `.http` files in `Blogs.Api` (`Blogs.Person.Api.http`, `Blogs.Blog.Api.http`, `Blogs.Post.Api.http`) contain a ready-made scenario for the JetBrains or VS Code HTTP clients. They create, read, update and delete people, blogs and posts, add and remove tags and comments, and check each step.

## Layout

```
samples/Blogs/src
├── Blogs.Core            domain model + read-model contracts       → RootBlocks, StronglyTypedIdentifier
├── Blogs.Application     MediatR commands and handlers             → Blogs.Core
├── Blogs.Infrastructure  EF Core, repositories, Dapper queries, DI  → RootBlocks.Persistence.EntityFramework,
│                                                                      .Persistence.SqlClient, .Messaging.MediatR
└── Blogs.Api             minimal API endpoints, composition root    → RootBlocks.AspNetCore.OpenApi, .Logging.Serilog
```

### Blogs.Core

- **Aggregates**: `Person`, `Blog` and `Post`. `Post` owns `Comment` (a child entity) and `Tag` (a value object), and has a `PostStatus` enumeration.
- **IDs**: `public partial struct PostId : IStronglyTypedIdentifier< Guid >;`. Each aggregate passes `XId.New()` to the `Entity<T>` constructor.
- **Domain events**: `PersonRegistered`, `PostPublished` and `CommentAdded`.
- **Domain exceptions**: `PostException` with `PostExceptionCode : Enumeration`, and the same pattern for blogs and people.
- **Repository interfaces**: `IPostRepository : IRepository< Post >` and so on. They have only the methods the handlers need.
- **Read model**: DTOs such as `PostDto` and query interfaces such as `IPostQueries`, so the API can read without going through aggregates.

### Blogs.Application

There is one command record and one handler per use case (`CreatePostCommand`, `PublishPostCommand`, `AddTagCommand`, ...). Each handler follows the same steps: load the aggregate, call a domain method, register the aggregate with the unit of work, commit. The project has no infrastructure references.

### Blogs.Infrastructure

- `Context : DatabaseContextWithOutbox` applies every configuration in the assembly, and maps the strongly-typed IDs with `UseStronglyTypedIdentifiers()` in `ConfigureConventions`.
- Configurations derive from `EntityConfiguration`. `Person` uses `SoftDeletableEntityConfiguration` (with a unique e-mail index that ignores deleted rows), `PostStatus` uses `EnumerationConfiguration`, and `Tag` uses `ValueObjectConfiguration`.
- `UnitOfWork` derives from the RootBlocks `UnitOfWork`.
- Repositories use EF. Queries use Dapper over `IConnectionFactory`, with multi-result SQL.
- `AddInfrastructure( connectionString )` wires up `AddMessaging()`, `AddSqlClientPersistence()`, the context (with lazy-loading proxies), the unit of work, the repositories and the queries.
- `Migrations/` contains the initial migration. `DesignTimeContextFactory` derives from `DesignTimeDatabaseContextFactory`.

### Blogs.Api

The API uses **minimal APIs, not MVC**. Each aggregate has an `Endpoints/XEndpoints.cs` with a `MapXEndpoints()` extension that builds a `MapGroup` and maps `public static` handler methods.

- The handlers are `public` because the XML doc comments only reach the OpenAPI document for visible members (see [OpenAPI](packages/openapi.md#xml-comments-and-the-cs9137-build-error)).
- Strongly-typed IDs bind directly from the route (`PostId id`) through their generated `TryParse`.
- `EntityNotFoundException<T>` is caught per endpoint and turned into `404`.
- `PATCH /post/{id}` takes a `JsonPatchDocument<Post>` from `Microsoft.AspNetCore.JsonPatch.SystemTextJson` and declares `.Accepts<...>( MediaTypeNames.Application.JsonPatch )`, so `AddJsonPatchExamples()` can attach its example.
- `AddOpenApi` registers `AddStronglyTypedIdentifiers()` (from StronglyTypedIdentifier.AspNetCore) and `AddJsonPatchExamples()`. Scalar serves the UI.
- Serilog is configured from `appsettings.json` through `UseSerilog()`.
