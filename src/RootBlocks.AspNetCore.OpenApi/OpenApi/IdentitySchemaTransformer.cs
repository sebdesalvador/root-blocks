namespace RootBlocks.AspNetCore.OpenApi;

/// <summary>
/// Renders <see cref="Identity"/> subclasses as plain UUID strings instead of objects wrapping a
/// <see cref="Guid"/>, matching how they are serialized on the wire.
/// </summary>
/// <remarks>
/// Without this, the document generator sees the custom JSON converter behind an identity, cannot
/// infer a shape from it and emits an empty schema, which client generators read as "any".
/// </remarks>
public sealed class IdentitySchemaTransformer : IOpenApiSchemaTransformer
{
    #region Interface Implementations

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if ( !typeof( Identity ).IsAssignableFrom( context.JsonTypeInfo.Type ) )
            return Task.CompletedTask;

        schema.Type = JsonSchemaType.String;
        schema.Format = "uuid";
        schema.Properties?.Clear();

        return Task.CompletedTask;
    }

    #endregion
}
