Here's the complete **CodeNinja** full-stack application. The backend is an ASP.NET Core 8 Web API with Identity API Endpoints and EF Core. The frontend is a React (Vite) SPA with syntax highlighting and a modern, casual UI.

## Project Structure

```
CodeNinja/
├── CodeNinja.sln
├── CodeNinja.Server/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Models/
│   ├── Program.cs
│   └── appsettings.json
└── codeninja.client/
    ├── src/
    │   ├── components/
    │   ├── context/
    │   ├── services/
    │   ├── App.jsx, App.css, main.jsx
    ├── index.html, package.json, vite.config.js
```

## Setup

Run these commands in a terminal to scaffold the project:

```powershell
cd C:\Users\rigam\source\repos
mkdir CodeNinja && cd CodeNinja
dotnet new sln -n CodeNinja
dotnet new webapi -n CodeNinja.Server --no-openapi
dotnet sln add CodeNinja.Server
cd CodeNinja.Server
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.11
cd ..
npm create vite@latest codeninja.client -- --template react
cd codeninja.client
npm install react-router-dom react-syntax-highlighter
npm install
```

Then replace each file with the contents below.

---

## Backend

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CodeNinjaDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
```csharp
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
```
```csharp
namespace CodeNinja.Server.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Snippet> Snippets { get; set; } = new List<Snippet>();
}
```
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CodeNinja.Server.Models;

namespace CodeNinja.Server.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Snippet> Snippets => Set<Snippet>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Snippet>(entity =>
        {
            entity.HasOne(s => s.Author)
                  .WithMany()
                  .HasForeignKey(s => s.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.Tags)
                  .WithMany(t => t.Snippets);

            entity.HasIndex(s => s.CopyCount);
            entity.HasIndex(s => s.CreatedAt);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });
    }
}
```
```csharp
namespace CodeNinja.Server.DTOs;

public class CreateSnippetDto
{
    public string Title { get; set; } = "";
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class SnippetDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
    public string? Description { get; set; }
    public int CopyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}
```
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNinja.Server.Data;
using CodeNinja.Server.DTOs;
using CodeNinja.Server.Models;

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
```
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeNinja.Server.Data;

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
```
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodeNinja.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:5173", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Middleware ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/identity").MapIdentityApi<IdentityUser>();
app.MapControllers();

// Auto-create database in development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
```

---

## Frontend

```json
{
  "name": "codeninja-client",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.21.0",
    "react-syntax-highlighter": "^15.5.0"
  },
  "devDependencies": {
    "@types/react": "^18.2.43",
    "@types/react-dom": "^18.2.17",
    "@vitejs/plugin-react": "^4.2.1",
    "vite": "^5.0.8"
  }
}
```
```javascript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7239',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
```

> **Note:** adjust the `target` port to match your `Properties/launchSettings.json` HTTPS URL in `CodeNinja.Server`.

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Fira+Code:wght@400;500&display=swap" rel="stylesheet" />
    <title>CodeNinja 🥷</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.jsx"></script>
  </body>
