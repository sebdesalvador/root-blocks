namespace RootBlocks.AspNetCore.Swashbuckle.Swagger;

public class JsonPatchDocumentFilter : IOperationFilter
{
    public void Apply( OpenApiOperation operation, OperationFilterContext context )
    {
        if ( operation.RequestBody is not null
          && operation.RequestBody.Content.TryGetValue( "application/json-patch+json", out var content ) )
        {
            content.Example = new OpenApiString( @"[
                    {
                        ""op"": ""replace"",
                        ""path"": ""/name"",
                        ""value"": ""New Name""
                    },
                    {
                        ""op"": ""replace"",
                        ""path"": ""/description"",
                        ""value"": ""New Description""
                    }
                ]" );
        }
    }
}
