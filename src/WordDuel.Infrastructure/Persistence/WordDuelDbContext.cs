using Microsoft.EntityFrameworkCore;

namespace WordDuel.Infrastructure.Persistence;

public class WordDuelDbContext : DbContext
{
    public WordDuelDbContext(DbContextOptions<WordDuelDbContext> options) : base(options)
    {
    }

    public DbSet<MatchEntity> Matches => Set<MatchEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<MoveEntity> Moves => Set<MoveEntity>();
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.BoardStateFlat).HasMaxLength(49).IsRequired();
            entity.Property(m => m.Version).IsConcurrencyToken();
            entity.HasMany(m => m.Players)
                .WithOne(p => p.Match)
                .HasForeignKey(p => p.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(m => m.Moves)
                .WithOne(mv => mv.Match)
                .HasForeignKey(mv => mv.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisplayName).HasMaxLength(40).IsRequired();
            entity.Property(p => p.RackFlat).HasMaxLength(7);
            entity.HasIndex(p => new { p.MatchId, p.Seat }).IsUnique();
        });

        modelBuilder.Entity<MoveEntity>(entity =>
        {
            entity.ToTable("moves");
            entity.HasKey(mv => mv.Id);
            entity.HasIndex(mv => new { mv.MatchId, mv.SequenceNumber }).IsUnique();
        });

        modelBuilder.Entity<IdempotencyRecordEntity>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.MatchId, i.PlayerId, i.Endpoint, i.IdempotencyKey }).IsUnique();
        });
    }
}
