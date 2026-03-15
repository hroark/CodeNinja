using CodeNinja.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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