namespace RootBlocks.Serialization.Converters;

public class IdentityTypeConverter< T > : TypeConverter
    where T : Identity
{
    public override bool CanConvertFrom( ITypeDescriptorContext? context, Type sourceType )
        => sourceType == typeof( string ) || base.CanConvertFrom( context, sourceType );

    public override object? ConvertFrom( ITypeDescriptorContext? context, CultureInfo? culture, object value )
    {
        var stringValue = value as string;

        if ( string.IsNullOrWhiteSpace( stringValue ) || !Guid.TryParse( stringValue, out var guid ) )
            return base.ConvertFrom( context, culture, value );

        return guid.ToIdentity< T >();
    }
}
