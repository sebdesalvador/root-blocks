namespace RootBlocks.Persistence;

public class ConnectionFactory : IConnectionFactory
{
    private readonly DbProviderFactory _connectionFactory;

    public string ConnectionString { get; }

    public ConnectionFactory( string connectionString, string factoryName )
    {
        ConnectionString = connectionString;
        _connectionFactory = DbProviderFactories.GetFactory( factoryName );
    }

    public DbConnection CreateConnection()
    {
        var connection = _connectionFactory.CreateConnection();

        if ( connection == null )
            throw new InvalidOperationException( "Connection could not be created." );

        connection.ConnectionString = ConnectionString;
        return connection;
    }
}
