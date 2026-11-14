# Maieru.FiapTechChallenge.Chassis

Recursos técnicos para serviços HTTP .NET 10: logs JSON, OpenTelemetry, correlação, ProblemDetails e health checks. A versão declarada no projeto é 1.1.1. Não contém entidades de negócio, banco ou dependência de Kafka.

## Uso

Instale uma versão disponível no NuGet:

```powershell
dotnet add package Maieru.FiapTechChallenge.Chassis --version 1.1.1
```

No `Program.cs` de uma aplicação ASP.NET Core:

```csharp
using Maieru.FiapTechChallenge.Chassis;

var builder = WebApplication.CreateBuilder(args);
builder.AddChassis(new ChassisOptions
{
    ServiceName = "oficina-os",
    ServiceVersion = "0.1.0",
    EnableOtlp = false,
    MeterNames = ["Oficina.Business"],
    ActivitySourceNames = ["Oficina.Application"]
});
var app = builder.Build();
app.UseChassis();
app.MapChassisHealthChecks("/api/os/v1/health");
app.Run();
```

`ServiceName` é obrigatório. `ServiceVersion` identifica a aplicação, não a versão NuGet. Habilite OTLP explicitamente e configure `OTEL_EXPORTER_OTLP_ENDPOINT` e protocolo compatíveis com o Collector. Não entregue a chave do provedor de telemetria ao navegador.

## Saúde, correlação e erros

Registre readiness específica com `builder.Services.AddHealthChecks()`. `/live` executa checks com tag `live`; `/ready` executa todos os checks registrados e retorna status/dependências em JSON. As rotas mapeadas são anônimas.

`X-Correlation-ID` aceita de 1 a 128 letras/dígitos ASCII, pontos, underscores e hífens. Ausente ou inválido, recebe outro ID. O middleware devolve o valor e o inclui nos scopes de log; clientes criados por `IHttpClientFactory` o propagam. `traceparent` segue a instrumentação OpenTelemetry. Exceções não tratadas retornam ProblemDetails com IDs de trace/correlação, sem detalhes internos.

`AddChassis` substitui os providers de log por JSON console e OpenTelemetry. Os meters e activity sources de negócio precisam ser informados nas opções; criar nomes sem instrumentação não gera métricas ou spans automaticamente.

## Mensageria

`MessageEnvelope<T>` contém MessageId, Type, Version, OccurredAtUtc, CorrelationId, CausationId, TraceParent, TraceState e Payload. É um contrato serializável; validação, ordenação, idempotência, retries e transações pertencem ao serviço.

`IMessagingService.PublishAsync(topic, key, envelope, cancellationToken)` não depende de Kafka. O pacote separado `Maieru.FiapTechChallenge.Messaging.Kafka` implementa publicação confirmada. A confirmação não coordena banco e broker atomicamente.

- [Código, build e testes](https://github.com/Maieru/fiap-tech-challenge-chassis).
- [Padrão de mensageria](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md).

Licença MIT.
