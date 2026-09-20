namespace Blogs.Api.Model;

/// <summary>
/// The body of a request registering a person.
/// </summary>
public record RegisterPersonRequestBody
{
    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The email address of the person.
    /// </summary>
    public string EmailAddress { get; set; } = null!;
}
