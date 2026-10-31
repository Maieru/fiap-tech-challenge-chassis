namespace Maieru.FiapTechChallenge.Chassis;

public sealed record MessageEnvelope<T>(
    Guid MessageId,
    string Type,
    int Version,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    Guid? CausationId,
    string? TraceParent,
    string? TraceState,
    T Payload);
