namespace RootBlocks.Extensions;

/// <summary>
/// Extension methods for <see cref="DateTime"/>. All methods preserve the <see cref="DateTime.Kind"/>
/// of their input; the library convention is that domain datetimes are UTC.
/// </summary>
public static class DateTimeExtensions
{
    #region Public Methods

    /// <summary>
    /// Determines whether the datetime falls within an inclusive range.
    /// </summary>
    /// <param name="dateTime">The datetime to test.</param>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns><c>true</c> when the datetime is within the range; otherwise <c>false</c>.</returns>
    public static bool IsBetween( this DateTime dateTime, DateTime start, DateTime end )
    {
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Returns the first instant of the day the datetime belongs to.
    /// </summary>
    /// <param name="dateTime">The datetime to truncate.</param>
    /// <returns>The same date at 00:00:00.0000000, with the original <see cref="DateTime.Kind"/>.</returns>
    public static DateTime StartOfDay( this DateTime dateTime )
    {
        return new DateTime( dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind );
    }

    /// <summary>
    /// Returns the last representable instant of the day the datetime belongs to.
    /// </summary>
    /// <param name="dateTime">The datetime to extend.</param>
    /// <returns>The same date at 23:59:59.9999999, with the original <see cref="DateTime.Kind"/>.</returns>
    public static DateTime EndOfDay( this DateTime dateTime )
    {
        return dateTime.StartOfDay().AddDays( 1 ).AddTicks( -1 );
    }

    /// <summary>
    /// Computes the number of whole years elapsed between a birth date and a reference date.
    /// </summary>
    /// <param name="birthDate">The birth date.</param>
    /// <param name="asOf">The date to measure against. Defaults to the current UTC date.</param>
    /// <returns>The age in whole years. Negative when the birth date is in the future.</returns>
    public static int GetAge( this DateTime birthDate, DateTime? asOf = null )
    {
        var reference = ( asOf ?? DateTime.UtcNow ).Date;
        var age = reference.Year - birthDate.Year;

        if ( birthDate.Date > reference.AddYears( -age ) ) age--;

        return age;
    }

    /// <summary>
    /// Determines whether the datetime falls on a Saturday or a Sunday.
    /// </summary>
    /// <param name="dateTime">The datetime to test.</param>
    /// <returns><c>true</c> when the datetime falls on a weekend; otherwise <c>false</c>.</returns>
    public static bool IsWeekend( this DateTime dateTime )
    {
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>
    /// Returns the next day that is not a weekend day.
    /// </summary>
    /// <param name="dateTime">The datetime to advance from.</param>
    /// <returns>The next weekday, preserving the time of day and the <see cref="DateTime.Kind"/>.</returns>
    /// <remarks>Public holidays are not taken into account.</remarks>
    public static DateTime NextBusinessDay( this DateTime dateTime )
    {
        var nextDay = dateTime.AddDays( 1 );

        while ( nextDay.IsWeekend() )
        {
            nextDay = nextDay.AddDays( 1 );
        }

        return nextDay;
    }

    #endregion
}
