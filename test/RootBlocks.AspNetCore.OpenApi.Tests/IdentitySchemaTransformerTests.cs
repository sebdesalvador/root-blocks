namespace RootBlocks.AspNetCore.OpenApi.Tests;

public class IdentitySchemaTransformerTests( StronglyTypedIdsFixture fixture )
    : IClassFixture< StronglyTypedIdsFixture >
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

    [ Fact ]
    public void AddStronglyTypedIds_DoesNotDragInTheJsonPatchExample()
    {
        var content = fixture.Document.GetProperty( "paths" )
                             .GetProperty( "/blogs/{id}" )
                             .GetProperty( "patch" )
                             .GetProperty( "requestBody" )
                             .GetProperty( "content" )
                             .GetProperty( JsonPatchExampleTransformer.JsonPatchMediaType );

        Assert.False( content.TryGetProperty( "example", out _ ) );
    }

    private JsonElement Schemas() => fixture.Document.GetProperty( "components" ).GetProperty( "schemas" );
}
