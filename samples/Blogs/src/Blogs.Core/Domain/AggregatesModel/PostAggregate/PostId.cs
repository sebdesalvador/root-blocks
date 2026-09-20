namespace Blogs.Core.Domain.AggregatesModel.PostAggregate;

[ JsonConverter( typeof( IdentityJsonConverter< PostId > ) ) ]
[ TypeConverter( typeof( IdentityTypeConverter< PostId > ) ) ]
public class PostId : Identity
{
    /// <summary>
    /// Binds a <see cref="PostId"/> from its string form, so it can be used directly as a route or
    /// query parameter.
    /// </summary>
    public static bool TryParse( string? value, IFormatProvider? provider, out PostId result )
        => TryCreate( value, out result );
}
