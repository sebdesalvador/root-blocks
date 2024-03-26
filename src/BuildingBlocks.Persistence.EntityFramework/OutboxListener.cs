namespace BuildingBlocks.Persistence.EntityFramework;

public class OutboxListener( ILogger< OutboxListener > logger, IServiceProvider provider ) : BackgroundService
{
    #region Fields

    private SqlDependencyEx? _listener;
    private bool _isProcessing;

    #endregion

    #region Overrides

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        if ( await IsBrokerEnabled( stoppingToken ) )
            await DoWorkAsync( stoppingToken );
    }

    public override async Task StopAsync( CancellationToken cancellationToken )
    {
        await StopListenerAsync( cancellationToken );
        await base.StopAsync( cancellationToken );
    }

    #endregion

    #region Private Methods

    private async Task< bool > IsBrokerEnabled( CancellationToken cancellationToken )
    {
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider
                                       .GetRequiredService< IDatabaseContextWithOutbox >() as DatabaseContextWithOutbox;

        if ( context == null )
            throw new NoCompatibleDatabaseContextFoundException();

        var connectionStringBuilder = new SqlConnectionStringBuilder( context.Database.GetConnectionString() );
        var enableBrokerScript = $@"
            DECLARE @bitBrokerEnabled BIT;

            SELECT @bitBrokerEnabled = is_broker_enabled
              FROM sys.databases
             WHERE name = '{connectionStringBuilder.InitialCatalog}';

            IF ( @bitBrokerEnabled = 0 )
                ALTER DATABASE [{connectionStringBuilder.InitialCatalog}] SET ENABLE_BROKER;

            SELECT is_broker_enabled
              FROM sys.databases
             WHERE name = '{connectionStringBuilder.InitialCatalog}';
            ";
        await using var conn = new SqlConnection( connectionStringBuilder.ToString() );
        await using var command = new SqlCommand( enableBrokerScript, conn );
        await conn.OpenAsync( cancellationToken );
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 60000;
        await using var reader = await command.ExecuteReaderAsync( cancellationToken );
        return await reader.ReadAsync( cancellationToken )
            && !reader.IsDBNull( 0 )
            && reader.GetBoolean( 0 );
    }

    private async Task DoWorkAsync( CancellationToken cancellationToken = default )
    {
        if ( cancellationToken.IsCancellationRequested ) return;

        try
        {
            await using var outerScope = provider.CreateAsyncScope();
            await using var outerDbContext = outerScope.ServiceProvider
                                                       .GetRequiredService< IDatabaseContextWithOutbox >() as DatabaseContextWithOutbox;

            if ( outerDbContext == null ) throw new NoCompatibleDatabaseContextFoundException();

            var connectionStringBuilder = new SqlConnectionStringBuilder( outerDbContext.Database.GetConnectionString() );
            _listener = new SqlDependencyEx( connectionStringBuilder.ToString(),
                                             connectionStringBuilder.InitialCatalog,
                                             nameof( IDatabaseContextWithOutbox.Outbox ),
                                             listenerType: SqlDependencyEx.NotificationTypes.Insert );
            _listener.TableChanged += async ( _, e ) =>
            {
                await using var innerScope = provider.CreateAsyncScope();
                await using var innerDbContext = innerScope.ServiceProvider
                                                           .GetRequiredService< IDatabaseContextWithOutbox >() as DatabaseContextWithOutbox;

                if ( innerDbContext == null )
                    throw new NoCompatibleDatabaseContextFoundException();

                var row = ( e.Data.FirstNode as XElement )?.FirstNode as XElement;

                if ( !Guid.TryParse( ( row?.FirstNode as XElement )?.Value ?? string.Empty, out var id ) )
                    return;

                var item = await innerDbContext.Outbox.SingleAsync( oi => oi.Id == id, cancellationToken: cancellationToken );
                await ProcessEventAsync( innerDbContext, item, cancellationToken );
            };
            var events = await outerDbContext.Outbox.ToListAsync( cancellationToken: cancellationToken );
            _listener.Start();
            await ProcessEventsAsync( outerDbContext, events, cancellationToken );
        }
        catch ( Exception e )
        {
            logger.LogError( "{Message}", e.Message );
            logger.LogDebug( e, "{Message}", e.Message );
        }
        finally
        {
            await Task.Delay( 30000, cancellationToken )
                      .ContinueWith( async _ => await StopListenerAsync( cancellationToken ), cancellationToken )
                      .ContinueWith( async _ => await DoWorkAsync( cancellationToken ), cancellationToken );
        }
    }

    private async Task ProcessEventsAsync( DatabaseContextWithOutbox context, IEnumerable< OutboxItem > items, CancellationToken cancellationToken = default )
    {
        foreach ( var item in items )
            await ProcessEventAsync( context, item, cancellationToken );
    }

    private async Task ProcessEventAsync( DatabaseContextWithOutbox context, OutboxItem item, CancellationToken cancellationToken = default )
    {
        try
        {
            if ( item.Event == null )
                throw new UnableToDeserializeDomainEventException( item.Id );

            _isProcessing = true;
            await using var scope = provider.CreateAsyncScope();
            var eventPublisher = scope.ServiceProvider
                                      .GetRequiredService< IEventPublisher >();
            await eventPublisher.PublishAsync( item.Event, cancellationToken );
            context.Remove( item );
            await context.SaveChangesAsync( cancellationToken );
            logger.LogInformation( "Outbox item removed ({EventId})", item.Id );
        }
        catch ( Exception e )
        {
            logger.LogError( e, "{Message}", e.Message );
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task StopListenerAsync( CancellationToken cancellationToken )
    {
        while ( _isProcessing )
            await Task.Delay( 100, cancellationToken ).ConfigureAwait( true );

        _listener?.Dispose();
    }

    #endregion
}
