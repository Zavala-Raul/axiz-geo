using AxizGeo.Domain.Entities;
using AxizGeo.Domain.Enums;
using AxizGeo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AxizGeo.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync(ct);

        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var demoPassword = configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogWarning("Seed:DemoPassword no está configurada; se omite la creación de usuarios demo.");
            return;
        }

        await EnsureUserAsync(userManager, "admin@axiz.demo", "Administrador Operaciones", Role.Admin, demoPassword);
        await EnsureUserAsync(userManager, "monitor@axiz.demo", "Mónica Treviño (Monitoreo)", Role.Monitor, demoPassword);

        var opUser1 = await EnsureUserAsync(userManager, "operador1@axiz.demo", "Carlos Méndez", Role.Operator, demoPassword);
        var opUser2 = await EnsureUserAsync(userManager, "operador2@axiz.demo", "Jorge Villarreal", Role.Operator, demoPassword);
        var opUser3 = await EnsureUserAsync(userManager, "operador3@axiz.demo", "Roberto Garza", Role.Operator, demoPassword);

        if (await db.Units.AnyAsync(ct))
        {
            return;
        }

        var unit1 = new Unit { EconomicNumber = "T-101", Plates = "84-AK-2L", Brand = "Kenworth", Model = "T680", Year = 2024, HeightMeters = 4.1, WeightTons = 36 };
        var unit2 = new Unit { EconomicNumber = "T-102", Plates = "91-BM-7P", Brand = "Freightliner", Model = "Cascadia", Year = 2023, HeightMeters = 4.0, WeightTons = 34 };
        var unit3 = new Unit { EconomicNumber = "T-103", Plates = "52-CN-9R", Brand = "International", Model = "LT625", Year = 2025, HeightMeters = 4.15, WeightTons = 38 };
        db.Units.AddRange(unit1, unit2, unit3);

        var op1 = new Operator { UserId = opUser1.Id, FullName = opUser1.FullName, LicenseNumber = "FED-TAM-884120", Phone = "+52 867 100 2001", Email = opUser1.Email! };
        var op2 = new Operator { UserId = opUser2.Id, FullName = opUser2.FullName, LicenseNumber = "FED-TAM-884121", Phone = "+52 867 100 2002", Email = opUser2.Email! };
        var op3 = new Operator { UserId = opUser3.Id, FullName = opUser3.FullName, LicenseNumber = "FED-TAM-884122", Phone = "+52 867 100 2003", Email = opUser3.Email! };
        db.Operators.AddRange(op1, op2, op3);

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // Ruta 1: Puente III (Comercio Mundial) -> Libramiento Mex II -> Parque Industrial Oradel (Nuevo Laredo)
        var route1Coords = new[]
        {
            new Coordinate(-99.5371, 27.5973),
            new Coordinate(-99.5455, 27.5820),
            new Coordinate(-99.5590, 27.5550),
            new Coordinate(-99.5665, 27.5230),
            new Coordinate(-99.5620, 27.4920),
            new Coordinate(-99.5480, 27.4755)
        };

        var route1 = new GeoRoute
        {
            Name = "Puente III Comercio Mundial → Parque Industrial Oradel",
            Waypoints = geometryFactory.CreateMultiPointFromCoords([route1Coords[0], route1Coords[^1]]),
            Path = geometryFactory.CreateLineString(route1Coords),
            CorridorMeters = 150,
            DistanceMeters = 14800,
            DurationSeconds = 1140,
            CreatedByName = "Administrador Operaciones"
        };

        // Ruta 2: Parque Industrial Oradel -> Carretera Nacional MX-85 Sur
        var route2Coords = new[]
        {
            new Coordinate(-99.5480, 27.4755),
            new Coordinate(-99.5360, 27.4620),
            new Coordinate(-99.5240, 27.4510),
            new Coordinate(-99.5152, 27.4426)
        };

        var route2 = new GeoRoute
        {
            Name = "Parque Industrial Oradel → Salida MX-85 Monterrey",
            Waypoints = geometryFactory.CreateMultiPointFromCoords([route2Coords[0], route2Coords[^1]]),
            Path = geometryFactory.CreateLineString(route2Coords),
            CorridorMeters = 150,
            DistanceMeters = 5200,
            DurationSeconds = 480,
            CreatedByName = "Administrador Operaciones"
        };
        db.Routes.AddRange(route1, route2);

        // Geocercas de ejemplo en Nuevo Laredo
        var customsFence = new Geofence
        {
            Name = "Recinto Aduanal Puente III",
            Type = GeofenceType.Circle,
            Kind = GeofenceKind.Allowed,
            Geometry = geometryFactory.CreatePoint(new Coordinate(-99.5371, 27.5973)),
            RadiusMeters = 500,
            Color = "#213D71"
        };

        var restrictedDowntown = new Geofence
        {
            Name = "Zona Restringida Centro Histórico",
            Type = GeofenceType.Polygon,
            Kind = GeofenceKind.Restricted,
            Geometry = geometryFactory.CreatePolygon(
            [
                new Coordinate(-99.5150, 27.5010),
                new Coordinate(-99.5010, 27.5010),
                new Coordinate(-99.5010, 27.4890),
                new Coordinate(-99.5150, 27.4890),
                new Coordinate(-99.5150, 27.5010)
            ]),
            Color = "#DC2626"
        };
        db.Geofences.AddRange(customsFence, restrictedDowntown);

        var now = DateTime.UtcNow;
        var activeTrip = new Trip
        {
            UnitId = unit1.Id,
            OperatorId = op1.Id,
            RouteId = route1.Id,
            Status = TripStatus.InProgress,
            ScheduledStart = now.AddMinutes(-20),
            StartedAt = now.AddMinutes(-15),
            LastPositionAt = now.AddMinutes(-1),
            Notes = "Cruce de importación caja seca 53 pies"
        };

        var scheduledTrip = new Trip
        {
            UnitId = unit2.Id,
            OperatorId = op2.Id,
            RouteId = route2.Id,
            Status = TripStatus.Scheduled,
            ScheduledStart = now.AddHours(1),
            Notes = "Salida programada rumbo a Monterrey"
        };
        db.Trips.AddRange(activeTrip, scheduledTrip);

        db.Positions.Add(new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            TripId = activeTrip.Id,
            Location = geometryFactory.CreatePoint(route1Coords[1]),
            AccuracyMeters = 8.5,
            SpeedKmh = 62,
            Heading = 195,
            CapturedAt = now.AddMinutes(-1),
            ReceivedAt = now.AddMinutes(-1),
            Source = PositionSource.Simulator,
            InsideCorridor = true,
            IsLate = false
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Datos semilla de Axiz Geo (Nuevo Laredo) insertados correctamente.");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        Role role,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error creando usuario semilla {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role.ToString());
        return user;
    }
}
