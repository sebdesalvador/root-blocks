namespace RootBlocks.Persistence.EntityFramework.Tests.Outbox;

[ Collection( SqlServerCollection.NAME ) ]
public class OutboxDispatcherTests( SqlServerFixture sqlServer )
{
    [ Fact ]
    public async Task DispatchPendingAsync_EventPublishedAtCommit_DoesNotPublishItAgain()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        await harness.PlaceOrdersAsync< SalesContext >();

        harness.Time.Advance( harness.Options.LockDuration + TimeSpan.FromHours( 1 ) );
        await harness.Dispatcher< SalesContext >().DispatchPendingAsync();

        Assert.Single( harness.Publisher.Published );
    }

    [ Fact ]
    public async Task DispatchPendingAsync_WhileTheUnitOfWorkPublishes_LeavesItsMessageAlone()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        var dispatcher = harness.Dispatcher< SalesContext >();
        var dispatched = false;
        harness.Publisher.BeforePublish = async ( _, _ ) =>
        {
            if ( dispatched )
                return;

            dispatched = true;
            await dispatcher.DispatchPendingAsync();
        };

        await harness.PlaceOrdersAsync< SalesContext >();

        Assert.Single( harness.Publisher.Published );
    }

    [ Fact ]
    public async Task DispatchPendingAsync_FailedMessage_RetriesItOnlyAfterTheBackoff()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var placed = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        var dispatcher = harness.Dispatcher< SalesContext >();

        // Still failing after the first delay: the next one doubles.
        harness.Time.Advance( harness.Options.FirstRetryDelay + TimeSpan.FromSeconds( 1 ) );
        await dispatcher.DispatchPendingAsync();
        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Equal( 2, message.Attempts );
        Assert.Equal( harness.Time.GetUtcNow().UtcDateTime + 2 * harness.Options.FirstRetryDelay, message.LockedUntil );

        // Back up, but the delay has not run out.
        harness.Publisher.Failure = null;
        harness.Time.Advance( harness.Options.FirstRetryDelay );
        await dispatcher.DispatchPendingAsync();
        Assert.Empty( harness.Publisher.Published );

        harness.Time.Advance( harness.Options.FirstRetryDelay + TimeSpan.FromSeconds( 1 ) );
        await dispatcher.DispatchPendingAsync();

        var published = Assert.IsType< OrderPlaced >( Assert.Single( harness.Publisher.Published ) );
        Assert.Equal( placed.Id, published.Id );
        Assert.Equal( placed.OrderId, published.OrderId );
        Assert.Equal( placed.Total, published.Total );
        message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Equal( harness.Time.GetUtcNow().UtcDateTime, message.ProcessedOn );
        Assert.Null( message.LockedUntil );
        Assert.Equal( 2, message.Attempts );
    }

    [ Fact ]
    public async Task DispatchPendingAsync_TwoDispatchersOnTheSameTable_PublishEachMessageOnce()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer, new() { [ "Outbox:BatchSize" ] = "100" } );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var placed = await harness.PlaceOrdersAsync< SalesContext >( 40 );
        harness.Publisher.Failure = null;
        harness.Publisher.BeforePublish = ( _, _ ) => Task.Delay( 10 );
        harness.Time.Advance( TimeSpan.FromMinutes( 1 ) );

        await Task.WhenAll( Task.Run( () => harness.Dispatcher< SalesContext >().DispatchPendingAsync() ),
                            Task.Run( () => harness.Dispatcher< SalesContext >().DispatchPendingAsync() ) );

        Assert.Equal( placed.Select( e => e.Id ).Order(), harness.Publisher.Published.Select( e => e.Id ).Order() );
        Assert.All( await harness.MessagesAsync< SalesContext >(), m => Assert.NotNull( m.ProcessedOn ) );
    }

    [ Fact ]
    public async Task DispatchPendingAsync_MessageAtMaxAttempts_LeavesItAlone()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer, new() { [ "Outbox:MaxAttempts" ] = "2" } );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        await harness.PlaceOrdersAsync< SalesContext >();
        var dispatcher = harness.Dispatcher< SalesContext >();
        harness.Time.Advance( TimeSpan.FromMinutes( 1 ) );
        await dispatcher.DispatchPendingAsync();

        harness.Publisher.Failure = null;
        harness.Time.Advance( TimeSpan.FromDays( 1 ) );
        await dispatcher.DispatchPendingAsync();

        Assert.Empty( harness.Publisher.Published );
        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Equal( 2, message.Attempts );
        Assert.Null( message.ProcessedOn );
    }

    [ Fact ]
    public async Task PurgeProcessedAsync_ProcessedMessages_DeletesOnlyThoseOlderThanTheRetention()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        var old = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        harness.Time.Advance( harness.Options.Retention - TimeSpan.FromDays( 1 ) );
        var recent = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var failed = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );

        harness.Time.Advance( TimeSpan.FromDays( 1 ) + TimeSpan.FromSeconds( 1 ) );
        await harness.Dispatcher< SalesContext >().PurgeProcessedAsync();

        var remaining = ( await harness.MessagesAsync< SalesContext >() ).Select( m => m.Id ).ToList();
        Assert.DoesNotContain( old.Id, remaining );
        Assert.Contains( recent.Id, remaining );
        Assert.Contains( failed.Id, remaining );
    }

    [ Fact ]
    public async Task DispatchPendingAsync_TwoContextsInTwoSchemas_EachDispatchesItsOwnTable()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var sales = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        var billing = Assert.Single( await harness.PlaceOrdersAsync< BillingContext >() );
        harness.Publisher.Failure = null;
        harness.Time.Advance( TimeSpan.FromMinutes( 1 ) );

        await harness.Dispatcher< SalesContext >().DispatchPendingAsync();

        Assert.Equal( sales.Id, Assert.Single( harness.Publisher.Published ).Id );
        Assert.Null( Assert.Single( await harness.MessagesAsync< BillingContext >() ).ProcessedOn );

        await harness.Dispatcher< BillingContext >().DispatchPendingAsync();

        Assert.Equal( [ sales.Id, billing.Id ], harness.Publisher.Published.Select( e => e.Id ) );
        Assert.Equal( sales.Id, Assert.Single( await harness.MessagesAsync< SalesContext >() ).Id );
        Assert.Equal( billing.Id, Assert.Single( await harness.MessagesAsync< BillingContext >() ).Id );
    }

    [ Fact ]
    public async Task ExecuteAsync_TickFails_KeepsRunningAndRecovers()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        await harness.PlaceOrdersAsync< SalesContext >();
        harness.Publisher.Failure = null;
        harness.Time.Advance( TimeSpan.FromMinutes( 1 ) );
        await harness.ExecuteSqlAsync< SalesContext >( "EXEC sp_rename 'sales.Outbox', 'Outbox_away'" );
        var dispatcher = harness.Dispatcher< SalesContext >();

        await dispatcher.StartAsync( CancellationToken.None );
        await Task.Delay( TimeSpan.FromSeconds( 1 ) );
        Assert.False( dispatcher.ExecuteTask!.IsCompleted );

        await harness.ExecuteSqlAsync< SalesContext >( "EXEC sp_rename 'sales.Outbox_away', 'Outbox'" );

        for ( var i = 0; i < 100 && harness.Publisher.Published.Count == 0; i++ )
        {
            harness.Time.Advance( harness.Options.PollInterval );
            await Task.Delay( 100 );
        }

        await dispatcher.StopAsync( CancellationToken.None );
        Assert.Single( harness.Publisher.Published );
    }

    [ Fact ]
    public async Task ExecuteAsync_PublishFailsAtCommit_RetriesWhenTheBackoffRunsOutNotAtThePoll()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer, new() { [ "Outbox:PollInterval" ] = "01:00:00" } );
        var dispatcher = harness.Dispatcher< SalesContext >();
        await dispatcher.StartAsync( CancellationToken.None );
        // Let it find the table empty and go to sleep for the whole poll interval.
        await Task.Delay( TimeSpan.FromSeconds( 2 ) );

        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var placed = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        harness.Publisher.Failure = null;
        harness.Time.Advance( harness.Options.FirstRetryDelay );

        var published = await AdvanceUntilAsync( harness, () => harness.Publisher.Published.Count == 1 );
        await dispatcher.StopAsync( CancellationToken.None );
        Assert.True( published, "the dispatcher was not woken when the retry fell due" );
        Assert.Equal( placed.Id, Assert.Single( harness.Publisher.Published ).Id );
    }

    [ Fact ]
    public async Task ExecuteAsync_MessageLeftBehindBeforeItStarted_RetriesItWhenItFallsDue()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer, new() { [ "Outbox:PollInterval" ] = "01:00:00" } );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        await harness.PlaceOrdersAsync< SalesContext >();
        harness.Publisher.Failure = null;
        var dispatcher = harness.Dispatcher< SalesContext >();

        await dispatcher.StartAsync( CancellationToken.None );
        await Task.Delay( TimeSpan.FromSeconds( 2 ) );
        Assert.Empty( harness.Publisher.Published );

        harness.Time.Advance( harness.Options.FirstRetryDelay );
        var published = await AdvanceUntilAsync( harness, () => harness.Publisher.Published.Count == 1 );
        await dispatcher.StopAsync( CancellationToken.None );
        Assert.True( published, "the dispatcher did not wake when the message fell due" );
    }

    [ Fact ]
    public async Task ExecuteAsync_MoreMessagesThanABatch_DispatchesThemAllWithoutWaiting()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer,
                                                                   new()
                                                                   {
                                                                       [ "Outbox:PollInterval" ] = "01:00:00",
                                                                       [ "Outbox:BatchSize" ] = "5"
                                                                   } );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        await harness.PlaceOrdersAsync< SalesContext >( 12 );
        harness.Publisher.Failure = null;
        harness.Time.Advance( TimeSpan.FromMinutes( 1 ) );
        var dispatcher = harness.Dispatcher< SalesContext >();

        await dispatcher.StartAsync( CancellationToken.None );
        var published = await WaitForAsync( () => harness.Publisher.Published.Count == 12 );
        await dispatcher.StopAsync( CancellationToken.None );

        Assert.True( published, $"published {harness.Publisher.Published.Count} of 12" );
    }

    [ Fact ]
    public async Task ExecuteAsync_StartupDelay_LeavesTheTableAloneUntilItRunsOut()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer,
                                                                   new()
                                                                   {
                                                                       [ "Outbox:PollInterval" ] = "01:00:00",
                                                                       [ "Outbox:StartupDelay" ] = "00:10:00"
                                                                   } );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        await harness.PlaceOrdersAsync< SalesContext >();
        harness.Publisher.Failure = null;
        harness.Time.Advance( harness.Options.FirstRetryDelay );
        var dispatcher = harness.Dispatcher< SalesContext >();

        await dispatcher.StartAsync( CancellationToken.None );
        await Task.Delay( TimeSpan.FromSeconds( 2 ) );
        Assert.Empty( harness.Publisher.Published );

        harness.Time.Advance( harness.Options.StartupDelay - TimeSpan.FromSeconds( 5 ) );
        var published = await AdvanceUntilAsync( harness, () => harness.Publisher.Published.Count == 1 );
        await dispatcher.StopAsync( CancellationToken.None );
        Assert.True( published, "the dispatcher did not look at the table once the startup delay ran out" );
    }

    [ Fact ]
    public async Task ExecuteAsync_StartupDelay_WakesEarlyWhenThisProcessLeavesAMessageBehind()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer,
                                                                   new()
                                                                   {
                                                                       [ "Outbox:PollInterval" ] = "01:00:00",
                                                                       [ "Outbox:StartupDelay" ] = "01:00:00"
                                                                   } );
        var dispatcher = harness.Dispatcher< SalesContext >();
        await dispatcher.StartAsync( CancellationToken.None );
        await Task.Delay( TimeSpan.FromSeconds( 1 ) );

        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );
        var placed = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );
        harness.Publisher.Failure = null;
        harness.Time.Advance( harness.Options.FirstRetryDelay );

        var published = await AdvanceUntilAsync( harness, () => harness.Publisher.Published.Count == 1 );
        await dispatcher.StopAsync( CancellationToken.None );
        Assert.True( published, "the dispatcher slept through the startup delay instead of being woken" );
        Assert.Equal( placed.Id, Assert.Single( harness.Publisher.Published ).Id );
    }

    /// <summary>
    /// Moves the fake clock on a second at a time until <paramref name="condition" /> holds, for at most 100 s,
    /// far short of the one-hour poll interval. A single jump is racy: the dispatcher reads the clock to work
    /// out its delay, then starts a timer that counts from the clock as it is then, so a jump landing between
    /// the two leaves the timer a full delay in the future.
    /// </summary>
    private static async Task< bool > AdvanceUntilAsync( OutboxHarness harness, Func< bool > condition )
    {
        for ( var i = 0; i < 100; i++ )
        {
            if ( condition() )
                return true;

            harness.Time.Advance( TimeSpan.FromSeconds( 1 ) );
            await Task.Delay( 100 );
        }

        return condition();
    }

    private static async Task< bool > WaitForAsync( Func< bool > condition )
    {
        for ( var i = 0; i < 100; i++ )
        {
            if ( condition() )
                return true;

            await Task.Delay( 100 );
        }

        return condition();
    }
}
