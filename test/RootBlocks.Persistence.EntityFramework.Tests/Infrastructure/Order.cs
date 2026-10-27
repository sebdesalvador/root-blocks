namespace RootBlocks.Persistence.EntityFramework.Tests.Infrastructure;

public class Order : Entity< Guid >, IAggregateRoot
{
    #region Properties

    public string Reference { get; private set; } = null!;

    #endregion

    #region Constructors

    protected Order() { }

    private Order( Guid id, string reference ) : base( id ) => Reference = reference;

    #endregion

    #region Public Methods

    public static Order Place( string reference, decimal total )
    {
        var order = new Order( Guid.NewGuid(), reference );
        order.AddDomainEvent( new OrderPlaced( order.Id, reference, total ) );
        return order;
    }

    #endregion
}

/// <summary>
/// Keeps its state behind private setters and a private constructor, as domain events usually do.
/// </summary>
public class OrderPlaced : DomainEvent
{
    #region Properties

    public Guid OrderId { get; private set; }
    public string Reference { get; private set; } = null!;
    public decimal Total { get; private set; }

    #endregion

    #region Constructors

    private OrderPlaced() { }

    public OrderPlaced( Guid orderId, string reference, decimal total )
    {
        OrderId = orderId;
        Reference = reference;
        Total = total;
    }

    #endregion
}

/// <summary>
/// Has no parameterless constructor: System.Text.Json goes through the parameterized one.
/// </summary>
public class InvoiceIssued( Guid invoiceId, decimal amount ) : DomainEvent
{
    public Guid InvoiceId { get; } = invoiceId;
    public decimal Amount { get; } = amount;
}

public class OrderConfiguration : EntityConfiguration< Order, Guid >
{
    public override void Configure( Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder< Order > builder )
    {
        base.Configure( builder );
        builder.Property( o => o.Reference ).HasMaxLength( 64 ).IsRequired();
    }
}
