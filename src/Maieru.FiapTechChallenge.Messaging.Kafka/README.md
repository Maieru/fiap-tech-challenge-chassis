# Maieru.FiapTechChallenge.Messaging.Kafka

Adaptador .NET 10 de `Maieru.FiapTechChallenge.Chassis.IMessagingService`. A versão declarada no projeto é 0.1.1. Depende de Confluent.Kafka; mantenha essa dependência na infraestrutura ou composição da aplicação.

## Instalação e registro

```powershell
dotnet add package Maieru.FiapTechChallenge.Messaging.Kafka --version 0.1.1
```

No ponto de composição, com `IServiceCollection services` e `IConfiguration configuration` disponíveis:

```csharp
using Maieru.FiapTechChallenge.Messaging.Kafka;

services.AddKafkaMessaging(config =>
{
    config.BootstrapServers = configuration["Kafka:BootstrapServers"];
    config.ClientId = "oficina-execution";
});
```

`BootstrapServers` não pode ser vazio. O callback permite configurar `ProducerConfig`, inclusive TLS/SASL e timeout. A extensão sempre força produção idempotente e `acks=all`; o timeout padrão é 10 segundos. Um único produtor e o serviço de publicação são registrados como singletons e descartados pelo container de DI.

## Publicação e garantias

Obtenha `IMessagingService` pela DI e envie tópico, chave e `MessageEnvelope<T>`. O adaptador serializa JSON camelCase, aguarda `ProduceAsync` e exige `PersistenceStatus.Persisted`. Tópicos e partições precisam ser provisionados pelo ambiente.

Aguarde as publicações antes do encerramento. Timeout ou cancelamento não retiram uma mensagem já enviada. O cliente mantém seus retries internos, mas o adaptador não inclui consumidor, retry de aplicação, Outbox ou transação de banco. A idempotência do produtor não substitui deduplicação de eventos de negócio pelo consumidor.

## Verificação e falhas

O [README do repositório](https://github.com/Maieru/fiap-tech-challenge-chassis#kafka-real-e-exemplos) contém o broker isolado, `KAFKA_TEST_BOOTSTRAP_SERVERS` e o exemplo que publica/consome pelo pacote NuGet. Brokers anunciados por `advertised.listeners` precisam ser resolvíveis do ambiente do consumidor.

Em entrega incerta, preserve chave e envelope, investigue o estado no broker e no banco e siga o [padrão de reconciliação](https://github.com/Maieru/fiap-tech-challenge/blob/main/docs/arquitetura/mensageria.md). Não interprete confirmação de persistência como conclusão do processamento de negócio. Licença MIT.
