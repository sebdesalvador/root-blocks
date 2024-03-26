namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public class OutboxItemConfiguration : IEntityTypeConfiguration< OutboxItem >
{
    public void Configure( EntityTypeBuilder< OutboxItem > builder )
    {
        builder.ToTable( "Outbox" );

        builder.HasKey( oi => oi.Id );

        builder.Property( oi => oi.Id )
               .ValueGeneratedNever();
        builder.Property( oi => oi.Event )
               .HasConversion( new DomainEventToJsonConverter() )
               .IsRequired();
    }
}
