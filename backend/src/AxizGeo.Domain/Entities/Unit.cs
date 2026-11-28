namespace AxizGeo.Domain.Entities;

public class Unit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EconomicNumber { get; set; } = string.Empty;
    public string Plates { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public double? HeightMeters { get; set; }
    public double? WeightTons { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Trip> Trips { get; set; } = [];
}
