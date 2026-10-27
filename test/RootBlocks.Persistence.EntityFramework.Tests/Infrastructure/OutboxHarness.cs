namespace RootBlocks.Persistence.EntityFramework.Tests.Infrastructure;

/// <summary>
/// A host with two modules, <see cref="SalesContext" /> and <see cref="BillingContext" />, on a fresh database,
/// with a clock the test moves.
/// </summary>
public sealed class OutboxHarness : IAsyncDisposable
{
    #region Properties

    public FakeTimeProvider Time { get; } = new( DateTimeOffset.UtcNow );
    public RecordingEventPublisher Publisher { get; } = new();
    public OutboxOptions Options => Services.GetRequiredService< Microsoft.Extensions.Options.IOptions< OutboxOptions > >().Value;
    public ServiceProvider Services { get; private set; } = null!;

    #endregion

    #region Public Methods

    public static async Task< OutboxHarness > CreateAsync(
        SqlServerFixture sqlServer,
        Dictionary< string, string? >? settings = null
    )
    {
        var harness = new OutboxHarness();
        var connectionString = sqlServer.NewDatabase();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection( settings ?? [ ] ).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton< TimeProvider >( harness.Time );
        services.AddSingleton< IEventPublisher >( harness.Publisher );
        services.AddDbContext< SalesContext >( o => o.UseSqlServer( connectionString ) );
        // Lazy-loading proxies, as the Blogs sample uses, must be able to materialize outbox messages too.
        services.AddDbContext< BillingContext >( o => o.UseLazyLoadingProxies().UseSqlServer( connectionString ) );
        services.AddOutbox< SalesContext >( configuration );
        services.AddOutbox< BillingContext >( configuration );
        services.AddScoped< ModuleUnitOfWork< SalesContext > >();
        services.AddScoped< ModuleUnitOfWork< BillingContext > >();
        harness.Services = services.BuildServiceProvider();

        await using var scope = harness.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService< SalesContext >().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService< BillingContext >().GetService< IRelationalDatabaseCreator >().CreateTablesAsync();
        return harness;
    }

    /// <summary>Places orders in <typeparamref name="TContext" /> and commits them in one unit of work.</summary>
    public async Task< List< OrderPlaced > > PlaceOrdersAsync< TContext >( int count = 1, CancellationToken cancellationToken = default )
        where TContext : DatabaseContext
    {
        await using var scope = Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService< ModuleUnitOfWork< TContext > >();
        var orders = Enumerable.Range( 1, count ).Select( i => Order.Place( $"O-{i}", i * 10m ) ).ToList();
        var events = orders.SelectMany( o => o.DomainEvents ).Cast< OrderPlaced >().ToList();
        orders.ForEach( unitOfWork.RegisterNew );
        await unitOfWork.CommitAsync( cancellationToken );
        return events;
    }

    public async Task< List< OutboxMessage > > MessagesAsync< TContext >()
        where TContext : DbContext
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService< TContext >().Set< OutboxMessage >().AsNoTracking().ToListAsync();
    }

    public async Task< int > OrderCountAsync< TContext >()
        where TContext : ModuleContext
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService< TContext >().Orders.CountAsync();
    }

    public OutboxDispatcher< TContext > Dispatcher< TContext >()
        where TContext : DbContext
        => ActivatorUtilities.CreateInstance< OutboxDispatcher< TContext > >( Services );

    public async Task ExecuteSqlAsync< TContext >( string sql )
        where TContext : DbContext
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService< TContext >().Database.ExecuteSqlRawAsync( sql );
    }

    public ValueTask DisposeAsync() => Services.DisposeAsync();

    #endregion
}
