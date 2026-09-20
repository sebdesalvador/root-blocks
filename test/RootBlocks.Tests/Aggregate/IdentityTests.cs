namespace RootBlocks.Tests.Aggregate;

public class IdentityTests
{
    [ Fact ]
    public void TryCreate_ValidGuid_ReturnsAnIdentityCarryingIt()
    {
        var guid = Guid.Parse( "1b15c0ad-278a-494b-9b5c-41aa57a66482" );

        Assert.True( Identity.TryCreate< TestIdentity >( guid.ToString(), out var identity ) );
        Assert.Equal( guid, identity.Value );
    }

    [ Fact ]
    public void TryCreate_AcceptsTheFormatsGuidAccepts()
    {
        var guid = Guid.NewGuid();

        Assert.True( Identity.TryCreate< TestIdentity >( guid.ToString( "B" ), out var braces ) );
        Assert.True( Identity.TryCreate< TestIdentity >( guid.ToString( "N" ), out var compact ) );
        Assert.Equal( guid, braces.Value );
        Assert.Equal( guid, compact.Value );
    }

    [ Theory ]
    [ InlineData( null ) ]
    [ InlineData( "" ) ]
    [ InlineData( "   " ) ]
    [ InlineData( "not-a-guid" ) ]
    [ InlineData( "1b15c0ad-278a-494b-9b5c" ) ]
    public void TryCreate_InvalidValue_ReturnsFalseAndNoIdentity( string? value )
    {
        Assert.False( Identity.TryCreate< TestIdentity >( value, out var identity ) );
        Assert.Null( identity );
    }

    [ Fact ]
    public void TryCreate_TwiceFromTheSameValue_ProducesEqualIdentities()
    {
        var guid = Guid.NewGuid().ToString();

        Identity.TryCreate< TestIdentity >( guid, out var first );
        Identity.TryCreate< TestIdentity >( guid, out var second );

        Assert.Equal( first, second );
    }

    private class TestIdentity : Identity;
}
