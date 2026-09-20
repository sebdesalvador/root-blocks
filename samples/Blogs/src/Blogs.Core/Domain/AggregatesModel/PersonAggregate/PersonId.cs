namespace Blogs.Core.Domain.AggregatesModel.PersonAggregate;

[ JsonConverter( typeof( IdentityJsonConverter< PersonId > ) ) ]
[ TypeConverter( typeof( IdentityTypeConverter< PersonId > ) ) ]
public class PersonId : Identity
{
    /// <summary>
    /// Binds a <see cref="PersonId"/> from its string form, so it can be used directly as a route or
    /// query parameter.
    /// </summary>
    public static bool TryParse( string? value, IFormatProvider? provider, out PersonId result )
        => TryCreate( value, out result );
}
