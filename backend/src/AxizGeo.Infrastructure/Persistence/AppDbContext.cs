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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("postgis");

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        });

        builder.Entity<Unit>(b =>
        {
            b.ToTable("units");
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
            b.ToTable("routes");
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
            b.ToTable("geofences");
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
        });

        builder.Entity<Position>(b =>
        {
            b.ToTable("positions");
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
        });
    }
}
