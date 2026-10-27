namespace RootBlocks.Persistence.EntityFramework.Tests.Outbox;

public class OutboxMessageTests
{
    private static readonly OutboxOptions OPTIONS = new();

    [ Fact ]
    public void ToEvent_EventWithPrivateSetters_ReadsBackItsIdDatesAndProperties()
    {
        var orderPlaced = new OrderPlaced( Guid.NewGuid(), "O-1", 12.5m );
        orderPlaced.SetCorrelationIds( Guid.NewGuid(), Guid.NewGuid() );

        var read = Assert.IsType< OrderPlaced >( OutboxMessage.For( orderPlaced, DateTime.UtcNow ).ToEvent() );

        Assert.Equal( orderPlaced.Id, read.Id );
        Assert.Equal( orderPlaced.OccurredOn, read.OccurredOn );
        Assert.Equal( orderPlaced.CausationId, read.CausationId );
        Assert.Equal( orderPlaced.CorrelationId, read.CorrelationId );
        Assert.Equal( orderPlaced.OrderId, read.OrderId );
        Assert.Equal( "O-1", read.Reference );
        Assert.Equal( 12.5m, read.Total );
    }

    [ Fact ]
    public void ToEvent_EventWithOnlyAParameterizedConstructor_ReadsBackItsIdAndProperties()
    {
        var invoiceIssued = new InvoiceIssued( Guid.NewGuid(), 99m );

        var read = Assert.IsType< InvoiceIssued >( OutboxMessage.For( invoiceIssued, DateTime.UtcNow ).ToEvent() );

        Assert.Equal( invoiceIssued.Id, read.Id );
        Assert.Equal( invoiceIssued.InvoiceId, read.InvoiceId );
        Assert.Equal( 99m, read.Amount );
    }

    [ Fact ]
    public void For_Event_StoresItsIdAssemblyQualifiedTypeAndOccurrence()
    {
        var orderPlaced = new OrderPlaced( Guid.NewGuid(), "O-1", 1m );
        var lockedUntil = DateTime.UtcNow.AddMinutes( 1 );

        var message = OutboxMessage.For( orderPlaced, lockedUntil );

        Assert.Equal( orderPlaced.Id, message.Id );
        Assert.Equal( typeof( OrderPlaced ).AssemblyQualifiedName, message.Type );
        Assert.Equal( orderPlaced.OccurredOn, message.OccurredOn );
        Assert.Equal( lockedUntil, message.LockedUntil );
        Assert.Null( message.ProcessedOn );
        Assert.Equal( 0, message.Attempts );
    }

    [ Fact ]
    public void ToEvent_TypeWrittenByAnotherAssemblyVersion_StillResolvesIt()
    {
        var orderPlaced = new OrderPlaced( Guid.NewGuid(), "O-1", 1m );
        var message = OutboxMessage.For( orderPlaced, DateTime.UtcNow );
        var olderType = message.Type.Replace( typeof( OrderPlaced ).Assembly.GetName().Version!.ToString(), "0.0.0.1" );
        typeof( OutboxMessage ).GetProperty( nameof( OutboxMessage.Type ) )!.SetValue( message, olderType );

        Assert.IsType< OrderPlaced >( message.ToEvent() );
    }

    [ Fact ]
    public void MarkFailed_RepeatedFailures_DoublesTheDelayUpToTheMaximum()
    {
        var now = DateTime.UtcNow;
        var message = OutboxMessage.For( new OrderPlaced( Guid.NewGuid(), "O-1", 1m ), now );
        var delays = new List< TimeSpan >();

        for ( var i = 0; i < 12; i++ )
        {
            message.MarkFailed( new InvalidOperationException( "boom" ), now, OPTIONS );
            delays.Add( message.LockedUntil!.Value - now );
        }

        Assert.Equal( TimeSpan.FromSeconds( 5 ), delays[ 0 ] );
        Assert.Equal( TimeSpan.FromSeconds( 10 ), delays[ 1 ] );
        Assert.Equal( TimeSpan.FromSeconds( 20 ), delays[ 2 ] );
        Assert.Equal( OPTIONS.MaxRetryDelay, delays[ ^1 ] );
        Assert.Equal( 12, message.Attempts );
        Assert.Contains( "boom", message.LastError );
    }

    [ Fact ]
    public void ApplyOutbox_AnyDbContext_MapsTheTableIntoItsDefaultSchema()
    {
        using var context = new PlainContext();

        var entityType = context.Model.FindEntityType( typeof( OutboxMessage ) )!;

        Assert.Equal( "Outbox", entityType.GetTableName() );
        Assert.Equal( "documents", entityType.GetSchema() );
        Assert.Contains( entityType.GetIndexes(),
                         i => i.Properties.Select( p => p.Name ).SequenceEqual( [ nameof( OutboxMessage.ProcessedOn ), nameof( OutboxMessage.OccurredOn ) ] ) );
    }

    private class PlainContext : DbContext
    {
        protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
            => optionsBuilder.UseSqlServer( "Server=unused" );

        protected override void OnModelCreating( ModelBuilder modelBuilder )
        {
            modelBuilder.HasDefaultSchema( "documents" );
            modelBuilder.ApplyOutbox();
        }
    }
}
