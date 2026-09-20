namespace RootBlocks.AspNetCore.OpenApi;

/// <summary>
/// Attaches a worked example to operations accepting a JSON Patch document, since the generated
/// schema on its own says little about the shape of a patch.
/// </summary>
public sealed class JsonPatchExampleTransformer : IOpenApiOperationTransformer
{
    #region Fields

    /// <summary>
    /// The media type a JSON Patch request body is sent as.
    /// </summary>
    public const string JsonPatchMediaType = "application/json-patch+json";

    private const string Example = """
                                   [
                                     { "op": "replace", "path": "/name", "value": "New Name" },
                                     { "op": "replace", "path": "/description", "value": "New Description" }
                                   ]
                                   """;

    #endregion

    #region Interface Implementations

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if ( operation.RequestBody?.Content is null )
            return Task.CompletedTask;

        if ( !operation.RequestBody.Content.TryGetValue( JsonPatchMediaType, out var content ) )
            return Task.CompletedTask;

        content.Example = JsonNode.Parse( Example );

        return Task.CompletedTask;
    }

    #endregion
}
