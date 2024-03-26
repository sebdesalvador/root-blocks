namespace BuildingBlocks.Persistence.MySqlClient.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlClientPersistence(
        this IServiceCollection services,
        string databaseConnectionString
    )
    {
        const string factoryName = "MySql.Data.MySqlClient";
        DbProviderFactories.RegisterFactory( factoryName, MySqlClientFactory.Instance );
        return services.AddPersistence( databaseConnectionString, factoryName );
    }
}
