namespace Blogs.Api.Model;

/// <summary>
/// The body of a request publishing a post on a blog.
/// </summary>
public record CreatePostRequestBody
{
    /// <summary>
    /// The identifier of the blog the post belongs to.
    /// </summary>
    public BlogId BlogId { get; set; } = null!;

    /// <summary>
    /// The title of the post.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// The body of the post.
    /// </summary>
    public string Content { get; set; } = null!;
}
