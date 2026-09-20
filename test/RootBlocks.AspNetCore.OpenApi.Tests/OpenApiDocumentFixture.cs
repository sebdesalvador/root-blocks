namespace RootBlocks.AspNetCore.OpenApi.Tests;

/// <summary>
/// Boots a minimal application wired with <c>AddRootBlocks()</c> and captures the document it
/// generates, so the tests assert against the real pipeline rather than a transformer in isolation.
/// </summary>
public sealed class OpenApiDocumentFixture : IAsyncLifetime
{
    #region Properties

    public JsonElement Document { get; private set; }

    #endregion

    #region Interface Implementations

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls( "http://127.0.0.1:0" );
        builder.Logging.ClearProviders();
        builder.Services.AddOpenApi( o => o.AddRootBlocks() );

        var app = builder.Build();

        app.MapOpenApi();
        app.MapPost( "/blogs", ( TestBlog blog ) => Results.Ok( blog ) );
        app.MapPatch( "/blogs/{id}", ( Guid id ) => Results.NoContent() )
           .Accepts< TestPatch >( JsonPatchExampleTransformer.JsonPatchMediaType );

        await app.StartAsync();

        try
        {
            using var client = new HttpClient();

            var address = app.Urls.First();
            var json = await client.GetStringAsync( $"{address}/openapi/v1.json" );

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
