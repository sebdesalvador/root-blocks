namespace RootBlocks.AspNetCore.OpenApi.Extensions;

/// <summary>
/// Extension methods registering the RootBlocks transformers on the built-in OpenAPI generation.
/// </summary>
public static class OpenApiOptionsExtensions
{
    #region Public Methods

    /// <summary>
    /// Attaches a worked example to every operation accepting a JSON Patch document.
    /// </summary>
    /// <param name="options">The options to register the transformer on.</param>
    /// <returns>The same options, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddOpenApi( o => o.AddJsonPatchExamples() );
    /// </code>
    /// </example>
    public static OpenApiOptions AddJsonPatchExamples( this OpenApiOptions options )
    {
        if ( options is null ) throw new ArgumentNullException( nameof( options ) );

        options.AddOperationTransformer< JsonPatchExampleTransformer >();

        return options;
    }

    #endregion
}
