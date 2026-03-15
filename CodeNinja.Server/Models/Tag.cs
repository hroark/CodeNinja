namespace CodeNinja.Server.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Snippet> Snippets { get; set; } = new List<Snippet>();
}
