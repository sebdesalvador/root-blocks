namespace RootBlocks.AspNetCore.OpenApi;

/// <summary>
/// Describes route and query parameters of an <see cref="Identity"/> type as UUID strings.
/// </summary>
/// <remarks>
/// Schema transformers never see these. A parameter bound through <c>TryParse</c> is described from
/// its binding source rather than from its JSON contract, so the generator settles for a bare
/// string and the <c>uuid</c> format is lost.
/// </remarks>
public sealed class IdentityParameterTransformer : IOpenApiOperationTransformer
{
    #region Interface Implementations

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if ( operation.Parameters is null )
            return Task.CompletedTask;

        foreach ( var description in context.Description.ParameterDescriptions )
        {
            if ( !typeof( Identity ).IsAssignableFrom( description.Type ) )
                continue;

            var parameter = operation.Parameters.FirstOrDefault( p => p.Name == description.Name );

            if ( parameter?.Schema is not OpenApiSchema schema )
                continue;

            schema.Type = JsonSchemaType.String;
            schema.Format = "uuid";
        }

        return Task.CompletedTask;
    }

    #endregion
}
