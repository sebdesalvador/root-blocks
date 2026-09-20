namespace Blogs.Core.Domain.AggregatesModel.PostAggregate;

[ JsonConverter( typeof( IdentityJsonConverter< CommentId > ) ) ]
[ TypeConverter( typeof( IdentityTypeConverter< CommentId > ) ) ]
public class CommentId : Identity
{
    /// <summary>
    /// Binds a <see cref="CommentId"/> from its string form, so it can be used directly as a route or
    /// query parameter.
    /// </summary>
    public static bool TryParse( string? value, IFormatProvider? provider, out CommentId result )
        => TryCreate( value, out result );
}