</html>
```
```javascript
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import App from './App';
import './App.css';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
```TODO:Still Need This Somewhere
```javascript
import { Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import Home from './components/Home';
import Login from './components/Login';
import Register from './components/Register';
import CreateSnippet from './components/CreateSnippet';
import Leaderboard from './components/Leaderboard';

export default function App() {
  return (
    <div className="app">
      <Navbar />
      <main className="main-content">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/create" element={<CreateSnippet />} />
          <Route path="/leaderboard" element={<Leaderboard />} />
        </Routes>
      </main>
    </div>
  );
}
```
```css
/* ===== Theme ===== */
:root {
  --bg: #f0f4f8;
  --surface: #ffffff;
  --primary: #6366f1;
  --primary-hover: #4f46e5;
  --primary-light: #e0e7ff;
  --accent: #10b981;
  --accent-hover: #059669;
  --danger: #f43f5e;
  --warning: #f59e0b;
  --text: #1e293b;
  --text-secondary: #64748b;
  --border: #e2e8f0;
  --code-bg: #1e1e2e;
  --code-text: #cdd6f4;
  --shadow: 0 1px 3px rgba(0, 0, 0, 0.08), 0 1px 2px rgba(0, 0, 0, 0.06);
  --shadow-lg: 0 10px 25px rgba(0, 0, 0, 0.08);
  --radius: 14px;
  --radius-sm: 10px;
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  background: var(--bg);
  color: var(--text);
  line-height: 1.6;
}

/* ===== Navbar ===== */
.navbar {
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  padding: 0 2rem;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: var(--shadow);
}

.navbar-brand {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--primary);
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.navbar-links {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.navbar-links a, .navbar-links button {
  padding: 0.5rem 1rem;
  border-radius: var(--radius-sm);
  text-decoration: none;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s;
  border: none;
  cursor: pointer;
  font-family: inherit;
}

.nav-link {
  color: var(--text-secondary);
  background: none;
}

.nav-link:hover {
  color: var(--primary);
  background: var(--primary-light);
}

.nav-btn {
  background: var(--primary);
  color: white;
}

.nav-btn:hover {
  background: var(--primary-hover);
}

.nav-btn-outline {
  background: none;
  color: var(--primary);
  border: 1.5px solid var(--primary) !important;
}

.nav-btn-outline:hover {
  background: var(--primary-light);
}

.nav-user {
  color: var(--text-secondary);
  font-size: 0.85rem;
  margin-right: 0.5rem;
}

/* ===== Layout ===== */
.main-content {
  max-width: 1100px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

/* ===== Hero ===== */
.hero {
  text-align: center;
  padding: 2rem 0 2.5rem;
}

.hero h1 {
  font-size: 2.2rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.hero p {
  color: var(--text-secondary);
  font-size: 1.1rem;
}

/* ===== Filters ===== */
.filters {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 2rem;
  flex-wrap: wrap;
}

.filters input,
.filters select {
  padding: 0.65rem 1rem;
  border: 1.5px solid var(--border);
  border-radius: var(--radius-sm);
  font-size: 0.9rem;
  font-family: inherit;
  background: var(--surface);
  transition: border-color 0.2s;
  flex: 1;
  min-width: 180px;
}

.filters input:focus,
.filters select:focus {
  outline: none;
  border-color: var(--primary);
}

/* ===== Snippet Grid ===== */
.snippet-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(480px, 1fr));
  gap: 1.5rem;
}

/* ===== Snippet Card ===== */
.snippet-card {
  background: var(--surface);
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  overflow: hidden;
  transition: box-shadow 0.25s, transform 0.25s;
}

.snippet-card:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-2px);
}

.snippet-header {
  padding: 1.25rem 1.25rem 0.75rem;
}

.snippet-title-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.snippet-title {
  font-size: 1.1rem;
  font-weight: 600;
}

.language-badge {
  background: var(--primary-light);
  color: var(--primary);
  padding: 0.2rem 0.65rem;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.snippet-desc {
  color: var(--text-secondary);
  font-size: 0.88rem;
  margin-bottom: 0.5rem;
}

.snippet-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-bottom: 0.5rem;
}

.tag-pill {
  background: #f1f5f9;
  color: var(--text-secondary);
  padding: 0.15rem 0.55rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 500;
}

.snippet-code {
  background: var(--code-bg);
  max-height: 250px;
  overflow: auto;
  font-size: 0.82rem !important;
  margin: 0 !important;
  border-radius: 0 !important;
}

.snippet-code pre {
  margin: 0 !important;
  padding: 1rem !important;
}

.snippet-footer {
  padding: 0.75rem 1.25rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top: 1px solid var(--border);
}

.snippet-meta {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-size: 0.8rem;
  color: var(--text-secondary);
}

.copy-section {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.copy-count {
  background: var(--primary-light);
  color: var(--primary);
  padding: 0.15rem 0.55rem;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 600;
  min-width: 28px;
  text-align: center;
  transition: transform 0.3s;
}

.copy-count.bumped {
  transform: scale(1.3);
}

.copy-btn {
  background: var(--accent);
  color: white;
  border: none;
  padding: 0.45rem 1rem;
  border-radius: var(--radius-sm);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s, transform 0.15s;
  font-family: inherit;
}

.copy-btn:hover {
  background: var(--accent-hover);
}

.copy-btn:active {
  transform: scale(0.96);
}

.copy-btn.copied {
  background: var(--primary);
}

.copy-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* ===== Forms ===== */
.form-page {
  max-width: 500px;
  margin: 2rem auto;
}

.form-page.wide {
  max-width: 720px;
}

.form-card {
  background: var(--surface);
  padding: 2rem;
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
}

.form-card h2 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
  text-align: center;
}

