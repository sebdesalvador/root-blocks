namespace BuildingBlocks.Persistence.Npgsql.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNpgsqlPersistence(
        this IServiceCollection services,
        string databaseConnectionString
    )
    {
        const string factoryName = "Npgsql";
        DbProviderFactories.RegisterFactory( factoryName, NpgsqlFactory.Instance );
        return services.AddPersistence( databaseConnectionString, factoryName );
    }
}
