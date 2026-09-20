namespace Blogs.Api.Endpoints;

/// <summary>
/// The person endpoints.
/// </summary>
public static class PersonEndpoints
{
    #region Public Methods

    /// <summary>
    /// Maps every person endpoint under <c>/person</c>.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on.</param>
    /// <returns>The group the endpoints were mapped on, to allow further configuration.</returns>
    public static RouteGroupBuilder MapPersonEndpoints( this IEndpointRouteBuilder endpoints )
    {
        var group = endpoints.MapGroup( "/person" ).WithTags( "Person" );

        group.MapGet( "/", FindPeople ).WithName( nameof( FindPeople ) );
        group.MapGet( "/{id}", GetPerson ).WithName( nameof( GetPerson ) );
        group.MapPost( "/", RegisterPerson ).WithName( nameof( RegisterPerson ) );
        group.MapPatch( "/{id}", UpdatePerson ).WithName( nameof( UpdatePerson ) )
             .Accepts< JsonPatchDocument< PersonDto > >( MediaTypeNames.Application.JsonPatch );
        group.MapDelete( "/{id}", DeletePerson ).WithName( nameof( DeletePerson ) );

        return group;
    }

    #endregion

    #region Handlers

    /// <summary>
    /// Finds people matching a search term.
    /// </summary>
    /// <param name="personQueries">The person read model.</param>
    /// <param name="searchTerm">The search terms to filter the people by.</param>
    /// <param name="pageIndex">The one-based index of the page to return.</param>
    /// <param name="pageSize">The number of people per page.</param>
    /// <param name="sortColumn">The property name (column) to sort the people by.</param>
    /// <param name="sortDirection">The direction to sort the people in.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A paged result of people, with the total count and pagination information.</returns>
    public static async Task< Ok< PagedResult< PersonDto > > > FindPeople(
        IPersonQueries personQueries,
        [ FromQuery( Name = "search-term" ) ] string? searchTerm = null,
        [ FromQuery( Name = "page-index" ) ] uint pageIndex = 1,
        [ FromQuery( Name = "page-size" ) ] uint pageSize = 10,
        [ FromQuery( Name = "sort-column" ) ] string sortColumn = "fullName",
        [ FromQuery( Name = "sort-direction" ) ] SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default
    )
    {
        var (people, total) = await personQueries.FindPeopleAsync(
            searchTerm,
            pageIndex,
            pageSize,
            sortColumn,
            sortDirection,
            cancellationToken
        );

        return TypedResults.Ok(
            new PagedResult< PersonDto >(
                people,
                total,
                new Pagination( pageIndex, pageSize, sortColumn, sortDirection )
            )
        );
    }

    /// <summary>
    /// Retrieves a person by their ID.
    /// </summary>
    /// <param name="id">The ID of the person to retrieve.</param>
    /// <param name="personQueries">The person read model.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>The person with the specified ID, or a 404 if they could not be found.</returns>
    public static async Task< Results< Ok< PersonDto >, NotFound > > GetPerson(
        PersonId id,
        IPersonQueries personQueries,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return TypedResults.Ok( await personQueries.GetPersonAsync( id, cancellationToken ) );
        }
        catch ( EntityNotFoundException< Person > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Registers a new person.
    /// </summary>
    /// <param name="body">The details of the person to register.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 201 carrying the newly registered person.</returns>
    public static async Task< CreatedAtRoute< PersonDto > > RegisterPerson(
        RegisterPersonRequestBody body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        var newPerson = await mediator.Send(
            new RegisterPersonCommand( body.FullName, body.EmailAddress ),
            cancellationToken
        );

        return TypedResults.CreatedAtRoute( newPerson, nameof( GetPerson ), new { id = newPerson.Id } );
    }

    /// <summary>
    /// Updates a person by their ID.
    /// </summary>
    /// <param name="id">The ID of the person to update.</param>
    /// <param name="body">A JSON patch document containing the changes to apply to the person.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the person was updated, or a 404 if they could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > UpdatePerson(
        PersonId id,
        JsonPatchDocument< PersonDto > body,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new UpdatePersonCommand( id, body ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Person > )
        {
            return TypedResults.NotFound();
        }
    }

    /// <summary>
    /// Deletes a person by their ID.
    /// </summary>
    /// <param name="id">The ID of the person to delete.</param>
    /// <param name="mediator">The command dispatcher.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A 204 if the person was deleted, or a 404 if they could not be found.</returns>
    public static async Task< Results< NoContent, NotFound > > DeletePerson(
        PersonId id,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await mediator.Send( new DeletePersonCommand( id ), cancellationToken );
            return TypedResults.NoContent();
        }
        catch ( EntityNotFoundException< Person > )
        {
            return TypedResults.NotFound();
        }
    }

    #endregion
}
