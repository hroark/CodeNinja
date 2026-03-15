using CodeNinja.Server.Data;
using CodeNinja.Server.DTOs;
using CodeNinja.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CodeNinja.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnippetsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SnippetsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? language,
        [FromQuery] string? search,
        [FromQuery] string? tag)
    {
        var query = _db.Snippets
            .Include(s => s.Author)
            .Include(s => s.Tags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(s => s.Language == language);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.Title.Contains(search) ||
                (s.Description != null && s.Description.Contains(search)));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(s => s.Tags.Any(t => t.Name == tag));

        var snippets = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SnippetDto
            {
                Id = s.Id,
                Title = s.Title,
                Code = s.Code,
                Language = s.Language,
                Description = s.Description,
                CopyCount = s.CopyCount,
                CreatedAt = s.CreatedAt,
                AuthorName = s.Author.Email ?? "Unknown",
                Tags = s.Tags.Select(t => t.Name).ToList()
            })
            .ToListAsync();

        return Ok(snippets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var snippet = await _db.Snippets
            .Include(s => s.Author)
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (snippet is null) return NotFound();

        return Ok(new SnippetDto
        {
            Id = snippet.Id,
            Title = snippet.Title,
            Code = snippet.Code,
            Language = snippet.Language,
            Description = snippet.Description,
            CopyCount = snippet.CopyCount,
            CreatedAt = snippet.CreatedAt,
            AuthorName = snippet.Author.Email ?? "Unknown",
            Tags = snippet.Tags.Select(t => t.Name).ToList()
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSnippetDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var tags = new List<Tag>();
        foreach (var name in dto.Tags ?? new List<string>())
        {
            var normalized = name.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized)) continue;

            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == normalized);
            if (tag is null)
            {
                tag = new Tag { Name = normalized };
                _db.Tags.Add(tag);
            }
            tags.Add(tag);
        }

        var snippet = new Snippet
        {
            Title = dto.Title,
            Code = dto.Code,
            Language = dto.Language,
            Description = dto.Description,
            AuthorId = userId,
            Tags = tags,
            CreatedAt = DateTime.UtcNow
        };

        _db.Snippets.Add(snippet);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = snippet.Id }, new { snippet.Id });
    }

    [Authorize]
    [HttpPost("{id}/copy")]
    public async Task<IActionResult> IncrementCopy(int id)
    {
        var snippet = await _db.Snippets.FindAsync(id);
        if (snippet is null) return NotFound();

        snippet.CopyCount++;
        await _db.SaveChangesAsync();

        return Ok(new { snippet.CopyCount });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var snippet = await _db.Snippets.FindAsync(id);
        if (snippet is null) return NotFound();
        if (snippet.AuthorId != userId) return Forbid();

        _db.Snippets.Remove(snippet);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
