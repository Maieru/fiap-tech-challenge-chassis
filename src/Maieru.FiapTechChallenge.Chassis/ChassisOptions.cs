namespace Maieru.FiapTechChallenge.Chassis;

public sealed class ChassisOptions
{
    public required string ServiceName { get; init; }
    public string ServiceVersion { get; init; } = "0.1.0";
    public bool EnableOtlp { get; init; }
    public string[] MeterNames { get; init; } = [];
    public string[] ActivitySourceNames { get; init; } = [];
}
