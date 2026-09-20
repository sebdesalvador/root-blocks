namespace RootBlocks.AspNetCore.Swashbuckle.Swagger;

/// <summary>
/// Attaches a worked example to operations accepting a JSON Patch document, since the generated
/// schema on its own says little about the shape of a patch.
/// </summary>
public class JsonPatchDocumentFilter : IOperationFilter
{
    #region Fields

    private const string JsonPatchMediaType = "application/json-patch+json";

    private static readonly string Example = """
                                             [
                                               { "op": "replace", "path": "/name", "value": "New Name" },
                                               { "op": "replace", "path": "/description", "value": "New Description" }
                                             ]
                                             """;

    #endregion

    #region Interface Implementations

    /// <inheritdoc />
    public void Apply( OpenApiOperation operation, OperationFilterContext context )
    {
        if ( operation.RequestBody?.Content is null )
            return;

        if ( !operation.RequestBody.Content.TryGetValue( JsonPatchMediaType, out var content ) )
            return;

        // Microsoft.OpenApi 2.x models examples as JsonNode, so the patch renders as a real array
        // rather than as a quoted string.
        content.Example = JsonNode.Parse( Example );
    }

    #endregion
}
