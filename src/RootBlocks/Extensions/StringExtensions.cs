namespace RootBlocks.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    #region Public Methods

    /// <summary>
    /// Determines whether the string is <c>null</c> or empty.
    /// </summary>
    /// <param name="value">The string to test. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the string is <c>null</c> or empty; otherwise <c>false</c>.</returns>
    public static bool IsNullOrEmpty( [ NotNullWhen( false ) ] this string? value )
    {
        return string.IsNullOrEmpty( value );
    }

    /// <summary>
    /// Determines whether the string holds at least one character.
    /// </summary>
    /// <param name="value">The string to test. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the string is neither <c>null</c> nor empty; otherwise <c>false</c>.</returns>
    public static bool IsNotNullOrEmpty( [ NotNullWhen( true ) ] this string? value )
    {
        return !string.IsNullOrEmpty( value );
    }

    /// <summary>
    /// Shortens the string to a maximum length, leaving shorter strings untouched.
    /// </summary>
    /// <param name="value">The string to truncate. May be <c>null</c>.</param>
    /// <param name="maxLength">The maximum number of characters to keep.</param>
    /// <returns>The truncated string, or <see cref="string.Empty"/> when the string is <c>null</c> or empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is negative.</exception>
    public static string Truncate( this string? value, int maxLength )
    {
        if ( maxLength < 0 ) throw new ArgumentOutOfRangeException( nameof( maxLength ) );
        if ( value.IsNullOrEmpty() ) return string.Empty;

        return value.Length <= maxLength ? value : value.Substring( 0, maxLength );
    }

    #endregion
}
