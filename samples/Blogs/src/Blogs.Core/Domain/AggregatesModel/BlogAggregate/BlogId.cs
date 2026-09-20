namespace Blogs.Core.Domain.AggregatesModel.BlogAggregate;

[ JsonConverter( typeof( IdentityJsonConverter< BlogId > ) ) ]
[ TypeConverter( typeof( IdentityTypeConverter< BlogId > ) ) ]
public class BlogId : Identity
{
    /// <summary>
    /// Binds a <see cref="BlogId"/> from its string form, so it can be used directly as a route or
    /// query parameter.
    /// </summary>
    public static bool TryParse( string? value, IFormatProvider? provider, out BlogId result )
        => TryCreate( value, out result );
}
