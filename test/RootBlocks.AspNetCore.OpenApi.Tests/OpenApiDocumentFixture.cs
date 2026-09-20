namespace RootBlocks.AspNetCore.OpenApi.Tests;

/// <summary>
/// Boots a minimal application and captures the document it generates, so the tests assert against
/// the real pipeline rather than a transformer in isolation.
/// </summary>
/// <remarks>
/// Each concrete fixture opts into a single extension, which lets every test class prove both that
/// its own transformer works and that it did not need the other one registered.
/// </remarks>
public abstract class OpenApiDocumentFixture : IAsyncLifetime
{
    #region Properties

    public JsonElement Document { get; private set; }

    #endregion

    #region Abstract Members

    protected abstract void Configure( OpenApiOptions options );

    #endregion

    #region Interface Implementations

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls( "http://127.0.0.1:0" );
        builder.Logging.ClearProviders();
        builder.Services.AddOpenApi( Configure );

        var app = builder.Build();

        app.MapOpenApi();
        app.MapPost( "/blogs", ( TestBlog blog ) => Results.Ok( blog ) );
        app.MapPatch( "/blogs/{id}", ( Guid id ) => Results.NoContent() )
           .Accepts< TestPatch >( JsonPatchExampleTransformer.JsonPatchMediaType );

        await app.StartAsync();

        try
        {
            using var client = new HttpClient();

            var json = await client.GetStringAsync( $"{app.Urls.First()}/openapi/v1.json" );

            Document = JsonDocument.Parse( json ).RootElement.Clone();
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #endregion

    #region Nested Types

    public class TestBlogId : Identity;

    public record TestBlog( TestBlogId Id, string Title );

    public record TestPatch( string Op, string Path, string Value );

    #endregion
}

/// <summary>A document generated with only <c>AddStronglyTypedIds()</c> registered.</summary>
public sealed class StronglyTypedIdsFixture : OpenApiDocumentFixture
{
    protected override void Configure( OpenApiOptions options ) => options.AddStronglyTypedIds();
}

/// <summary>A document generated with only <c>AddJsonPatchExamples()</c> registered.</summary>
public sealed class JsonPatchExamplesFixture : OpenApiDocumentFixture
{
    protected override void Configure( OpenApiOptions options ) => options.AddJsonPatchExamples();
}
