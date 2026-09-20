namespace RootBlocks.Tests.Validation;

public class ValidationResultTests
{
    [ Fact ]
    public void Success_IsValidAndCarriesNoErrorMessage()
    {
        var result = ValidationResult.Success();

        Assert.True( result.IsValid );
        Assert.Null( result.ErrorMessage );
    }

    [ Fact ]
    public void Success_ReturnsTheSameInstanceEveryTime()
    {
        Assert.Same( ValidationResult.Success(), ValidationResult.Success() );
    }

    [ Fact ]
    public void Failure_IsNotValidAndCarriesTheErrorMessage()
    {
        var result = ValidationResult.Failure( "Title is required" );

        Assert.False( result.IsValid );
        Assert.Equal( "Title is required", result.ErrorMessage );
    }

    [ Theory ]
    [ InlineData( null ) ]
    [ InlineData( "" ) ]
    public void Failure_WithoutAnErrorMessage_Throws( string? errorMessage )
    {
        Assert.Throws< ArgumentException >( () => ValidationResult.Failure( errorMessage! ) );
    }

    [ Fact ]
    public void ToString_Success_DescribesTheOutcome()
    {
        Assert.Equal( "Valid", ValidationResult.Success().ToString() );
    }

    [ Fact ]
    public void ToString_Failure_IncludesTheErrorMessage()
    {
        Assert.Equal( "Invalid: Title is required", ValidationResult.Failure( "Title is required" ).ToString() );
    }
}
