namespace Blogs.Infrastructure.Persistence.Configurations.PostAggregate;

[ ExcludeFromCodeCoverage ]
public class CommentConfiguration : EntityConfiguration< Comment, CommentId >
{
    public override void Configure( EntityTypeBuilder< Comment > builder )
    {
        base.Configure( builder );

        builder.Property( c => c.Content )
               .HasMaxLength( 1024 )
               .IsRequired();
        builder.Property( c => c.AuthorId )
               .IsRequired();
        builder.Property( c => c.PostId )
               .IsRequired();

        builder.HasOne< Person >()
               .WithMany()
               .HasForeignKey( c => c.AuthorId )
               .OnDelete( DeleteBehavior.Restrict )
               .IsRequired();
        builder.HasOne( c => c.Post )
               .WithMany( p => p.Comments)
               .HasForeignKey( c => c.PostId )
               .OnDelete( DeleteBehavior.Cascade )
               .IsRequired();
    }
}
