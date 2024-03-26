namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class SoftDeletableEntityConfiguration< TEntity, TIdentity > : EntityConfiguration< TEntity, TIdentity >
    where TEntity : Entity< TIdentity >
    where TIdentity : Identity, new()
{
    public override void Configure( EntityTypeBuilder< TEntity > builder )
    {
        base.Configure( builder );

        builder.HasQueryFilter( e => !EF.Property< DateTime? >( e, nameof( ISoftDeletable.DeletedOn ) ).HasValue );

        builder.Property< DateTime? >( nameof( ISoftDeletable.DeletedOn ) )
               .IsRequired( false );

        builder.HasIndex( nameof( ISoftDeletable.DeletedOn ) );
    }
}
