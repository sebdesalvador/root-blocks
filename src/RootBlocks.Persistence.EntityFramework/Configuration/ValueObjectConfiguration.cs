namespace RootBlocks.Persistence.EntityFramework.Configuration;

public abstract class ValueObjectConfiguration< T > : IEntityTypeConfiguration< T >
    where T : ValueObject
{
    private const string ID_PROPERTY_NAME = "Id";

    public virtual void Configure( EntityTypeBuilder< T > builder )
    {
        builder.ToTable( typeof( T ).Name );

        builder.Property< Guid >( ID_PROPERTY_NAME )
               .IsRequired();

        builder.HasKey( ID_PROPERTY_NAME );
    }
}
