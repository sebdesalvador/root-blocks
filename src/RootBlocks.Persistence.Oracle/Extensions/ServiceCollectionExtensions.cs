namespace RootBlocks.Persistence.Oracle.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOraclePersistence(
        this IServiceCollection services,
        string databaseConnectionString
    )
    {
        const string factoryName = "Oracle.ManagedDataAccess.Client";
        DbProviderFactories.RegisterFactory( factoryName, OracleClientFactory.Instance );
        return services.AddPersistence( databaseConnectionString, factoryName );
    }
}
