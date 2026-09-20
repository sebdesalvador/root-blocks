namespace RootBlocks.AspNetCore.OpenApi.Tests;

/// <summary>
/// Shares one generated document across the test classes, so the application boots once.
/// </summary>
[ CollectionDefinition( nameof( OpenApiDocumentCollection ) ) ]
public class OpenApiDocumentCollection : ICollectionFixture< OpenApiDocumentFixture >;
