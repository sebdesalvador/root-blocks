namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class TranslatableEnumerationConfiguration< T > : EnumerationConfiguration< T >
    where T : TranslatableEnumeration
{
    public override void Configure( EntityTypeBuilder< T > builder )
    {
        base.Configure( builder );
        builder.Property( e => e.Key )
               .HasMaxLength( 512 )
               .IsRequired();
    }
}
