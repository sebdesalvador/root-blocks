namespace RootBlocks.Tests.Extensions;

public class ValidationExtensionsTests
{
    [ Theory ]
    [ InlineData( "seb@example.com", true ) ]
    [ InlineData( "seb.de-salvador+tag@sub.example.co.uk", true ) ]
    [ InlineData( "Seb <seb@example.com>", false ) ]
    [ InlineData( "seb@", false ) ]
    [ InlineData( "@example.com", false ) ]
    [ InlineData( "not an email", false ) ]
    [ InlineData( "", false ) ]
    [ InlineData( "   ", false ) ]
    [ InlineData( null, false ) ]
    public void IsValidEmail_AcceptsOnlyBareAddresses( string? email, bool expected )
    {
        Assert.Equal( expected, email.IsValidEmail() );
    }

    [ Theory ]
    [ InlineData( "0123456789", true ) ]
    [ InlineData( "+32 475 12 34 56", true ) ]
    [ InlineData( "123456789012345", true ) ]
    [ InlineData( "1234567890123456", false ) ]
    [ InlineData( "123456789", false ) ]
    [ InlineData( "", false ) ]
    [ InlineData( "   ", false ) ]
    [ InlineData( null, false ) ]
    public void IsValidPhoneNumber_CountsDigitsOnly( string? phoneNumber, bool expected )
    {
        Assert.Equal( expected, phoneNumber.IsValidPhoneNumber() );
    }

    [ Theory ]
    [ InlineData( 5, 1, 10, true ) ]
    [ InlineData( 1, 1, 10, true ) ]
    [ InlineData( 10, 1, 10, true ) ]
    [ InlineData( 0, 1, 10, false ) ]
    [ InlineData( 11, 1, 10, false ) ]
    public void IsInRange_Int_BoundsAreInclusive( int value, int min, int max, bool expected )
    {
        Assert.Equal( expected, value.IsInRange( min, max ) );
    }

    [ Fact ]
    public void IsInRange_Decimal_BoundsAreInclusive()
    {
        Assert.True( 1.5m.IsInRange( 1.5m, 2.5m ) );
        Assert.True( 2.5m.IsInRange( 1.5m, 2.5m ) );
        Assert.False( 2.51m.IsInRange( 1.5m, 2.5m ) );
    }

    [ Fact ]
    public void ValidateRequired_ValueSupplied_ReturnsSuccess()
    {
        Assert.True( "value".ValidateRequired( "Title" ).IsValid );
    }

    [ Fact ]
    public void ValidateRequired_NullValue_ReturnsFailureNamingTheField()
    {
        var result = ( ( string? )null ).ValidateRequired( "Title" );

        Assert.False( result.IsValid );
        Assert.Equal( "Title is required", result.ErrorMessage );
    }
}
