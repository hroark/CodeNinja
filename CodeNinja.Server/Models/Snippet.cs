using Microsoft.AspNetCore.Identity;

namespace CodeNinja.Server.Models;

public class Snippet
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CopyCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string AuthorId { get; set; } = string.Empty;
    public IdentityUser Author { get; set; } = null!;
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}