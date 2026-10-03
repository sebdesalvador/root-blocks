namespace RootBlocks.Persistence.EntityFramework.Configuration;

public abstract class SoftDeletableEntityConfiguration< TEntity, TId > : EntityConfiguration< TEntity, TId >
    where TEntity : Entity< TId >
    where TId : IEquatable< TId >
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
