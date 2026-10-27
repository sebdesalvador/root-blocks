namespace RootBlocks.Persistence.EntityFramework.Tests.Outbox;

[ Collection( SqlServerCollection.NAME ) ]
public class UnitOfWorkOutboxTests( SqlServerFixture sqlServer )
{
    [ Fact ]
    public async Task CommitAsync_ChangeWithEvent_SavesBothAndMarksTheEventPublished()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );

        var placed = Assert.Single( await harness.PlaceOrdersAsync< SalesContext >() );

        Assert.Equal( 1, await harness.OrderCountAsync< SalesContext >() );
        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Equal( placed.Id, message.Id );
        Assert.Equal( typeof( OrderPlaced ).AssemblyQualifiedName, message.Type );
        Assert.Equal( harness.Time.GetUtcNow().UtcDateTime, message.ProcessedOn );
        Assert.Null( message.LockedUntil );
        Assert.Equal( 0, message.Attempts );
        Assert.Equal( placed.Id, Assert.Single( harness.Publisher.Published ).Id );
    }

    [ Fact ]
    public async Task RollbackTransaction_AfterCommit_LeavesNeitherTheChangeNorItsMessage()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        await using var scope = harness.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService< ModuleUnitOfWork< SalesContext > >();

        await unitOfWork.BeginTransactionAsync();
        unitOfWork.RegisterNew( Order.Place( "O-1", 1m ) );
        await unitOfWork.CommitAsync();
        unitOfWork.RollbackTransaction();

        Assert.Equal( 0, await harness.OrderCountAsync< SalesContext >() );
        Assert.Empty( await harness.MessagesAsync< SalesContext >() );
        Assert.Empty( harness.Publisher.Published );
    }

    [ Fact ]
    public async Task CommitTransactionAsync_AfterCommit_PublishesTheEventsOnlyThen()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        await using var scope = harness.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService< ModuleUnitOfWork< SalesContext > >();

        await unitOfWork.BeginTransactionAsync();
        unitOfWork.RegisterNew( Order.Place( "O-1", 1m ) );
        await unitOfWork.CommitAsync();
        Assert.Empty( harness.Publisher.Published );
        await unitOfWork.CommitTransactionAsync();

        Assert.Single( harness.Publisher.Published );
        Assert.NotNull( Assert.Single( await harness.MessagesAsync< SalesContext >() ).ProcessedOn );
    }

    [ Fact ]
    public async Task CommitAsync_HandlerFails_KeepsTheChangeAndRecordsTheFailureWithoutThrowing()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        harness.Publisher.Failure = () => new InvalidOperationException( "handler is down" );

        await harness.PlaceOrdersAsync< SalesContext >();

        Assert.Equal( 1, await harness.OrderCountAsync< SalesContext >() );
        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Null( message.ProcessedOn );
        Assert.Equal( 1, message.Attempts );
        Assert.Contains( "handler is down", message.LastError );
        Assert.Equal( harness.Time.GetUtcNow().UtcDateTime + harness.Options.FirstRetryDelay, message.LockedUntil );
    }

    [ Fact ]
    public async Task CommitAsync_HandlerTimesOut_RecordsTheFailureWithoutThrowing()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        harness.Publisher.Failure = () => new TaskCanceledException( "The request timed out." );

        await harness.PlaceOrdersAsync< SalesContext >();

        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Null( message.ProcessedOn );
        Assert.Equal( 1, message.Attempts );
    }

    [ Fact ]
    public async Task CommitAsync_CallerCancelsWhilePublishing_StopsButKeepsTheChangeAndItsMessage()
    {
        await using var harness = await OutboxHarness.CreateAsync( sqlServer );
        using var cancellation = new CancellationTokenSource();
        harness.Publisher.BeforePublish = ( _, token ) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        };

        await Assert.ThrowsAnyAsync< OperationCanceledException >( () => harness.PlaceOrdersAsync< SalesContext >( cancellationToken: cancellation.Token ) );

        Assert.Equal( 1, await harness.OrderCountAsync< SalesContext >() );
        var message = Assert.Single( await harness.MessagesAsync< SalesContext >() );
        Assert.Null( message.ProcessedOn );
        Assert.Equal( 0, message.Attempts );
        Assert.Equal( harness.Time.GetUtcNow().UtcDateTime + harness.Options.LockDuration, message.LockedUntil );
    }
}
