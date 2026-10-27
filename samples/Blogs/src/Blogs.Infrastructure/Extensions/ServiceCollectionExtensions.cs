namespace Blogs.Infrastructure.Extensions;

[ ExcludeFromCodeCoverage ]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure( this IServiceCollection services,
                                                        IConfiguration configuration )
    {
        var databaseConnectionString = configuration.GetConnectionString( "DefaultConnection" )!;
        return services.AddMessaging()
                       .AddSqlClientPersistence( databaseConnectionString )
                       .AddScoped< IPersonQueries, PersonQueries >()
                       .AddScoped< IBlogQueries, BlogQueries >()
                       .AddScoped< IPostQueries, PostQueries >()
                       .AddContext( databaseConnectionString )
                       .AddOutbox< Context >( configuration )
                       .AddScoped< IUnitOfWork, Persistence.UnitOfWork >()
                       .AddScoped< IPersonRepository, PersonRepository >()
                       .AddScoped< IBlogRepository, BlogRepository >()
                       .AddScoped< IPostRepository, PostRepository >();
    }

    private static IServiceCollection AddContext( this IServiceCollection services, string connectionString )
        => services.AddDbContext< Context >(
            o =>
                o.UseLazyLoadingProxies()
                 .UseSqlServer( connectionString )
        );
}
