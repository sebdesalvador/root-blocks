namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class DerivedEntityConfiguration< TEntity, TBase > : IEntityTypeConfiguration< TEntity >
    where TEntity : class, TBase
    where TBase : class
{
    public virtual void Configure( EntityTypeBuilder< TEntity > builder )
    {
        builder.HasBaseType< TBase >();
        builder.ToTable( typeof( TEntity ).Name );
    }
}
