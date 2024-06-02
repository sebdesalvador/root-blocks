namespace RootBlocks.Pagination;

/// <summary>
/// Wraps a list of items together with the total number of available items.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class PagedResult< T >
{
    #region Properties

    /// <summary>
    /// Gets the items.
    /// </summary>
    public IEnumerable< T > Items { get; set; }

    /// <summary>
    /// Gets the pagination.
    /// </summary>
    public Pagination Pagination { get; set; }

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    public uint TotalNumberOfItems { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    /// <value>The total number of pages.</value>
    public int TotalNumberOfPages
    {
        get
        {
            if ( Pagination.PageSize <= 0 )
                return 0;

            return ( int )Math.Ceiling( ( double )TotalNumberOfItems / Pagination.PageSize );
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult&lt;T&gt;"/> class.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="totalNumberOfItems">The total number of items.</param>
    /// <param name="pagination">The pagination.</param>
    public PagedResult( IEnumerable< T > items, uint totalNumberOfItems, Pagination pagination )
    {
        Items = items;
        TotalNumberOfItems = totalNumberOfItems;
        Pagination = pagination;
    }

    #endregion
}
