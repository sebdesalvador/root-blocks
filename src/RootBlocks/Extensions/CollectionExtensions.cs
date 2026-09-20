namespace RootBlocks.Extensions;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class CollectionExtensions
{
    #region Public Methods

    /// <summary>
    /// Determines whether the collection is <c>null</c> or contains no element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="collection">The collection to test. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the collection is <c>null</c> or empty; otherwise <c>false</c>.</returns>
    public static bool IsNullOrEmpty< T >( this IEnumerable< T >? collection )
    {
        return collection switch
        {
            null => true,
            ICollection< T > materialized => materialized.Count == 0,
            _ => !collection.Any()
        };
    }

    /// <summary>
    /// Determines whether the collection is not <c>null</c> and contains at least one element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="collection">The collection to test. May be <c>null</c>.</param>
    /// <returns><c>true</c> when the collection holds at least one element; otherwise <c>false</c>.</returns>
    public static bool HasItems< T >( this IEnumerable< T >? collection )
    {
        return !collection.IsNullOrEmpty();
    }

    /// <summary>
    /// Applies an action to every element of the collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="collection">The collection to enumerate. May be <c>null</c>.</param>
    /// <param name="action">The action to apply to each element.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static void ForEach< T >( this IEnumerable< T >? collection, Action< T > action )
    {
        if ( action is null ) throw new ArgumentNullException( nameof( action ) );
        if ( collection is null ) return;

        foreach ( var item in collection )
        {
            action( item );
        }
    }

    /// <summary>
    /// Filters out the <c>null</c> elements of the collection.
    /// </summary>
    /// <typeparam name="T">The reference type of the elements.</typeparam>
    /// <param name="collection">The collection to filter. May be <c>null</c>.</param>
    /// <returns>The non-null elements, or an empty sequence when the collection is <c>null</c>.</returns>
    public static IEnumerable< T > WhereNotNull< T >( this IEnumerable< T? >? collection )
        where T : class
    {
        return collection is null ? Enumerable.Empty< T >() : collection.OfType< T >();
    }

    /// <summary>
    /// Materializes the collection into a list, substituting an empty list for a <c>null</c> collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="collection">The collection to materialize. May be <c>null</c>.</param>
    /// <returns>A list holding the elements of the collection; never <c>null</c>.</returns>
    public static List< T > ToSafeList< T >( this IEnumerable< T >? collection )
    {
        return collection?.ToList() ?? [ ];
    }

    #endregion
}
