# Getting started

This guide builds one aggregate, `Post`, through all four layers: domain, persistence, application and API. It follows the layout of the [Blogs sample](sample.md), which you can open to see the complete version of each step.

It assumes SQL Server, EF Core, MediatR and an ASP.NET Core minimal API. Each of these choices can be swapped out. The core `RootBlocks` package has no dependency on any of them.

## 1. Create the projects

```bash
dotnet new classlib -n MyApp.Core
dotnet new classlib -n MyApp.Application
dotnet new classlib -n MyApp.Infrastructure
dotnet new web      -n MyApp.Api

dotnet add MyApp.Core           package RootBlocks
dotnet add MyApp.Core           package StronglyTypedIdentifier            # optional, for typed IDs
dotnet add MyApp.Infrastructure package RootBlocks.Persistence.EntityFramework
dotnet add MyApp.Infrastructure package RootBlocks.Persistence.SqlClient
dotnet add MyApp.Infrastructure package RootBlocks.Messaging.MediatR
dotnet add MyApp.Infrastructure package StronglyTypedIdentifier.EntityFrameworkCore
dotnet add MyApp.Infrastructure package Dapper
dotnet add MyApp.Application    package MediatR
```

Then add the references: Application → Core, Infrastructure → Core, Api → Application and Infrastructure.

| Layer | Contains | References |
|-------|----------|------------|
| Core | Aggregates, value objects, domain events, repository interfaces, read-model DTOs and query interfaces | `RootBlocks` |
| Application | Commands and their handlers | Core |
| Infrastructure | EF configurations, `DbContext`, repositories, unit of work, raw SQL queries, DI registration | Core, RootBlocks persistence and messaging packages |
| Api | Endpoints, request models, composition root | Application, Infrastructure |

## 2. Model the domain

### An identifier

