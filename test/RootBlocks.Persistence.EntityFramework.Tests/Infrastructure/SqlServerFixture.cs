namespace RootBlocks.Persistence.EntityFramework.Tests.Infrastructure;

/// <summary>
/// One SQL Server container for the whole test run; each test gets its own database. Service Broker is never
/// enabled.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    #region Fields

    private readonly MsSqlContainer _container = new MsSqlBuilder( "mcr.microsoft.com/mssql/server:2022-latest" ).Build();

    #endregion

    #region Public Methods

    public string NewDatabase()
        => new SqlConnectionStringBuilder( _container.GetConnectionString() )
        {
            InitialCatalog = $"outbox_{Guid.NewGuid():N}"
        }.ConnectionString;

    #endregion

    #region Interface Implementations

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    #endregion
}

[ CollectionDefinition( NAME ) ]
public class SqlServerCollection : ICollectionFixture< SqlServerFixture >
{
    public const string NAME = "SQL Server";
}
