namespace RootBlocks.AspNetCore.OpenApi.Tests;

[ Collection( nameof( OpenApiDocumentCollection ) ) ]
public class IdentitySchemaTransformerTests( OpenApiDocumentFixture fixture )
{
    [ Fact ]
    public void IdentitySchema_IsAUuidString()
    {
        var schema = Schemas().GetProperty( nameof( OpenApiDocumentFixture.TestBlogId ) );

        Assert.Equal( "string", schema.GetProperty( "type" ).GetString() );
        Assert.Equal( "uuid", schema.GetProperty( "format" ).GetString() );
    }

    [ Fact ]
    public void IdentitySchema_DoesNotLeakTheWrappedValueAsAProperty()
    {
        var schema = Schemas().GetProperty( nameof( OpenApiDocumentFixture.TestBlogId ) );

        // Without the transformer the generator emits an empty schema, which reads as "any" to a
        // client generator; with it, the schema must be the uuid string and nothing else.
        Assert.False( schema.TryGetProperty( "properties", out _ ) );
    }

    [ Fact ]
    public void NonIdentitySchema_IsLeftAlone()
    {
        var schema = Schemas().GetProperty( nameof( OpenApiDocumentFixture.TestBlog ) );

        Assert.Equal( "object", schema.GetProperty( "type" ).GetString() );
        Assert.True( schema.TryGetProperty( "properties", out _ ) );
    }

    private JsonElement Schemas() => fixture.Document.GetProperty( "components" ).GetProperty( "schemas" );
}
