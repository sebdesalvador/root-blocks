namespace RootBlocks.Tests.Extensions;

public class DateTimeExtensionsTests
{
    private static readonly DateTime Reference = new( 1981, 4, 9, 16, 30, 0, DateTimeKind.Utc );

    [ Fact ]
    public void IsBetween_ValueInsideTheRange_ReturnsTrue()
    {
        Assert.True( Reference.IsBetween( Reference.AddDays( -1 ), Reference.AddDays( 1 ) ) );
    }

    [ Fact ]
    public void IsBetween_BoundsAreInclusive()
    {
        Assert.True( Reference.IsBetween( Reference, Reference ) );
    }

    [ Fact ]
    public void IsBetween_ValueOutsideTheRange_ReturnsFalse()
    {
        Assert.False( Reference.IsBetween( Reference.AddDays( 1 ), Reference.AddDays( 2 ) ) );
    }

    [ Fact ]
    public void StartOfDay_ReturnsMidnightAndPreservesTheKind()
    {
        var startOfDay = Reference.StartOfDay();

        Assert.Equal( new DateTime( 1981, 4, 9, 0, 0, 0, DateTimeKind.Utc ), startOfDay );
        Assert.Equal( DateTimeKind.Utc, startOfDay.Kind );
    }

    [ Fact ]
    public void EndOfDay_ReturnsTheLastTickOfTheDay()
    {
        var endOfDay = Reference.EndOfDay();

        Assert.Equal( Reference.StartOfDay().AddDays( 1 ), endOfDay.AddTicks( 1 ) );
        Assert.Equal( DateTimeKind.Utc, endOfDay.Kind );
    }

    [ Fact ]
    public void EndOfDay_KeepsTheSubSecondPrecisionOfTheDay()
    {
        Assert.Equal( 9_999_999, Reference.EndOfDay().Ticks % TimeSpan.TicksPerSecond );
    }

    [ Fact ]
    public void GetAge_BirthdayAlreadyPassed_CountsTheFullYear()
    {
        var birthDate = new DateTime( 2000, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        Assert.Equal( 25, birthDate.GetAge( new DateTime( 2025, 6, 1, 0, 0, 0, DateTimeKind.Utc ) ) );
    }

    [ Fact ]
    public void GetAge_BirthdayNotReachedYet_DoesNotCountTheYear()
    {
        var birthDate = new DateTime( 2000, 12, 31, 0, 0, 0, DateTimeKind.Utc );

        Assert.Equal( 24, birthDate.GetAge( new DateTime( 2025, 6, 1, 0, 0, 0, DateTimeKind.Utc ) ) );
    }

    [ Fact ]
    public void GetAge_OnTheBirthday_CountsTheFullYear()
    {
        var birthDate = new DateTime( 2000, 6, 1, 0, 0, 0, DateTimeKind.Utc );

        Assert.Equal( 25, birthDate.GetAge( new DateTime( 2025, 6, 1, 23, 0, 0, DateTimeKind.Utc ) ) );
    }

    [ Fact ]
    public void GetAge_WithoutReference_MeasuresAgainstTheCurrentUtcDate()
    {
        Assert.Equal( 0, DateTime.UtcNow.GetAge() );
    }

    [ Theory ]
    [ InlineData( 2025, 6, 7, true ) ]
    [ InlineData( 2025, 6, 8, true ) ]
    [ InlineData( 2025, 6, 9, false ) ]
    [ InlineData( 2025, 6, 6, false ) ]
    public void IsWeekend_ReturnsTrueForSaturdayAndSunday( int year, int month, int day, bool expected )
    {
        var dateTime = new DateTime( year, month, day, 0, 0, 0, DateTimeKind.Utc );

        Assert.Equal( expected, dateTime.IsWeekend() );
    }

    [ Fact ]
    public void NextBusinessDay_FromAFriday_SkipsTheWeekend()
    {
        var friday = new DateTime( 2025, 6, 6, 9, 15, 0, DateTimeKind.Utc );

        Assert.Equal( new DateTime( 2025, 6, 9, 9, 15, 0, DateTimeKind.Utc ), friday.NextBusinessDay() );
    }

    [ Fact ]
    public void NextBusinessDay_FromAMonday_ReturnsTheNextDay()
    {
        var monday = new DateTime( 2025, 6, 9, 9, 15, 0, DateTimeKind.Utc );

        Assert.Equal( new DateTime( 2025, 6, 10, 9, 15, 0, DateTimeKind.Utc ), monday.NextBusinessDay() );
    }

    [ Fact ]
    public void NextBusinessDay_PreservesTheTimeOfDay()
    {
        Assert.Equal( Reference.TimeOfDay, Reference.NextBusinessDay().TimeOfDay );
    }
}
