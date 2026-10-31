# FIAP Tech Challenge Chassis

Technical defaults for .NET 10 HTTP services. No business entities, databases or broker are included.

```csharp
builder.AddChassis(new ChassisOptions { ServiceName = "oficina-os", EnableOtlp = false });
var app = builder.Build();
app.UseChassis();
app.MapChassisHealthChecks("/api/os/v1/health");
```

Register service-specific readiness checks with `builder.Services.AddHealthChecks()`.
Enable OTLP explicitly and configure `OTEL_EXPORTER_OTLP_ENDPOINT` for your collector.
Pass business meter/activity source names through the options. `traceparent` is handled by
OpenTelemetry; the independent `X-Correlation-ID` is returned and included in log scopes.
Correlation values accept at most 128 ASCII letters, digits, dots, underscores or hyphens.
Missing/invalid correlation IDs are replaced. Exception responses expose no internal details.

`MessageEnvelope<T>` is only a serializable contract: version/payload validation, delivery,
ordering, idempotency, retries and transactions remain responsibilities of the service.

Consumers pin a NuGet version. MIT license. Source and setup:
https://github.com/Maieru/fiap-tech-challenge-chassis
