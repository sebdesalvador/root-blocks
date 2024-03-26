namespace BuildingBlocks.AspNetCore.Swashbuckle.Swagger;

public class IdentitySchemaFilter : ISchemaFilter
{
    public void Apply( OpenApiSchema schema, SchemaFilterContext context )
    {
        if ( !context.Type.IsSubclassOf( typeof( Identity ) ) )
            return;

        schema.Type = "string";
        schema.Format = "uuid";
        schema.Properties?.Clear();
    }
}
