namespace RootBlocks.Tests.Extensions;

public class GuidExtensionsTests
{
    [ Fact ]
    public void ToIdentity_ReturnsAnIdentityCarryingTheGuid()
    {
        var guid = Guid.Parse( "1b15c0ad-278a-494b-9b5c-41aa57a66482" );

        var identity = guid.ToIdentity< TestIdentity >();

        Assert.Equal( guid, identity.Value );
    }

    [ Fact ]
    public void ToIdentity_TwoIdentitiesFromTheSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        Assert.Equal( guid.ToIdentity< TestIdentity >(), guid.ToIdentity< TestIdentity >() );
    }

    private class TestIdentity : Identity;
}
