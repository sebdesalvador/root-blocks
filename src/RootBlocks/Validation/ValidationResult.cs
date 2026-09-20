namespace RootBlocks.Validation;

/// <summary>
/// The outcome of a validation: either a success, or a failure carrying an error message.
/// </summary>
public sealed class ValidationResult
{
    #region Fields

    private static readonly ValidationResult Valid = new( true, null );

    #endregion

    #region Constructors

    private ValidationResult( bool isValid, string? errorMessage )
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether the validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message describing the failure, or <c>null</c> when the validation succeeded.
    /// </summary>
    public string? ErrorMessage { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the successful validation result.
    /// </summary>
    /// <returns>A result whose <see cref="IsValid"/> is <c>true</c>.</returns>
    public static ValidationResult Success() => Valid;

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errorMessage">The message describing why the validation failed.</param>
    /// <returns>A result whose <see cref="IsValid"/> is <c>false</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="errorMessage"/> is <c>null</c> or empty.</exception>
    public static ValidationResult Failure( string errorMessage )
    {
        if ( errorMessage.IsNullOrEmpty() )
            throw new ArgumentException( "An error message is required.", nameof( errorMessage ) );

        return new ValidationResult( false, errorMessage );
    }

    #endregion

    #region Overrides

    /// <inheritdoc />
    public override string ToString() => IsValid ? "Valid" : $"Invalid: {ErrorMessage}";

    #endregion
}
