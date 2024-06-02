namespace RootBlocks.Persistence.EntityFramework.Converters;

public class DomainEventToJsonConverter : ValueConverter< DomainEvent, string >
{
    public DomainEventToJsonConverter( ConverterMappingHints mappingHints = null )
        : base(
            v => Serialize( v ),
            v => Deserialize( v ),
            mappingHints ) { }

    public static ValueConverterInfo DefaultInfo { get; }
        = new( typeof( DomainEvent ),
               typeof( string ),
               i => new DomainEventToJsonConverter( i.MappingHints ) );

    private static string Serialize( DomainEvent @event ) => JsonSerializer.Serialize( @event );

    private static DomainEvent Deserialize( string json )
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse( json );
            var typeName = jsonDoc.RootElement.GetProperty( "typeName" );
            var type = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .SelectMany( a => a.GetTypes() )
                                .First( t => t.Name == typeName.GetString() );
            var domainEvent = ( DomainEvent )JsonSerializer.Deserialize( json, type );

            if ( domainEvent is null )
                throw new SerializationException( $"Unable to deserialize to {typeName}." );

            return domainEvent;
        }
        catch ( Exception )
        {
            return null;
        }
    }
}
