namespace RootBlocks.Resilience;

/// <summary>
/// Runs an operation again after a failure, waiting a little longer before each attempt.
/// </summary>
/// <remarks>
/// A deliberately small helper for the simple case. Anything needing jitter, circuit breaking or
/// per-exception policies is better served by a dedicated resilience library such as Polly.
/// </remarks>
public static class Retry
{
    #region Fields

    /// <summary>
    /// The number of attempts made when no other value is supplied.
    /// </summary>
    public const int DefaultMaxAttempts = 3;

    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds( 1 );

    #endregion

    #region Public Methods

    /// <summary>
    /// Runs an operation, retrying it after a failure until it succeeds or the attempts run out.
    /// </summary>
    /// <typeparam name="T">The type returned by the operation.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="maxAttempts">The total number of attempts, including the first one.</param>
    /// <param name="delay">The base wait between attempts, multiplied by the attempt number. Defaults to one second.</param>
    /// <returns>The value produced by the first successful attempt.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is less than one.</exception>
    /// <remarks>This blocks the calling thread while waiting; prefer <see cref="ExecuteAsync{T}"/>.</remarks>
    public static T Execute< T >( Func< T > operation, int maxAttempts = DefaultMaxAttempts, TimeSpan? delay = null )
    {
        if ( operation is null ) throw new ArgumentNullException( nameof( operation ) );
        GuardAttempts( maxAttempts );

        var baseDelay = delay ?? DefaultDelay;

        for ( var attempt = 1; ; attempt++ )
        {
            try
            {
                return operation();
            }
            catch when ( attempt < maxAttempts )
            {
                Thread.Sleep( Backoff( baseDelay, attempt ) );
            }
        }
    }

    /// <summary>
    /// Runs an asynchronous operation, retrying it after a failure until it succeeds or the attempts run out.
    /// </summary>
    /// <typeparam name="T">The type returned by the operation.</typeparam>
    /// <param name="operation">The operation to run. It receives the cancellation token.</param>
    /// <param name="maxAttempts">The total number of attempts, including the first one.</param>
    /// <param name="delay">The base wait between attempts, multiplied by the attempt number. Defaults to one second.</param>
    /// <param name="cancellationToken">Cancels the waiting between attempts and is passed to the operation.</param>
    /// <returns>The value produced by the first successful attempt.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is less than one.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled.</exception>
    public static async Task< T > ExecuteAsync< T >(
        Func< CancellationToken, Task< T > > operation,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default
    )
    {
        if ( operation is null ) throw new ArgumentNullException( nameof( operation ) );
        GuardAttempts( maxAttempts );

        var baseDelay = delay ?? DefaultDelay;

        for ( var attempt = 1; ; attempt++ )
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation( cancellationToken ).ConfigureAwait( false );
            }
            catch ( Exception exception ) when ( attempt < maxAttempts && !IsCancellation( exception, cancellationToken ) )
            {
                await Task.Delay( Backoff( baseDelay, attempt ), cancellationToken ).ConfigureAwait( false );
            }
        }
    }

    #endregion

    #region Private Methods

    private static TimeSpan Backoff( TimeSpan baseDelay, int attempt )
    {
        return TimeSpan.FromTicks( baseDelay.Ticks * attempt );
    }

    private static bool IsCancellation( Exception exception, CancellationToken cancellationToken )
    {
        return exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
    }

    private static void GuardAttempts( int maxAttempts )
    {
        if ( maxAttempts < 1 )
            throw new ArgumentOutOfRangeException( nameof( maxAttempts ), "At least one attempt is required." );
    }

    #endregion
}
