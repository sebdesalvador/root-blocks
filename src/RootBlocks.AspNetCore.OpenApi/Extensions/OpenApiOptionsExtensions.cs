namespace RootBlocks.AspNetCore.OpenApi.Extensions;

/// <summary>
/// Extension methods registering the RootBlocks transformers on the built-in OpenAPI generation.
/// </summary>
public static class OpenApiOptionsExtensions
{
    #region Public Methods

    /// <summary>
    /// Registers every RootBlocks transformer, so the generated document describes the types this
    /// library introduces accurately.
    /// </summary>
    /// <param name="options">The options to register the transformers on.</param>
    /// <returns>The same options, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddOpenApi( o => o.AddRootBlocks() );
    /// </code>
    /// </example>
    public static OpenApiOptions AddRootBlocks( this OpenApiOptions options )
    {
        if ( options is null ) throw new ArgumentNullException( nameof( options ) );

        options.AddSchemaTransformer< IdentitySchemaTransformer >();
        options.AddOperationTransformer< JsonPatchExampleTransformer >();

        return options;
    }

    #endregion
}
