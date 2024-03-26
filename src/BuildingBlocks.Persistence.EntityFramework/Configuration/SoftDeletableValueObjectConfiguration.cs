namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class SoftDeletableValueObjectConfiguration< T > : ValueObjectConfiguration< T >
    where T : ValueObject
{
    public override void Configure( EntityTypeBuilder< T > builder )
    {
        base.Configure( builder );

        builder.HasQueryFilter( vo => !EF.Property< DateTime? >( vo, nameof( ISoftDeletable.DeletedOn ) ).HasValue );

        builder.Property< DateTime? >( nameof( ISoftDeletable.DeletedOn ) )
               .IsRequired( false );

        builder.HasIndex( nameof( ISoftDeletable.DeletedOn ) );
    }
}