.form-group {
  margin-bottom: 1.25rem;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 0.4rem;
  color: var(--text-secondary);
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.7rem 1rem;
  border: 1.5px solid var(--border);
  border-radius: var(--radius-sm);
  font-size: 0.95rem;
  font-family: inherit;
  transition: border-color 0.2s;
  background: var(--surface);
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.form-group textarea.code-input {
  font-family: 'Fira Code', monospace;
  font-size: 0.88rem;
  min-height: 200px;
  background: #fafafa;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: var(--primary);
}

.form-btn {
  width: 100%;
  padding: 0.75rem;
  background: var(--primary);
  color: white;
  border: none;
  border-radius: var(--radius-sm);
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
  font-family: inherit;
}

.form-btn:hover {
  background: var(--primary-hover);
}

.form-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.form-error {
  background: #fef2f2;
  color: var(--danger);
  padding: 0.75rem 1rem;
  border-radius: var(--radius-sm);
  font-size: 0.88rem;
  margin-bottom: 1rem;
  border: 1px solid #fecaca;
}

.form-footer {
  text-align: center;
  margin-top: 1.25rem;
  font-size: 0.9rem;
  color: var(--text-secondary);
}

.form-footer a {
  color: var(--primary);
  text-decoration: none;
  font-weight: 500;
}

/* ===== Leaderboard ===== */
.leaderboard-page h2 {
  font-size: 1.8rem;
  font-weight: 700;
  text-align: center;
  margin-bottom: 2rem;
}

.leaderboard-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.leaderboard-item {
  background: var(--surface);
  border-radius: var(--radius);
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1.25rem;
  box-shadow: var(--shadow);
  transition: transform 0.2s;
}

.leaderboard-item:hover {
  transform: translateX(4px);
}

.lb-rank {
  font-size: 1.5rem;
  font-weight: 700;
  min-width: 48px;
  text-align: center;
}

.lb-info {
  flex: 1;
}

.lb-title {
  font-weight: 600;
  font-size: 1.05rem;
}

.lb-author {
  color: var(--text-secondary);
  font-size: 0.85rem;
}

.lb-stats {
  text-align: right;
}

.lb-copies {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--primary);
}

