namespace RootBlocks.AspNetCore.OpenApi.Tests;

[ Collection( nameof( OpenApiDocumentCollection ) ) ]
public class JsonPatchExampleTransformerTests( OpenApiDocumentFixture fixture )
{
    [ Fact ]
    public void JsonPatchOperation_CarriesAnExample()
    {
        Assert.True( PatchContent().TryGetProperty( "example", out var example ) );
        Assert.Equal( JsonValueKind.Array, example.ValueKind );
    }

    [ Fact ]
    public void JsonPatchExample_IsAnArrayOfPatchOperations()
    {
        var example = PatchContent().GetProperty( "example" );

        Assert.All(
            example.EnumerateArray(),
            operation =>
            {
                Assert.Equal( "replace", operation.GetProperty( "op" ).GetString() );
                Assert.StartsWith( "/", operation.GetProperty( "path" ).GetString() );
                Assert.NotNull( operation.GetProperty( "value" ).GetString() );
            }
        );
    }

    [ Fact ]
    public void NonPatchOperation_CarriesNoExample()
    {
        var content = fixture.Document.GetProperty( "paths" )
                              .GetProperty( "/blogs" )
                              .GetProperty( "post" )
                              .GetProperty( "requestBody" )
                              .GetProperty( "content" )
                              .GetProperty( "application/json" );

        Assert.False( content.TryGetProperty( "example", out _ ) );
    }

    private JsonElement PatchContent() =>
        fixture.Document.GetProperty( "paths" )
               .GetProperty( "/blogs/{id}" )
               .GetProperty( "patch" )
               .GetProperty( "requestBody" )
               .GetProperty( "content" )
               .GetProperty( JsonPatchExampleTransformer.JsonPatchMediaType );
}
