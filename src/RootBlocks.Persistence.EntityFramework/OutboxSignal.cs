namespace RootBlocks.Persistence.EntityFramework;

/// <summary>
/// Wakes the <see cref="OutboxDispatcher{TContext}" /> of a context in this process when one of its messages
/// falls due, so that a retry runs when its backoff runs out instead of at the next poll. Registered as a
/// singleton by <see cref="Extensions.ServiceCollectionExtensions.AddOutbox{TContext}" />;
/// <see cref="OutboxPublisher" /> notifies it after every publish that leaves a message behind.
/// </summary>
/// <remarks>
/// It only reaches dispatchers of the same process. A message left behind by another host is found by the
/// dispatcher's next look at the table, at the latest after <see cref="OutboxOptions.PollInterval" />.
/// </remarks>
public sealed class OutboxSignal
{
    #region Fields

    private readonly object _gate = new();
    private readonly List< Listener > _listeners = [ ];

    #endregion

    #region Public Methods

    /// <summary>
    /// Tells the dispatchers of <paramref name="context" />'s type that a message falls due at
    /// <paramref name="dueAt" /> (UTC). Call it after writing outbox messages the unit of work does not publish,
    /// for instance in a transaction opened on the context directly.
    /// </summary>
    public void Notify( DbContext context, DateTime dueAt )
    {
        ArgumentNullException.ThrowIfNull( context );

        Listener[] listeners;

        lock ( _gate )
            listeners = _listeners.Where( l => l.ContextType.IsInstanceOfType( context ) ).ToArray();

        foreach ( var listener in listeners )
            listener.Wake( dueAt );
    }

    #endregion

    #region Internal Methods

    internal IDisposable Subscribe( Type contextType, Action< DateTime > wake )
    {
        var listener = new Listener( this, contextType, wake );

        lock ( _gate )
            _listeners.Add( listener );

        return listener;
    }

    #endregion

    #region Nested Types

    private sealed class Listener( OutboxSignal signal, Type contextType, Action< DateTime > wake ) : IDisposable
    {
        public Type ContextType { get; } = contextType;

        public void Wake( DateTime dueAt ) => wake( dueAt );

        public void Dispose()
        {
            lock ( signal._gate )
                signal._listeners.Remove( this );
        }
    }

    #endregion
}
