namespace RootBlocks.Tests.Extensions;

public class CollectionExtensionsTests
{
    [ Fact ]
    public void IsNullOrEmpty_NullCollection_ReturnsTrue()
    {
        IEnumerable< string >? collection = null;

        Assert.True( collection.IsNullOrEmpty() );
    }

    [ Fact ]
    public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
    {
        Assert.True( Array.Empty< string >().IsNullOrEmpty() );
    }

    [ Fact ]
    public void IsNullOrEmpty_PopulatedCollection_ReturnsFalse()
    {
        Assert.False( new[ ] { "value" }.IsNullOrEmpty() );
    }

    [ Fact ]
    public void IsNullOrEmpty_LazySequence_DoesNotEnumerateBeyondTheFirstElement()
    {
        var enumerated = 0;

        Assert.False( Counting( 3, () => enumerated++ ).IsNullOrEmpty() );
        Assert.Equal( 1, enumerated );
    }

    [ Fact ]
    public void HasItems_PopulatedCollection_ReturnsTrue()
    {
        Assert.True( new[ ] { "value" }.HasItems() );
    }

    [ Theory ]
    [ InlineData( true ) ]
    [ InlineData( false ) ]
    public void HasItems_IsTheOppositeOfIsNullOrEmpty( bool empty )
    {
        string[ ] collection = empty ? [ ] : [ "value" ];

        Assert.NotEqual( collection.IsNullOrEmpty(), collection.HasItems() );
    }

    [ Fact ]
    public void ForEach_PopulatedCollection_AppliesTheActionToEveryElement()
    {
        var visited = new List< int >();

        new[ ] { 1, 2, 3 }.ForEach( visited.Add );

        Assert.Equal( [ 1, 2, 3 ], visited );
    }

    [ Fact ]
    public void ForEach_NullCollection_DoesNothing()
    {
        IEnumerable< int >? collection = null;

        collection.ForEach( _ => throw new InvalidOperationException( "Should not be called." ) );
    }

    [ Fact ]
    public void ForEach_NullAction_Throws()
    {
        Assert.Throws< ArgumentNullException >( () => new[ ] { 1 }.ForEach( null! ) );
    }

    [ Fact ]
    public void WhereNotNull_MixedCollection_KeepsOnlyTheNonNullElements()
    {
        var result = new string?[ ] { "a", null, "b" }.WhereNotNull();

        Assert.Equal( [ "a", "b" ], result );
    }

    [ Fact ]
    public void WhereNotNull_NullCollection_ReturnsAnEmptySequence()
    {
        IEnumerable< string? >? collection = null;

        Assert.Empty( collection.WhereNotNull() );
    }

    [ Fact ]
    public void ToSafeList_NullCollection_ReturnsAnEmptyList()
    {
        IEnumerable< int >? collection = null;

        Assert.Empty( collection.ToSafeList() );
    }

    [ Fact ]
    public void ToSafeList_PopulatedCollection_ReturnsTheElements()
    {
        Assert.Equal( [ 1, 2 ], new[ ] { 1, 2 }.ToSafeList() );
    }

    private static IEnumerable< int > Counting( int count, Action onElement )
    {
        for ( var i = 0; i < count; i++ )
        {
            onElement();
            yield return i;
        }
    }
}
