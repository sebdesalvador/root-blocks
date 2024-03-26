namespace BuildingBlocks.Serialization.Tests.Converters;

public class IdentityJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new IdentityJsonConverter< TestIdentityWithoutAttribute >() }
    };

    [ Fact ]
    public void ReadJson_ValidGuid_ReturnsIdentity()
    {
        var guidValue = Guid.Parse( "36500652-2DD1-456C-8E9A-26C4B49C8903" );

        var serializedIdentity = $"\"{guidValue}\"";
        var identityWithAttribute = JsonSerializer.Deserialize< TestIdentityWithAttribute >( serializedIdentity );
        Assert.NotNull( identityWithAttribute );
        Assert.Equal( guidValue, identityWithAttribute.Value );

        var identityWithoutAttribute = JsonSerializer.Deserialize< TestIdentityWithoutAttribute >( serializedIdentity, _options );
        Assert.NotNull( identityWithoutAttribute );
        Assert.Equal( guidValue, identityWithoutAttribute.Value );
    }

    [ Fact ]
    public void WriteJson_ValidIdentity_WritesGuid()
    {
        var identityWithAttribute = new TestIdentityWithAttribute();
        var serializedIdentityWithAttribute = JsonSerializer.Serialize( identityWithAttribute );
        Assert.Equal( $"\"{identityWithAttribute.Value}\"", serializedIdentityWithAttribute );

        var identityWithoutAttribute = new TestIdentityWithoutAttribute();
        var serializedIdentityWithoutAttribute = JsonSerializer.Serialize( identityWithoutAttribute, _options );
        Assert.Equal( $"\"{identityWithoutAttribute.Value}\"", serializedIdentityWithoutAttribute );
    }
}
