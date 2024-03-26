namespace BuildingBlocks.Persistence.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseConnectionString,
        string factoryName
    ) => services.AddSingleton< IConnectionFactory >( new ConnectionFactory( databaseConnectionString, factoryName ) );
}
