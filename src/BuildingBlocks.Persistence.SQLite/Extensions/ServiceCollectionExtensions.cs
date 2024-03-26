namespace BuildingBlocks.Persistence.SQLite.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSQLitePersistence(
        this IServiceCollection services,
        string databaseConnectionString
    )
    {
        const string factoryName = "System.Data.SQLite";
        DbProviderFactories.RegisterFactory( factoryName, SQLiteFactory.Instance );
        return services.AddPersistence( databaseConnectionString, factoryName );
    }
}
