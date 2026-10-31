using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Maieru.FiapTechChallenge.Chassis.Tests;

public sealed class ChassisTests
{
    private static async Task<WebApplication> StartAsync(bool healthy = true, HttpMessageHandler? downstream = null)
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.AddChassis(new ChassisOptions { ServiceName = "test-chassis", MeterNames = ["test"], ActivitySourceNames = ["test"] });

        if (downstream is not null)
            _ = builder.Services.AddHttpClient("downstream").ConfigurePrimaryHttpMessageHandler(() => downstream);

        _ = builder.Services.AddHealthChecks().AddCheck("database", () => healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(), tags: ["ready"]);
        var app = builder.Build();
        _ = app.UseChassis();
        _ = app.MapChassisHealthChecks();

        _ = app.MapGet("/echo", (HttpContext context, ILoggerFactory logs) =>
        {
            logs.CreateLogger("test").LogInformation("Request processed");
            return Results.Json(new { correlation = context.Items[ChassisExtensions.CorrelationHeader], traceId = Activity.Current?.TraceId.ToString() });
        });

        _ = app.MapGet("/failure", (Func<string>)(() => throw new InvalidOperationException("internal-secret-detail")));
        _ = app.MapGet("/forward", async (IHttpClientFactory factory) => Results.Content(await factory.CreateClient("downstream").GetStringAsync("http://downstream/echo"), "application/json"));
        await app.StartAsync();
        return app;
    }

    [TestCase("/health/live", false, HttpStatusCode.OK)]
    [TestCase("/health/ready", false, HttpStatusCode.ServiceUnavailable)]
    [TestCase("/health/ready", true, HttpStatusCode.OK)]
    public async Task Health_ShouldRespectDependencyBoundary(string endpoint, bool healthy, HttpStatusCode expected)
    {
        await using var app = await StartAsync(healthy);
        using var response = await app.GetTestClient().GetAsync(endpoint);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            Assert.That(body.GetProperty("status").GetString(), Is.EqualTo(expected == HttpStatusCode.OK ? "Healthy" : "Unhealthy"));
        });
    }

    [TestCase("order-123", true)]
    [TestCase("invalid id", false)]
    [TestCase("", false)]
    public async Task Correlation_ShouldReturnValidatedIdentifier(string supplied, bool preserve)
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        if (supplied.Length > 0)
            client.DefaultRequestHeaders.Add(ChassisExtensions.CorrelationHeader, supplied);
        using var response = await client.GetAsync("/echo");
        var correlation = response.Headers.GetValues(ChassisExtensions.CorrelationHeader).Single();
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(correlation, Is.Not.Empty);
            Assert.That(body.GetProperty("correlation").GetString(), Is.EqualTo(correlation));
            Assert.That(correlation == supplied, Is.EqualTo(preserve));
        });
    }

    [Test]
    public async Task Trace_ShouldContinueIncomingW3cContext()
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        const string traceId = "12345678901234567890123456789012";
        client.DefaultRequestHeaders.Add("traceparent", $"00-{traceId}-1234567890123456-01");
        var body = JsonDocument.Parse(await client.GetStringAsync("/echo")).RootElement;
        Assert.That(body.GetProperty("traceId").GetString(), Is.EqualTo(traceId));
    }

    [Test]
    public async Task Exception_ShouldReturnSafeProblemAndCorrelation()
    {
        await using var app = await StartAsync();
        using var response = await app.GetTestClient().GetAsync("/failure");
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(text, Does.Not.Contain("internal-secret-detail"));
            Assert.That(body.GetProperty("correlationId").GetString(), Is.Not.Empty);
        });
    }

    [Test]
    public async Task HttpClient_ShouldForwardRequestCorrelationToNextService()
    {
        await using var downstream = await StartAsync();
        await using var gateway = await StartAsync(downstream: downstream.GetTestServer().CreateHandler());
        using var client = gateway.GetTestClient();
        client.DefaultRequestHeaders.Add(ChassisExtensions.CorrelationHeader, "distributed-operation-123");
        var body = JsonDocument.Parse(await client.GetStringAsync("/forward")).RootElement;
        Assert.That(body.GetProperty("correlation").GetString(), Is.EqualTo("distributed-operation-123"));
    }

    [Test]
    public void Envelope_ShouldRoundTripWithoutLosingMetadata()
    {
        var envelope = new MessageEnvelope<string>(Guid.NewGuid(), "Example", 1, DateTimeOffset.UtcNow, "order-123", Guid.NewGuid(), "trace", "state", "payload");
        var restored = JsonSerializer.Deserialize<MessageEnvelope<string>>(JsonSerializer.Serialize(envelope));
        Assert.That(restored, Is.EqualTo(envelope));
    }

    [Test]
    public void AddChassis_ShouldRejectMissingServiceName()
    {
        var builder = WebApplication.CreateBuilder();
        _ = Assert.Throws<ArgumentException>(() => builder.AddChassis(new ChassisOptions { ServiceName = " " }));
    }
}
