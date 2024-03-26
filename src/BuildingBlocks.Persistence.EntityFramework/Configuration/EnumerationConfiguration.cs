namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class EnumerationConfiguration< T > : IEntityTypeConfiguration< T >
    where T : Enumeration
{
    public virtual void Configure( EntityTypeBuilder< T > builder )
    {
        builder.ToTable( typeof( T ).Name );

        builder.HasKey( e => e.Id );

        builder.Property( e => e.Id )
               .ValueGeneratedNever()
               .IsRequired();
        builder.Property( e => e.Name )
               .HasMaxLength( 256 )
               .IsRequired();

        builder.HasIndex( e => e.Name )
               .IsUnique();

        Seed( builder );
    }

    protected virtual void Seed( EntityTypeBuilder< T > builder )
        => builder.HasData( Enumeration.GetAll< T >() );
}
