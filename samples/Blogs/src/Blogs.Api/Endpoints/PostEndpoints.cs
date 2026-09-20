namespace Blogs.Api.Endpoints;

/// <summary>
/// The post endpoints, including its tags and comments.
/// </summary>
public static class PostEndpoints
{
    #region Public Methods

    /// <summary>
    /// Maps every post endpoint under <c>/post</c>.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on.</param>
    /// <returns>The group the endpoints were mapped on, to allow further configuration.</returns>
    public static RouteGroupBuilder MapPostEndpoints( this IEndpointRouteBuilder endpoints )
    {
        var group = endpoints.MapGroup( "/post" ).WithTags( "Post" );

        group.MapGet( "/", FindPosts ).WithName( nameof( FindPosts ) );
        group.MapGet( "/{id}", GetPost ).WithName( nameof( GetPost ) );
        group.MapPost( "/", CreatePost ).WithName( nameof( CreatePost ) );
        group.MapPatch( "/{id}", UpdatePost ).WithName( nameof( UpdatePost ) )
             .Accepts< JsonPatchDocument< Post > >( MediaTypeNames.Application.JsonPatch );
        group.MapDelete( "/{id}", DeletePost ).WithName( nameof( DeletePost ) );

        group.MapPost( "/{id}/tags/{tag}", AddTag ).WithName( nameof( AddTag ) );
        group.MapDelete( "/{id}/tags/{tag}", RemoveTag ).WithName( nameof( RemoveTag ) );

        group.MapPost( "/{id}/comments", AddComment ).WithName( nameof( AddComment ) );
        group.MapDelete( "/{id}/comments/{commentId}", DeleteComment ).WithName( nameof( DeleteComment ) );

        return group;
    }

    #endregion

    #region Handlers

    /// <summary>
    /// Finds posts matching a search term and a set of tags.
    /// </summary>
    /// <param name="postQueries">The post read model.</param>
    /// <param name="searchTerm">The term to search for in the posts' content or title.</param>
    /// <param name="tags">The tags a post must carry to be returned.</param>
    /// <param name="pageIndex">The one-based index of the page to return.</param>
    /// <param name="pageSize">The number of posts per page.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A paged result of posts, with the total count and pagination information.</returns>
    public static async Task< Ok< PagedResult< PostDto > > > FindPosts(
        IPostQueries postQueries,
        [ FromQuery( Name = "search-term" ) ] string? searchTerm = null,
        [ FromQuery( Name = "tags" ) ] string[]? tags = null,
        [ FromQuery( Name = "page-index" ) ] uint pageIndex = 1,
        [ FromQuery( Name = "page-size" ) ] uint pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        var (posts, total) = await postQueries.FindPostsAsync(
            searchTerm,
            tags,
            pageIndex,
            pageSize,
            cancellationToken
        );

        return TypedResults.Ok( new PagedResult< PostDto >( posts, total, new Pagination( pageIndex, pageSize ) ) );
    }

    /// <summary>
    /// Retrieves a post by its ID.
    /// </summary>
    /// <param name="id">The ID of the post to retrieve.</param>
    /// <param name="postQueries">The post read model.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>The post with the specified ID, or a 404 if it could not be found.</returns>
    public static async Task< Results< Ok< PostDto >, NotFound > > GetPost(
        PostId id,
        IPostQueries postQueries,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return TypedResults.Ok( await postQueries.GetPostAsync( id, cancellationToken ) );
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Publishes a new post on a blog.
    /// </summary>
    /// <param name="body">The details of the post to create.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 201 carrying the newly created post.</returns>
    public static async Task< CreatedAtRoute< PostDto > > CreatePost(
        CreatePostRequestBody body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        var newPost = await mediator.Send(
            new CreatePostCommand( body.BlogId, body.Title, body.Content ),
            cancellationToken
        );

        return TypedResults.CreatedAtRoute( newPost, nameof( GetPost ), new { id = newPost.Id } );
    }

    /// <summary>
    /// Updates a post by its ID.
    /// </summary>
    /// <param name="id">The ID of the post to update.</param>
    /// <param name="body">A JSON patch document containing the changes to apply to the post.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the post was updated, or a 404 if it could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > UpdatePost(
        PostId id,
        JsonPatchDocument< Post > body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new UpdatePostCommand( id, body ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Deletes a post by its ID.
    /// </summary>
    /// <param name="id">The ID of the post to delete.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the post was deleted, or a 404 if it could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > DeletePost(
        PostId id,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new DeletePostCommand( id ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Adds a tag to a post.
    /// </summary>
    /// <param name="id">The ID of the post to tag.</param>
    /// <param name="tag">The tag to add.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the tag was added, or a 404 if the post could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > AddTag(
        PostId id,
        string tag,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new AddTagCommand( id, tag ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Removes a tag from a post.
    /// </summary>
    /// <param name="id">The ID of the post to untag.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the tag was removed, or a 404 if the post could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > RemoveTag(
        PostId id,
        string tag,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new DeleteTagCommand( id, tag ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Adds a comment to a post.
    /// </summary>
    /// <param name="id">The ID of the post to add the comment to.</param>
    /// <param name="body">The details of the comment to add.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the comment was added, or a 404 if the post could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > AddComment(
        PostId id,
        AddCommentRequestBody body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new AddCommentCommand( id, body.AuthorId, body.Content ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Deletes a comment from a post.
    /// </summary>
    /// <param name="id">The ID of the post that the comment belongs to.</param>
    /// <param name="commentId">The ID of the comment to delete.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the comment was deleted, or a 404 if the post or the comment could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > DeleteComment(
        PostId id,
        CommentId commentId,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new DeleteCommentCommand( id, commentId ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Post > )
        {
            return TypedResults.NotFound();
        }
        catch ( EntityNotFoundException< Comment > )
        {
            return TypedResults.NotFound();
        }
    }

    #endregion
}
