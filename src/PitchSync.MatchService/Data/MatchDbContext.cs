using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Entities;

namespace PitchSync.MatchService.Data;

public sealed class MatchDbContext : DbContext
{
    public MatchDbContext(DbContextOptions<MatchDbContext> options) : base(options) { }

    public DbSet<MatchRoom> MatchRooms => Set<MatchRoom>();
    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<PlayerLineup> PlayerLineups => Set<PlayerLineup>();
    public DbSet<PlayerRating> PlayerRatings => Set<PlayerRating>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── MatchRoom ────────────────────────────────────────────────────────
        builder.Entity<MatchRoom>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
            entity.Property(r => r.HomeTeam).IsRequired().HasMaxLength(100);
            entity.Property(r => r.AwayTeam).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Competition).HasMaxLength(100);
            entity.Property(r => r.CreatedByUserId).IsRequired();
            entity.Property(r => r.InviteCode).HasMaxLength(8);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(r => r.IsPublic).HasDefaultValue(true);
            entity.Property(r => r.HomeScore).HasDefaultValue(0);
            entity.Property(r => r.AwayScore).HasDefaultValue(0);
        });

        // ── RoomParticipant ──────────────────────────────────────────────────
        builder.Entity<RoomParticipant>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.JoinedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => new { p.MatchRoomId, p.UserId }).IsUnique();

            entity.HasOne(p => p.MatchRoom)
                  .WithMany(r => r.Participants)
                  .HasForeignKey(p => p.MatchRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MatchEvent ───────────────────────────────────────────────────────
        builder.Entity<MatchEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PostedByUserId).IsRequired();
            entity.Property(e => e.PostedByDisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Team).HasMaxLength(10);
            entity.Property(e => e.PlayerName).HasMaxLength(100);
            entity.Property(e => e.SecondaryPlayerName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => new { e.MatchRoomId, e.Minute });
            entity.HasIndex(e => new { e.MatchRoomId, e.CreatedAt });

            entity.HasOne(e => e.MatchRoom)
                  .WithMany(r => r.Events)
                  .HasForeignKey(e => e.MatchRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerLineup ─────────────────────────────────────────────────────
        builder.Entity<PlayerLineup>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Team).IsRequired().HasMaxLength(10);
            entity.Property(l => l.PlayerName).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Position).HasMaxLength(30);
            entity.Property(l => l.AddedByUserId).IsRequired();
            entity.Property(l => l.IsStarting).HasDefaultValue(true);

            entity.HasOne(l => l.MatchRoom)
                  .WithMany(r => r.PlayerLineups)
                  .HasForeignKey(l => l.MatchRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerRating ─────────────────────────────────────────────────────
        builder.Entity<PlayerRating>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.PlayerName).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Team).IsRequired().HasMaxLength(10);
            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.Rating).HasPrecision(3, 1);

            entity.HasIndex(r => new { r.MatchRoomId, r.PlayerName, r.Team, r.UserId }).IsUnique();

            entity.HasOne(r => r.MatchRoom)
                  .WithMany(rm => rm.PlayerRatings)
                  .HasForeignKey(r => r.MatchRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
