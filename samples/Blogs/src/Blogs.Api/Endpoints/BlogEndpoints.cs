namespace Blogs.Api.Endpoints;

/// <summary>
/// The blog endpoints.
/// </summary>
public static class BlogEndpoints
{
    #region Public Methods

    /// <summary>
    /// Maps every blog endpoint under <c>/blog</c>.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on.</param>
    /// <returns>The group the endpoints were mapped on, to allow further configuration.</returns>
    public static RouteGroupBuilder MapBlogEndpoints( this IEndpointRouteBuilder endpoints )
    {
        var group = endpoints.MapGroup( "/blog" ).WithTags( "Blog" );

        group.MapGet( "/{id}", GetBlog ).WithName( nameof( GetBlog ) );
        group.MapPost( "/", CreateBlog ).WithName( nameof( CreateBlog ) );
        group.MapPatch( "/{id}", UpdateBlog ).WithName( nameof( UpdateBlog ) )
             .Accepts< JsonPatchDocument< Blog > >( MediaTypeNames.Application.JsonPatch );
        group.MapDelete( "/{id}", DeleteBlog ).WithName( nameof( DeleteBlog ) );

        return group;
    }

    #endregion

    #region Handlers

    /// <summary>
    /// Retrieves a blog by its ID.
    /// </summary>
    /// <param name="id">The ID of the blog to retrieve.</param>
    /// <param name="blogQueries">The blog read model.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>The blog with the specified ID, or a 404 if it could not be found.</returns>
    public static async Task< Results< Ok< BlogDto >, NotFound > > GetBlog(
        BlogId id,
        IBlogQueries blogQueries,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return TypedResults.Ok( await blogQueries.GetBlogAsync( id, cancellationToken ) );
        }
        catch ( EntityNotFoundException< Blog > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Creates a new blog.
    /// </summary>
    /// <param name="body">The details of the blog to create.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 201 carrying the newly created blog.</returns>
    public static async Task< CreatedAtRoute< BlogDto > > CreateBlog(
        CreateBlogRequestBody body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        var newBlog = await mediator.Send(
            new CreateBlogCommand( body.Title, body.OwnerId, body.Description ),
            cancellationToken
        );

        return TypedResults.CreatedAtRoute( newBlog, nameof( GetBlog ), new { id = newBlog.Id } );
    }

    /// <summary>
    /// Updates a blog by its ID.
    /// </summary>
    /// <param name="id">The ID of the blog to update.</param>
    /// <param name="body">A JSON patch document containing the changes to apply to the blog.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the blog was updated, or a 404 if it could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > UpdateBlog(
        BlogId id,
        JsonPatchDocument< Blog > body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new UpdateBlogCommand( id, body ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Blog > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Deletes a blog by its ID.
    /// </summary>
    /// <param name="id">The ID of the blog to delete.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the blog was deleted, or a 404 if it could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > DeleteBlog(
        BlogId id,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new DeleteBlogCommand( id ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Blog > )
        {
            return TypedResults.NotFound();
        }
    }

    #endregion
}
