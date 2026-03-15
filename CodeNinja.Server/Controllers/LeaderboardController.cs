using CodeNinja.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeNinja.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LeaderboardController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int count = 10)
    {
        var top = await _db.Snippets
            .Include(s => s.Author)
            .Include(s => s.Tags)
            .Where(s => s.CopyCount > 0)
            .OrderByDescending(s => s.CopyCount)
            .Take(count)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Language,
                s.CopyCount,
                AuthorName = s.Author.Email ?? "Unknown",
                Tags = s.Tags.Select(t => t.Name).ToList()
            })
            .ToListAsync();

        var result = top.Select((s, i) => new
        {
            Rank = i + 1,
            s.Id,
            s.Title,
            s.Language,
            s.CopyCount,
            s.AuthorName,
            s.Tags
        });

        return Ok(result);
    }
}