.lb-copies-label {
  font-size: 0.75rem;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* ===== Toast ===== */
.toast {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  background: var(--code-bg);
  color: white;
  padding: 0.8rem 1.5rem;
  border-radius: var(--radius-sm);
  font-size: 0.9rem;
  font-weight: 500;
  box-shadow: var(--shadow-lg);
  animation: slideIn 0.3s ease, fadeOut 0.3s ease 1.7s forwards;
  z-index: 200;
}

@keyframes slideIn {
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

@keyframes fadeOut {
  to { opacity: 0; transform: translateY(10px); }
}

/* ===== Empty State ===== */
.empty-state {
  text-align: center;
  padding: 4rem 1rem;
  color: var(--text-secondary);
}

.empty-state span {
  font-size: 3rem;
  display: block;
  margin-bottom: 1rem;
}

/* ===== Responsive ===== */
@media (max-width: 600px) {
  .snippet-grid {
    grid-template-columns: 1fr;
  }
  .navbar {
    padding: 0 1rem;
  }
  .hero h1 {
    font-size: 1.6rem;
  }
}
```
```javascript
import { createContext, useContext, useState, useEffect, useCallback } from 'react';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem('cn_token'));
  const [user, setUser] = useState(null);

  const fetchUser = useCallback(async (t) => {
    try {
      const res = await fetch('/api/identity/manage/info', {
        headers: { Authorization: `Bearer ${t}` },
      });
      if (res.ok) {
        setUser(await res.json());
      } else {
        logout();
      }
    } catch {
      logout();
    }
  }, []);

  useEffect(() => {
    if (token) fetchUser(token);
  }, [token, fetchUser]);

  const login = async (email, password) => {
    const res = await fetch('/api/identity/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) {
      const body = await res.json().catch(() => null);
      throw new Error(body?.title || 'Invalid email or password.');
    }
    const data = await res.json();
    localStorage.setItem('cn_token', data.accessToken);
    setToken(data.accessToken);
  };

  const register = async (email, password) => {
    const res = await fetch('/api/identity/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) {
      const body = await res.json().catch(() => null);
      const msgs = body?.errors
        ? Object.values(body.errors).flat().join(' ')
        : body?.title || 'Registration failed.';
      throw new Error(msgs);
    }
  };

  const logout = () => {
    localStorage.removeItem('cn_token');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ token, user, login, register, logout, isAuthenticated: !!token }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
```
```javascript
const hdrs = (token) => {
  const h = { 'Content-Type': 'application/json' };
  if (token) h['Authorization'] = `Bearer ${token}`;
  return h;
};

const api = {
  getSnippets: (params = {}) => {
    const q = new URLSearchParams(
      Object.fromEntries(Object.entries(params).filter(([, v]) => v))
    ).toString();
    return fetch(`/api/snippets${q ? '?' + q : ''}`).then((r) => r.json());
  },

  createSnippet: (data, token) =>
    fetch('/api/snippets', {
      method: 'POST',
      headers: hdrs(token),
      body: JSON.stringify(data),
    }),

  copySnippet: (id, token) =>
    fetch(`/api/snippets/${id}/copy`, {
      method: 'POST',
      headers: hdrs(token),
    }).then((r) => r.json()),

  getLeaderboard: (count = 10) =>
    fetch(`/api/leaderboard?count=${count}`).then((r) => r.json()),
};

export default api;
```
```javascript
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { isAuthenticated, user, logout } = useAuth();

  return (
    <nav className="navbar">
      <Link to="/" className="navbar-brand">
        🥷 CodeNinja
      </Link>
      <div className="navbar-links">
        <Link to="/" className="nav-link">Browse</Link>
        <Link to="/leaderboard" className="nav-link">🏆 Leaderboard</Link>
        {isAuthenticated ? (
          <>
            <Link to="/create" className="nav-btn">+ New Snippet</Link>
            <span className="nav-user">{user?.email?.split('@')[0]}</span>
            <button onClick={logout} className="nav-link">Logout</button>
          </>
        ) : (
          <>
            <Link to="/login" className="nav-btn-outline">Log In</Link>
            <Link to="/register" className="nav-btn">Sign Up</Link>
          </>
        )}
      </div>
    </nav>
  );
}
```
```javascript
import { useState } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { useAuth } from '../context/AuthContext';
import api from '../services/api';

