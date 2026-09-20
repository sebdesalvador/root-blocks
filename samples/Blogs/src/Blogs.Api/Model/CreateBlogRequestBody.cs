namespace Blogs.Api.Model;

/// <summary>
/// The body of a request creating a blog.
/// </summary>
public record CreateBlogRequestBody
{
    /// <summary>
    /// The title of the blog.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// The identifier of the person owning the blog.
    /// </summary>
    public PersonId OwnerId { get; set; } = null!;

    /// <summary>
    /// An optional description of what the blog is about.
    /// </summary>
    public string? Description { get; set; }
}
