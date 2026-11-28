using AxizGeo.Domain.Entities;
using AxizGeo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AxizGeo.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<GeoRoute> Routes => Set<GeoRoute>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("postgis");

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });

        builder.Entity<Unit>(b =>
        {
            b.ToTable("units", t =>
            {
                t.HasCheckConstraint("CK_units_Year", "\"Year\" BETWEEN 1990 AND 2035");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.EconomicNumber).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.EconomicNumber).IsUnique();
            b.Property(x => x.Plates).HasMaxLength(20).IsRequired();
            b.Property(x => x.Brand).HasMaxLength(64).IsRequired();
            b.Property(x => x.Model).HasMaxLength(64).IsRequired();
        });

        builder.Entity<Operator>(b =>
        {
            b.ToTable("operators");
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.LicenseNumber).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.LicenseNumber).IsUnique();
            b.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.HasIndex(x => x.UserId).IsUnique();
            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GeoRoute>(b =>
        {
            b.ToTable("routes", t =>
            {
                t.HasCheckConstraint("CK_routes_CorridorMeters", "\"CorridorMeters\" BETWEEN 25 AND 2000");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.CreatedByName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Waypoints)
                .HasColumnType("geography (MultiPoint, 4326)")
                .IsRequired();
            b.Property(x => x.Path)
                .HasColumnType("geography (LineString, 4326)")
                .IsRequired();
        });

        builder.Entity<Geofence>(b =>
        {
            b.ToTable("geofences", t =>
            {
                t.HasCheckConstraint("CK_geofences_RadiusMeters", "\"RadiusMeters\" IS NULL OR \"RadiusMeters\" > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsRequired();
            b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(24).IsRequired();
            b.Property(x => x.Color).HasMaxLength(16);
            b.Property(x => x.Geometry)
                .HasColumnType("geography (Geometry, 4326)")
                .IsRequired();
            b.HasIndex(x => x.Geometry).HasMethod("GIST");
        });

        builder.Entity<Trip>(b =>
        {
            b.ToTable("trips");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasOne(x => x.Unit)
                .WithMany(u => u.Trips)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Operator)
                .WithMany(o => o.Trips)
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Route)
                .WithMany(r => r.Trips)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.Status, x.LastPositionAt });

            // Índices únicos parciales en PostgreSQL: impiden condiciones de carrera de viajes simultáneos
            b.HasIndex(x => x.UnitId)
                .IsUnique()
                .HasDatabaseName("IX_trips_UnitId_Active")
                .HasFilter("\"Status\" = 'InProgress'");

            b.HasIndex(x => x.OperatorId)
                .IsUnique()
                .HasDatabaseName("IX_trips_OperatorId_Active")
                .HasFilter("\"Status\" = 'InProgress'");
        });

        builder.Entity<Position>(b =>
        {
            b.ToTable("positions", t =>
            {
                t.HasCheckConstraint("CK_positions_AccuracyMeters", "\"AccuracyMeters\" >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Source).HasConversion<string>().HasMaxLength(24).IsRequired();
            b.Property(x => x.Location)
                .HasColumnType("geography (Point, 4326)")
                .IsRequired();

            b.HasOne(x => x.Trip)
                .WithMany(t => t.Positions)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TripId, x.CapturedAt });
            b.HasIndex(x => x.Location).HasMethod("GIST");
        });

        builder.Entity<Alert>(b =>
        {
            b.ToTable("alerts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            b.Property(x => x.Message).HasMaxLength(300).IsRequired();
            b.Property(x => x.AttendedByName).HasMaxLength(150);
            b.Property(x => x.Resolution).HasMaxLength(500);
            b.Property(x => x.Location)
                .HasColumnType("geography (Point, 4326)");

            b.HasOne(x => x.Trip)
                .WithMany(t => t.Alerts)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Geofence)
                .WithMany()
                .HasForeignKey(x => x.GeofenceId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.Status, x.CreatedAt });
            b.HasIndex(x => new { x.TripId, x.Type, x.Status });

            // Índice único parcial anti-spam: máximo 1 alerta abierta por tipo y viaje
            b.HasIndex(x => new { x.TripId, x.Type })
                .IsUnique()
                .HasDatabaseName("IX_alerts_TripId_Type_Open")
                .HasFilter("\"Status\" IN ('New', 'InReview')");
        });
    }
}
