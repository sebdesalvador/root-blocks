namespace RootBlocks.Tests.Resilience;

public class RetryTests
{
    private static readonly TimeSpan NoDelay = TimeSpan.Zero;

    #region Execute

    [ Fact ]
    public void Execute_OperationSucceedsImmediately_RunsItOnce()
    {
        var attempts = 0;

        var result = Retry.Execute( () => { attempts++; return "value"; }, delay: NoDelay );

        Assert.Equal( "value", result );
        Assert.Equal( 1, attempts );
    }

    [ Fact ]
    public void Execute_OperationSucceedsOnTheLastAttempt_ReturnsItsValue()
    {
        var attempts = 0;

        var result = Retry.Execute(
            () =>
            {
                attempts++;
                if ( attempts < 3 ) throw new InvalidOperationException();
                return attempts;
            },
            delay: NoDelay
        );

        Assert.Equal( 3, result );
    }

    [ Fact ]
    public void Execute_OperationAlwaysFails_RethrowsTheLastExceptionAfterExhaustingTheAttempts()
    {
        var attempts = 0;

        Assert.Throws< InvalidOperationException >(
            () => Retry.Execute< int >(
                () => { attempts++; throw new InvalidOperationException(); },
                maxAttempts: 4,
                delay: NoDelay
            )
        );

        Assert.Equal( 4, attempts );
    }

    [ Fact ]
    public void Execute_SingleAttempt_DoesNotRetry()
    {
        var attempts = 0;

        Assert.Throws< InvalidOperationException >(
            () => Retry.Execute< int >(
                () => { attempts++; throw new InvalidOperationException(); },
                maxAttempts: 1,
                delay: NoDelay
            )
        );

        Assert.Equal( 1, attempts );
    }

    [ Fact ]
    public void Execute_NullOperation_Throws()
    {
        Assert.Throws< ArgumentNullException >( () => Retry.Execute< int >( null! ) );
    }

    [ Theory ]
    [ InlineData( 0 ) ]
    [ InlineData( -1 ) ]
    public void Execute_FewerThanOneAttempt_Throws( int maxAttempts )
    {
        Assert.Throws< ArgumentOutOfRangeException >( () => Retry.Execute( () => 1, maxAttempts ) );
    }

    #endregion

    #region ExecuteAsync

    [ Fact ]
    public async Task ExecuteAsync_OperationSucceedsImmediately_RunsItOnce()
    {
        var attempts = 0;

        var result = await Retry.ExecuteAsync(
            _ => { attempts++; return Task.FromResult( "value" ); },
            delay: NoDelay
        );

        Assert.Equal( "value", result );
        Assert.Equal( 1, attempts );
    }

    [ Fact ]
    public async Task ExecuteAsync_OperationSucceedsOnTheLastAttempt_ReturnsItsValue()
    {
        var attempts = 0;

        var result = await Retry.ExecuteAsync(
            _ =>
            {
                attempts++;
                if ( attempts < 3 ) throw new InvalidOperationException();
                return Task.FromResult( attempts );
            },
            delay: NoDelay
        );

        Assert.Equal( 3, result );
    }

    [ Fact ]
    public async Task ExecuteAsync_OperationAlwaysFails_RethrowsAfterExhaustingTheAttempts()
    {
        var attempts = 0;

        await Assert.ThrowsAsync< InvalidOperationException >(
            () => Retry.ExecuteAsync< int >(
                _ => { attempts++; throw new InvalidOperationException(); },
                maxAttempts: 4,
                delay: NoDelay
            )
        );

        Assert.Equal( 4, attempts );
    }

    [ Fact ]
    public async Task ExecuteAsync_CancelledBeforeTheFirstAttempt_DoesNotRunTheOperation()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync< OperationCanceledException >(
            () => Retry.ExecuteAsync< int >(
                _ => throw new InvalidOperationException( "Should not be called." ),
                cancellationToken: source.Token
            )
        );
    }

    [ Fact ]
    public async Task ExecuteAsync_CancelledByTheOperation_StopsRetrying()
    {
        using var source = new CancellationTokenSource();
        var attempts = 0;

        await Assert.ThrowsAnyAsync< OperationCanceledException >(
            () => Retry.ExecuteAsync< int >(
                token =>
                {
                    attempts++;
                    source.Cancel();
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult( 0 );
                },
                delay: NoDelay,
                cancellationToken: source.Token
            )
        );

        Assert.Equal( 1, attempts );
    }

    [ Fact ]
    public async Task ExecuteAsync_NullOperation_Throws()
    {
        await Assert.ThrowsAsync< ArgumentNullException >( () => Retry.ExecuteAsync< int >( null! ) );
    }

    [ Fact ]
    public async Task ExecuteAsync_FewerThanOneAttempt_Throws()
    {
        await Assert.ThrowsAsync< ArgumentOutOfRangeException >(
            () => Retry.ExecuteAsync( _ => Task.FromResult( 1 ), maxAttempts: 0 )
        );
    }

    #endregion
}
