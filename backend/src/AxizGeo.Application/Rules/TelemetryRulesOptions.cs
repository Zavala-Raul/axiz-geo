namespace AxizGeo.Application.Rules;

public sealed class TelemetryRulesOptions
{
    public const string SectionName = "TelemetryRules";

    public double MaxAccuracyMeters { get; set; } = 50.0;
    public int DefaultCorridorMeters { get; set; } = 150;
    public int NoSignalMinutes { get; set; } = 3;
    public int ConsecutiveDeviationsRequired { get; set; } = 2;
    public int LateThresholdSeconds { get; set; } = 120;
}
