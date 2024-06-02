namespace RootBlocks.Serialization.Tests;

[ JsonConverter( typeof( DomainEventConverter< TestDomainEventWithAttribute > ) ) ]
public class TestDomainEventWithAttribute : DomainEvent
{
    [ SuppressMessage( "ReSharper", "UnusedMember.Local" ) ]
    private TestDomainEventWithAttribute() { }

    public TestDomainEventWithAttribute( Complex complex ) => Complex = complex;

    public string Primitive { get; init; } = "Primitive";
    public Complex Complex { get; private set; } = null!;
}
