namespace BuildingBlocks.Pagination;

/// <summary>
/// Contains information necessary for pagination and sorting.
/// </summary>
public class Pagination
{
    #region Fields

    private uint _pageIndex;
    private uint _pageSize;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="SortDirection" />.
    /// </summary>
    public SortDirection SortDirection { get; set; }

    /// <summary>
    /// Gets or sets the name of the property sorting should be applied to.
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>
    /// Gets or sets the current page index.
    /// </summary>
    public uint PageIndex
    {
        get => _pageIndex;
        set
        {
            if ( value < 1 )
                throw new ArgumentOutOfRangeException( nameof( PageIndex ), "Page index must be greater than 0." );

            _pageIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of elements per page.
    /// </summary>
    public uint PageSize
    {
        get => _pageSize;
        set
        {
            if ( value < 1 )
                throw new ArgumentOutOfRangeException( nameof( PageSize ), "Page size must be greater than 0." );

            _pageSize = value;
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Pagination" /> class.
    /// </summary>
    public Pagination()
        : this( 1, 5 ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pagination" /> class.
    /// </summary>
    /// <param name="pageIndex">The current page index.</param>
    /// <param name="pageSize">Number of elements per page.</param>
    /// <param name="sortColumn">The name of the property sorting should be applied to.</param>
    /// <param name="sortDirection">The <see cref="SortDirection" />.</param>
    public Pagination(
        uint pageIndex,
        uint pageSize,
        string? sortColumn = null,
        SortDirection sortDirection = SortDirection.Ascending
    )
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SortColumn = sortColumn;
        SortDirection = sortDirection;
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString() => $"SortColumn: {SortColumn}, PageIndex: {PageIndex}, PageSize: {PageSize}";

    #endregion
}
