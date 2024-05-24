using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PitchSync.IdentityService.Entities;

namespace PitchSync.IdentityService.Data;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.FavoriteTeam)
                  .HasMaxLength(100);

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
