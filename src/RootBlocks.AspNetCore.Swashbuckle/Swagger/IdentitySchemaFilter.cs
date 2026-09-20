namespace RootBlocks.AspNetCore.Swashbuckle.Swagger;

/// <summary>
/// Renders <see cref="Identity"/> subclasses as plain UUID strings instead of objects wrapping a
/// <see cref="Guid"/>, matching how they are serialized on the wire.
/// </summary>
public class IdentitySchemaFilter : ISchemaFilter
{
    #region Interface Implementations

    /// <inheritdoc />
    public void Apply( IOpenApiSchema schema, SchemaFilterContext context )
    {
        if ( !context.Type.IsSubclassOf( typeof( Identity ) ) )
            return;

        // Microsoft.OpenApi 2.x hands filters the read-only interface; only the concrete schema is mutable.
        if ( schema is not OpenApiSchema mutableSchema )
            return;

        mutableSchema.Type = JsonSchemaType.String;
        mutableSchema.Format = "uuid";
        mutableSchema.Properties?.Clear();
    }

    #endregion
}
