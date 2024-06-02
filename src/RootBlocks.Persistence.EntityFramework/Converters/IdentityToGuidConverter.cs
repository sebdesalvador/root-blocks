namespace RootBlocks.Persistence.EntityFramework.Converters;

public class IdentityToGuidConverter< T > : ValueConverter< T, Guid >
    where T : Identity, new()
{
    public IdentityToGuidConverter( ConverterMappingHints mappingHints = null )
        : base(
            v => v.Value,
            v => ConvertToIdentity< T >( v ),
            mappingHints ) { }

    public static ValueConverterInfo DefaultInfo { get; }
        = new( typeof( T ),
               typeof( Guid ),
               i => new IdentityToGuidConverter< T >( i.MappingHints ) );

    private static TId ConvertToIdentity< TId >( Guid initialValue )
        where TId : Identity, new()
        => initialValue.ToIdentity< TId >();
}
