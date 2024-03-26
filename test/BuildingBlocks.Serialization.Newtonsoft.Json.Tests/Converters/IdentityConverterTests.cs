namespace BuildingBlocks.Serialization.Newtonsoft.Json.Tests.Converters;

public class IdentityConverterTests
{
    private readonly JsonSerializerSettings _settings = new()
    {
        Converters = { new IdentityConverter< TestIdentity >() }
    };

    private readonly JsonSerializerSettings _otherSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    [ Fact ]
    public void ReadJson_ValidGuid_ReturnsIdentity()
    {
        var guidValue = Guid.Parse( "36500652-2DD1-456C-8E9A-26C4B49C8903" );
        var serializedIdentity = $"\"{guidValue}\"";
        var identity = JsonConvert.DeserializeObject< TestIdentity >( serializedIdentity, _settings );

        Assert.NotNull( identity );
        Assert.Equal( guidValue, identity.Value );
    }

    [ Fact ]
    public void WriteJson_ValidIdentity_WritesGuid()
    {
        var identity = new TestIdentity();
        var serializedIdentity = JsonConvert.SerializeObject( identity, _settings );
        Assert.Equal( $"\"{identity.Value}\"", serializedIdentity );
    }
}
