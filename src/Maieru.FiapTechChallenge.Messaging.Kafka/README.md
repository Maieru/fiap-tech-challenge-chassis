# Kafka messaging

.NET 10 adapter implementing `Maieru.FiapTechChallenge.Chassis.IMessagingService`.
Register it in Infrastructure or the application's composition root:

```csharp
services.AddKafkaMessaging(config =>
{
    config.BootstrapServers = configuration["Kafka:BootstrapServers"];
    config.ClientId = "oficina-execution";
});
```

`PublishAsync` awaits `ProduceAsync` and requires `PersistenceStatus.Persisted`.
The adapter uses camelCase JSON, a singleton producer, idempotent production,
`acks=all`, and a configurable delivery timeout (10 seconds by default).
The registration always enforces idempotence and `acks=all`; other Kafka settings,
including TLS/SASL, are configured through `ProducerConfig`.

The container owns and disposes the producer. Await all publication calls before
shutting down. Cancellation and timeouts do not retract a message already sent.
No database transaction, consumer, outbox, or application retry is hidden here.
Kafka's internal delivery retries are still enabled by the client.

Full standard and failure recovery: [messaging guide](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md).