export default function SnippetCard({ snippet }) {
  const { isAuthenticated, token } = useAuth();
  const [copyCount, setCopyCount] = useState(snippet.copyCount);
  const [copied, setCopied] = useState(false);
  const [bumped, setBumped] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(snippet.code);
      const result = await api.copySnippet(snippet.id, token);
      setCopyCount(result.copyCount);
      setCopied(true);
      setBumped(true);
      setTimeout(() => setCopied(false), 2000);
      setTimeout(() => setBumped(false), 400);
    } catch (err) {
      console.error('Copy failed', err);
    }
  };

  const langMap = {
    'c#': 'csharp', 'c++': 'cpp', 'shell': 'bash',
    'objective-c': 'objectivec',
  };
  const prismLang = langMap[snippet.language?.toLowerCase()] || snippet.language?.toLowerCase() || 'text';

  return (
    <div className="snippet-card">
      <div className="snippet-header">
        <div className="snippet-title-row">
          <span className="snippet-title">{snippet.title}</span>
          <span className="language-badge">{snippet.language}</span>
        </div>
        {snippet.description && <p className="snippet-desc">{snippet.description}</p>}
        {snippet.tags?.length > 0 && (
          <div className="snippet-tags">
            {snippet.tags.map((t) => (
              <span key={t} className="tag-pill">#{t}</span>
            ))}
          </div>
        )}
      </div>

      <div className="snippet-code">
        <SyntaxHighlighter
          language={prismLang}
          style={oneDark}
          customStyle={{ margin: 0, borderRadius: 0, fontSize: '0.82rem' }}
          wrapLongLines
        >
          {snippet.code}
        </SyntaxHighlighter>
      </div>

      <div className="snippet-footer">
        <div className="snippet-meta">
          <span>👤 {snippet.authorName?.split('@')[0]}</span>
          <span>{new Date(snippet.createdAt).toLocaleDateString()}</span>
        </div>
        <div className="copy-section">
          <span className={`copy-count${bumped ? ' bumped' : ''}`}>{copyCount}</span>
          {isAuthenticated ? (
            <button className={`copy-btn${copied ? ' copied' : ''}`} onClick={handleCopy}>
              {copied ? '✓ Copied!' : '📋 Copy'}
            </button>
          ) : (
            <button className="copy-btn" disabled title="Log in to copy">
              🔒 Copy
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
```
```javascript
import { useEffect, useState } from 'react';
import api from '../services/api';
import SnippetCard from './SnippetCard';

const LANGUAGES = [
  '', 'javascript', 'typescript', 'python', 'csharp', 'java', 'cpp', 'go',
  'rust', 'ruby', 'php', 'swift', 'kotlin', 'sql', 'html', 'css', 'bash', 'powershell',
];

export default function Home() {
  const [snippets, setSnippets] = useState([]);
  const [search, setSearch] = useState('');
  const [language, setLanguage] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setLoading(true);
      api.getSnippets({ search, language }).then((data) => {
        setSnippets(data);
        setLoading(false);
      });
    }, 300);
    return () => clearTimeout(timeout);
  }, [search, language]);

  return (
    <>
      <div className="hero">
        <h1>🥷 Share Code Like a Ninja</h1>
        <p>Discover, copy, and share useful code snippets with developers everywhere.</p>
      </div>

      <div className="filters">
        <input
          type="text"
          placeholder="🔍  Search snippets..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select value={language} onChange={(e) => setLanguage(e.target.value)}>
          <option value="">All Languages</option>
          {LANGUAGES.filter(Boolean).map((l) => (
            <option key={l} value={l}>{l}</option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="empty-state"><span>⏳</span>Loading...</div>
      ) : snippets.length === 0 ? (
        <div className="empty-state">
          <span>📭</span>
          No snippets found. Be the first to share one!
        </div>
      ) : (
        <div className="snippet-grid">
          {snippets.map((s) => (
            <SnippetCard key={s.id} snippet={s} />
          ))}
        </div>
      )}
    </>
  );
}
```
```javascript
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-page">
      <div className="form-card">
        <h2>🥷 Welcome Back</h2>
        {error && <div className="form-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Email</label>
            <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="form-group">
            <label>Password</label>
            <input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          <button className="form-btn" disabled={loading}>
            {loading ? 'Logging in...' : 'Log In'}
          </button>
        </form>
        <div className="form-footer">
          Don&apos;t have an account? <Link to="/register">Sign up</Link>
        </div>
      </div>
    </div>
  );
}
```
```javascript
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Register() {
  const { register, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }
    setLoading(true);
    try {
      await register(email, password);
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-page">
      <div className="form-card">
        <h2>🥷 Join CodeNinja</h2>
        {error && <div className="form-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Email</label>
            <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="form-group">
            <label>Password</label>
            <input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          <div className="form-group">
            <label>Confirm Password</label>
            <input type="password" required value={confirm} onChange={(e) => setConfirm(e.target.value)} />
          </div>
          <button className="form-btn" disabled={loading}>
            {loading ? 'Creating account...' : 'Sign Up'}
          </button>
        </form>
        <div className="form-footer">
          Already have an account? <Link to="/login">Log in</Link>
        </div>
      </div>
    </div>
  );
}
```
```javascript
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../services/api';

const LANGUAGES = [
  'javascript', 'typescript', 'python', 'csharp', 'java', 'cpp', 'go',
  'rust', 'ruby', 'php', 'swift', 'kotlin', 'sql', 'html', 'css', 'bash', 'powershell',
];

export default function CreateSnippet() {
  const { token, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    title: '', code: '', language: 'javascript', description: '', tags: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isAuthenticated) {
    return (
      <div className="empty-state">
        <span>🔒</span>Please log in to create a snippet.
      </div>
    );
  }

  const handleChange = (e) =>
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const tags = form.tags
        .split(',')
        .map((t) => t.trim())
        .filter(Boolean);

      const res = await api.createSnippet(
        { title: form.title, code: form.code, language: form.language, description: form.description, tags },
        token
      );

      if (!res.ok) {
        throw new Error('Failed to create snippet.');
      }
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-page wide">
      <div className="form-card">
        <h2>✨ New Snippet</h2>
        {error && <div className="form-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Title</label>
            <input name="title" required value={form.title} onChange={handleChange} placeholder="e.g. Debounce hook in React" />
          </div>
          <div className="form-group">
            <label>Language</label>
            <select name="language" value={form.language} onChange={handleChange}>
              {LANGUAGES.map((l) => (
                <option key={l} value={l}>{l}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Tags (comma-separated)</label>
            <input name="tags" value={form.tags} onChange={handleChange} placeholder="e.g. react, hooks, utility" />
          </div>
          <div className="form-group">
            <label>Description (optional)</label>
            <textarea name="description" value={form.description} onChange={handleChange} placeholder="A short description of what this snippet does..." />
          </div>
          <div className="form-group">
            <label>Code</label>
            <textarea className="code-input" name="code" required value={form.code} onChange={handleChange} placeholder="Paste your code here..." />
          </div>
          <button className="form-btn" disabled={loading}>
            {loading ? 'Publishing...' : '🚀 Publish Snippet'}
          </button>
        </form>
      </div>
    </div>
  );
}
```
```javascript
import { useEffect, useState } from 'react';
import api from '../services/api';

const medals = ['🥇', '🥈', '🥉'];

export default function Leaderboard() {
  const [entries, setEntries] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getLeaderboard(15).then((data) => {
      setEntries(data);
      setLoading(false);
    });
  }, []);

  if (loading) {
    return <div className="empty-state"><span>⏳</span>Loading leaderboard...</div>;
  }

  return (
    <div className="leaderboard-page">
      <h2>🏆 Most Copied Snippets</h2>
      {entries.length === 0 ? (
        <div className="empty-state">
          <span>📭</span>No snippets have been copied yet. Be the first!
        </div>
      ) : (
        <div className="leaderboard-list">
          {entries.map((e) => (
            <div className="leaderboard-item" key={e.id}>
              <div className="lb-rank">
                {e.rank <= 3 ? medals[e.rank - 1] : `#${e.rank}`}
              </div>
              <div className="lb-info">
                <div className="lb-title">{e.title}</div>
                <div className="lb-author">
                  by {e.authorName?.split('@')[0]} &middot;{' '}
                  <span className="language-badge" style={{ fontSize: '0.7rem' }}>{e.language}</span>
                  {e.tags?.map((t) => (
                    <span key={t} className="tag-pill" style={{ marginLeft: '0.25rem' }}>#{t}</span>
                  ))}
                </div>
              </div>
              <div className="lb-stats">
                <div className="lb-copies">{e.copyCount}</div>
                <div className="lb-copies-label">copies</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

## Create the Database & Run

1. **Apply migrations** (from the solution root):

```powershell
cd C:\Users\rigam\source\repos\CodeNinja\CodeNinja.Server
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> The `Program.cs` also calls `EnsureCreated()` so the database will be auto-created if no migrations exist yet. For production, use migrations.

2. **Start the backend** (in one terminal):

```powershell
cd C:\Users\rigam\source\repos\CodeNinja\CodeNinja.Server
dotnet run --launch-profile https
```

Note the HTTPS port from the console output (e.g. `https://localhost:7239`). If it differs from `7239`, update the `target` in `vite.config.js`.

3. **Start the frontend** (in another terminal):

```powershell
cd C:\Users\rigam\source\repos\CodeNinja\codeninja.client
npm run dev
```

4. Open **https://localhost:5173** in your browser.

---

### Key Features Summary

| Feature | How It Works |
|---|---|
| **Registration / Login** | ASP.NET Core Identity API endpoints (`/api/identity/register`, `/api/identity/login`) with bearer tokens |
| **Create Snippet** | Authenticated `POST /api/snippets` with title, code, language, tags, description |
| **Copy & Count** | `📋 Copy` button copies code to clipboard + calls `POST /api/snippets/{id}/copy` to increment counter |
| **Leaderboard** | `GET /api/leaderboard` returns top snippets ranked by copy count |
| **Filtering** | Search by title/description and filter by language on the home page |
| **Syntax Highlighting** | `react-syntax-highlighter` with the One Dark theme |