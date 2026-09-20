using System.Net.Mail;

namespace RootBlocks.Extensions;

/// <summary>
/// Extension methods validating common primitive values.
/// </summary>
public static class ValidationExtensions
{
    #region Fields

    private const int MinimumPhoneNumberDigits = 10;
    private const int MaximumPhoneNumberDigits = 15;

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the string is a parseable email address.
    /// </summary>
    /// <param name="email">The candidate email address. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the value parses as a single email address; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This is a shape check, not a deliverability check: only sending a message proves an address exists.
    /// </remarks>
    public static bool IsValidEmail( this string? email )
    {
        if ( string.IsNullOrWhiteSpace( email ) ) return false;

        try
        {
            return new MailAddress( email ).Address == email;
        }
        catch ( FormatException )
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the string holds a plausible phone number.
    /// </summary>
    /// <param name="phoneNumber">The candidate phone number. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the value holds between 10 and 15 digits; otherwise <c>false</c>.</returns>
    /// <remarks>Separators are ignored; only the digit count is checked, per the E.164 bounds.</remarks>
    public static bool IsValidPhoneNumber( this string? phoneNumber )
    {
        if ( string.IsNullOrWhiteSpace( phoneNumber ) ) return false;

        var digits = phoneNumber.Count( char.IsDigit );

        return digits is >= MinimumPhoneNumberDigits and <= MaximumPhoneNumberDigits;
    }

    /// <summary>
    /// Determines whether the value falls within an inclusive range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns><c>true</c> when the value is within the range; otherwise <c>false</c>.</returns>
    public static bool IsInRange( this int value, int min, int max )
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Determines whether the value falls within an inclusive range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns><c>true</c> when the value is within the range; otherwise <c>false</c>.</returns>
    public static bool IsInRange( this decimal value, decimal min, decimal max )
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Validates that a required value was supplied.
    /// </summary>
    /// <typeparam name="T">The reference type of the value.</typeparam>
    /// <param name="value">The value to test. May be <c>null</c>.</param>
    /// <param name="fieldName">The name of the field, used to build the error message.</param>
    /// <returns>A success when the value is not <c>null</c>; otherwise a failure naming the field.</returns>
    public static ValidationResult ValidateRequired< T >( this T? value, string fieldName )
        where T : class
    {
        return value is not null
            ? ValidationResult.Success()
            : ValidationResult.Failure( $"{fieldName} is required" );
    }

    #endregion
}
