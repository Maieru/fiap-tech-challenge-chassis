using Maieru.FiapTechChallenge.Chassis;

var builder = WebApplication.CreateBuilder(args);
builder.AddChassis(new ChassisOptions { ServiceName = "chassis-sample" });
var app = builder.Build();
app.UseChassis();
app.MapChassisHealthChecks();
app.MapGet("/", () => Results.Ok(new { service = "chassis-sample" }));
await app.RunAsync();
