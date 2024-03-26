namespace BuildingBlocks.Persistence.EntityFramework;

public abstract class DesignTimeDatabaseContextFactory< T > : IDesignTimeDbContextFactory< T >
    where T : DbContext
{
    protected abstract T InitializeContext( DbContextOptions< T > options );

    public T CreateDbContext( string[] args )
    {
        var basePath = Directory.GetCurrentDirectory();
        var environmentName = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" );
        Console.WriteLine( $"\nDesignTimeContextFactory.Create(string[]):\n\tBase Path: {basePath}\n\tEnvironmentVariable: {environmentName}" );
        return Create( basePath, environmentName );
    }

    private T Create( string basePath, string? environmentName )
    {
        const string appSettingsFileName = "appsettings";
        var builder = new ConfigurationBuilder().SetBasePath( basePath )
                                                .AddJsonFile( $"{appSettingsFileName}.json", false );

        if ( environmentName is not null )
            builder.AddJsonFile( $"{appSettingsFileName}.{environmentName}.json", true );

        builder.AddJsonFile( $"{appSettingsFileName}.Local.json", true )
               .AddEnvironmentVariables();
        var config = builder.Build();
        var connectionString = config.GetConnectionString( "DefaultConnection" );

        if ( string.IsNullOrWhiteSpace( connectionString ) )
            throw new InvalidOperationException( "Could not find a connection string named 'DefaultConnection'." );

        var optionsBuilder = new DbContextOptionsBuilder< T >();
        optionsBuilder.UseSqlServer( connectionString )
                      .EnableSensitiveDataLogging();
        Console.WriteLine( $"\nDesignTimeContextFactory.Create(string):\n\tConnection string: {connectionString}\n" );
        var options = optionsBuilder.Options;
        return InitializeContext( options );
    }
}
