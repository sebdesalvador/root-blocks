namespace Blogs.Api.Model;

/// <summary>
/// The body of a request adding a comment to a post.
/// </summary>
public record AddCommentRequestBody
{
    /// <summary>
    /// The identifier of the person writing the comment.
    /// </summary>
    public PersonId AuthorId { get; set; } = null!;

    /// <summary>
    /// The text of the comment.
    /// </summary>
    public string Content { get; set; } = null!;
}
