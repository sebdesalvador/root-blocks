namespace RootBlocks.Tests.Extensions;

public class StringExtensionsTests
{
    [ Theory ]
    [ InlineData( null, true ) ]
    [ InlineData( "", true ) ]
    [ InlineData( " ", false ) ]
    [ InlineData( "value", false ) ]
    public void IsNullOrEmpty_ReturnsTrueForNullAndEmpty( string? value, bool expected )
    {
        Assert.Equal( expected, value.IsNullOrEmpty() );
    }

    [ Theory ]
    [ InlineData( null, false ) ]
    [ InlineData( "", false ) ]
    [ InlineData( "value", true ) ]
    public void IsNotNullOrEmpty_IsTheOppositeOfIsNullOrEmpty( string? value, bool expected )
    {
        Assert.Equal( expected, value.IsNotNullOrEmpty() );
    }

    [ Fact ]
    public void Truncate_LongerThanTheLimit_CutsTheString()
    {
        Assert.Equal( "val", "value".Truncate( 3 ) );
    }

    [ Fact ]
    public void Truncate_ShorterThanTheLimit_ReturnsTheStringUnchanged()
    {
        Assert.Equal( "value", "value".Truncate( 10 ) );
    }

    [ Fact ]
    public void Truncate_ExactlyTheLimit_ReturnsTheStringUnchanged()
    {
        Assert.Equal( "value", "value".Truncate( 5 ) );
    }

    [ Theory ]
    [ InlineData( null ) ]
    [ InlineData( "" ) ]
    public void Truncate_NullOrEmpty_ReturnsAnEmptyString( string? value )
    {
        Assert.Equal( string.Empty, value.Truncate( 3 ) );
    }

    [ Fact ]
    public void Truncate_NegativeLimit_Throws()
    {
        Assert.Throws< ArgumentOutOfRangeException >( () => "value".Truncate( -1 ) );
    }
}