RootBlocks does not provide identifier types. `Entity<T>` works with any `T : IEquatable<T>`. A plain `Guid` is enough to get started. The rest of this guide uses a strongly-typed ID from [StronglyTypedIdentifier](https://github.com/sebdesalvador/StronglyTypedIdentifier):

```csharp
public partial struct PostId : IStronglyTypedIdentifier< Guid >;
```

### An enumeration

An `Enumeration` is a class that works like an enum but is keyed by a `Guid` and can be stored as a lookup table. Declare each value as a `public static readonly` field. `GetAll<T>()` finds the values by reflecting over those fields.

```csharp
public class PostStatus : Enumeration
{
    public static readonly PostStatus Draft     = new( Guid.Parse( "00000000-0000-0000-0000-000000000001" ), nameof( Draft ) );
    public static readonly PostStatus Published = new( Guid.Parse( "00000000-0000-0000-0000-000000000002" ), nameof( Published ) );

    private PostStatus( Guid id, string name ) : base( id, name ) { }
}
```

### A value object

A value object compares equal to another when its atomic values are equal.

```csharp
public class Tag : ValueObject
{
    public string Value { get; private init; }

    public Tag( string value ) => Value = value;

    protected override IEnumerable< object > GetAtomicValues()
    {
        yield return Value;
    }
}
```

### A domain event

```csharp
public class PostPublished( PostId postId ) : DomainEvent
{
    public PostId PostId { get; } = postId;
}
```

### A domain exception

A `DomainException` carries an `Enumeration` as its error code, so callers can branch on the code instead of parsing the message.

```csharp
public class PostExceptionCode : Enumeration
{
    public static readonly PostExceptionCode AlreadyPublished = new( Guid.Parse( "00000000-0000-0000-0000-000000000006" ), "ALREADY_PUBLISHED" );

    private PostExceptionCode( Guid id, string name ) : base( id, name ) { }
}

public class PostException( PostExceptionCode code ) : DomainException( code );
```

### The aggregate root

```csharp
public class Post : Entity< PostId >, IAggregateRoot
{
    private readonly List< Tag > _tags = [ ];

    public string Title { get; private set; }
    public DateTime? PublishedOn { get; private set; }
    public Guid PostStatusId { get; private set; }
    public virtual IReadOnlyCollection< Tag > Tags => _tags.AsReadOnly();

    public Post( string title ) : base( PostId.New() )
    {
        Title = title;
        PostStatusId = PostStatus.Draft.Id;
    }

    public void Publish()
    {
        if ( PublishedOn is not null )
            throw new PostException( PostExceptionCode.AlreadyPublished );

        PublishedOn = DateTime.UtcNow;
        PostStatusId = PostStatus.Published.Id;
        AddDomainEvent( new PostPublished( Id ) );
    }

    public void AddTag( string value ) => _tags.Add( new Tag( value ) );
}
```

The ID is assigned in the constructor. RootBlocks never lets the database generate entity IDs. `CreatedOn` and `LastModifiedOn` come from `Entity<T>` and are filled in by the `DatabaseContext` when it saves.

### The repository interface

```csharp
public interface IPostRepository : IRepository< Post >
{
    Task< Post > GetByIdAsync( PostId id, CancellationToken cancellationToken = default );
}
```

`IRepository<T>` only accepts types that implement `IAggregateRoot`. This keeps repositories at the aggregate level.

## 3. Persist it with EF Core

### Entity configurations

Derive from the RootBlocks base configurations and call `base.Configure` first. Each base configuration names the table after the type, makes `Id` the key without database generation, and maps the audit columns. `EntityConfiguration` also adds a `RowVersion` concurrency token.

```csharp
public class PostConfiguration : EntityConfiguration< Post, PostId >
{
    public override void Configure( EntityTypeBuilder< Post > builder )
    {
        base.Configure( builder );

        builder.Property( p => p.Title ).HasMaxLength( 256 ).IsRequired();
        builder.HasOne< PostStatus >().WithMany().HasForeignKey( p => p.PostStatusId );
        builder.Metadata.FindNavigation( nameof( Post.Tags ) )!
               .SetPropertyAccessMode( PropertyAccessMode.Field );
    }
}

public class PostStatusConfiguration : EnumerationConfiguration< PostStatus >; // also seeds every value

public class TagConfiguration : ValueObjectConfiguration< Tag >
{
    public override void Configure( EntityTypeBuilder< Tag > builder )
    {
        base.Configure( builder ); // adds a shadow Guid "Id" key
        builder.Property( t => t.Value ).HasMaxLength( 256 ).IsRequired();
        builder.HasOne< Post >().WithMany( p => p.Tags ).HasForeignKey( "PostId" );
    }
}
```

### The context

```csharp
public sealed class Context( DbContextOptions options ) : DatabaseContextWithOutbox( options )
{
    public DbSet< Post > Posts { get; init; } = null!;

    protected override void ConfigureConventions( ModelConfigurationBuilder configurationBuilder )
    {
        // Maps every strongly-typed ID in the Core assembly (from StronglyTypedIdentifier.EntityFrameworkCore).
        configurationBuilder.UseStronglyTypedIdentifiers( typeof( PostId ).Assembly );
        base.ConfigureConventions( configurationBuilder );
    }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.ApplyConfigurationsFromAssembly( Assembly.GetExecutingAssembly() );
        base.OnModelCreating( modelBuilder ); // must run after your configurations
    }
}
```

Call `base.OnModelCreating` after applying your configurations. The base method sets the UTC converters on every `DateTime` column and adds `DeletedOn` to unique indexes. It can only do that for entities that are already in the model.

Use `DatabaseContext` instead of `DatabaseContextWithOutbox` if you do not want an `Outbox` table. See [Entity Framework persistence](packages/entity-framework.md#the-outbox) for the differences.

### The unit of work and the repository

```csharp
public class UnitOfWork( Context context, IEventPublisher eventPublisher )
    : RootBlocks.Persistence.EntityFramework.UnitOfWork( context, eventPublisher );

public class PostRepository( Context context ) : IPostRepository
{
    public async Task< Post > GetByIdAsync( PostId id, CancellationToken cancellationToken = default )
        => await context.Posts.SingleOrDefaultAsync( p => p.Id == id, cancellationToken )
        ?? throw new EntityNotFoundException< Post >( nameof( Post.Id ), id );
}
```

### Migrations

Add a design-time factory so `dotnet ef` can create the context. It reads `ConnectionStrings:DefaultConnection` from `appsettings.json`, then `appsettings.{ASPNETCORE_ENVIRONMENT}.json`, then `appsettings.Local.json`, then environment variables.

```csharp
public class DesignTimeContextFactory : DesignTimeDatabaseContextFactory< Context >
{
    protected override Context InitializeContext( DbContextOptions< Context > options ) => new( options );
}
```

```bash
dotnet ef migrations add Initial --project MyApp.Infrastructure
dotnet ef database update     --project MyApp.Infrastructure
```

## 4. Read with raw SQL

The read side bypasses EF and returns DTOs straight from SQL through `IConnectionFactory`. It is registered by the database driver package.

```csharp
public class PostQueries( IConnectionFactory connectionFactory ) : IPostQueries
{
    public async Task< PostDto > GetPostAsync( PostId id, CancellationToken cancellationToken = default )
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync< PostDto >(
                   "SELECT Id, Title, PublishedOn FROM Post WHERE Id = @Id",
                   new { Id = id.Value } )
            ?? throw new EntityNotFoundException< Post >( nameof( Post.Id ), id );
    }
}
```

## 5. Register everything

```csharp
public static IServiceCollection AddInfrastructure( this IServiceCollection services, string connectionString )
    => services.AddMessaging()                               // IEventPublisher via MediatR
               .AddSqlClientPersistence( connectionString )  // IConnectionFactory for SQL Server
               .AddDbContext< Context >( o => o.UseSqlServer( connectionString ) )
               .AddScoped< IUnitOfWork, UnitOfWork >()
               .AddScoped< IPostRepository, PostRepository >()
               .AddScoped< IPostQueries, PostQueries >();
```

`AddMessaging()` registers the publisher but not MediatR itself. Register MediatR in the application layer, and make sure the assembly scan includes your notification handlers:

```csharp
services.AddMediatR( cfg => cfg.RegisterServicesFromAssembly( typeof( PublishPostCommand ).Assembly ) );
```

## 6. Handle a command

```csharp
public record PublishPostCommand( PostId PostId ) : IRequest;

public class PublishPostCommandHandler( IUnitOfWork unitOfWork, IPostRepository posts )
    : IRequestHandler< PublishPostCommand >
{
    public async Task Handle( PublishPostCommand request, CancellationToken cancellationToken )
    {
        var post = await posts.GetByIdAsync( request.PostId, cancellationToken );
        post.Publish();
        unitOfWork.RegisterDirty( post );
        await unitOfWork.CommitAsync( cancellationToken );
    }
}
```

`CommitAsync` collects the aggregate's domain events, stores them in the outbox when the context has one, saves, and throws `ConcurrencyException` if no row changed. It then publishes each event through `IEventPublisher`.

## 7. React to the event

Domain events implement MediatR's `INotification`, so a handler is a normal notification handler:

```csharp
public class NotifySubscribersWhenPostPublished : INotificationHandler< PostPublished >
{
    public Task Handle( PostPublished notification, CancellationToken cancellationToken )
    {
        // send emails, update a projection, ...
        return Task.CompletedTask;
    }
}
```

## 8. Expose it

```csharp
var builder = WebApplication.CreateBuilder( args );
builder.Services.AddMediatR( cfg => cfg.RegisterServicesFromAssembly( typeof( PublishPostCommand ).Assembly ) );
builder.Services.AddInfrastructure( builder.Configuration.GetConnectionString( "DefaultConnection" )! );

var app = builder.Build();

app.MapPost( "/post/{id}/publish", async ( PostId id, IMediator mediator, CancellationToken ct ) =>
{
    await mediator.Send( new PublishPostCommand( id ), ct );
    return TypedResults.NoContent();
} );

app.Run();
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Initial Catalog=myapp;User ID=sa;Password=...;TrustServerCertificate=true;"
  }
}
```

## Next steps

- [Domain model](core/domain-model.md) explains each building block in detail.
- [Entity Framework persistence](packages/entity-framework.md) covers soft delete, concurrency, transactions and the outbox.
- [OpenAPI](packages/openapi.md) explains how to document the API.
- [The Blogs sample](sample.md) shows all of this working together.
