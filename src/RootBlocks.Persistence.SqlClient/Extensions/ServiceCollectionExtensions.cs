namespace RootBlocks.Persistence.SqlClient.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlClientPersistence(
        this IServiceCollection services,
        string databaseConnectionString
    )
    {
        const string factoryName = "Microsoft.Data.SqlClient";
        DbProviderFactories.RegisterFactory( factoryName, Microsoft.Data.SqlClient.SqlClientFactory.Instance );
        return services.AddPersistence( databaseConnectionString, factoryName );
    }
}
