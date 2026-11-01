using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Maieru.FiapTechChallenge.Chassis;
using Maieru.FiapTechChallenge.Messaging.Kafka;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var bootstrap = args.Length > 0 ? args[0] : "127.0.0.1:19092";
var topic = $"package-consumer-{Guid.NewGuid():N}";
using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);

try
{
    var services = new ServiceCollection();
    _ = services.AddKafkaMessaging(config => config.BootstrapServers = bootstrap);
    await using var provider = services.BuildServiceProvider();
    var orderId = Guid.NewGuid();
    var envelope = new MessageEnvelope<OrderCreated>(Guid.NewGuid(), "sample.order-created", 1,
        DateTimeOffset.UtcNow, "package-consumer", null, null, null, new OrderCreated(orderId));

    await provider.GetRequiredService<IMessagingService>().PublishAsync(topic, orderId.ToString(), envelope);

    using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = $"sample-{Guid.NewGuid():N}",
        EnableAutoCommit = false,
        EnableAutoOffsetStore = false,
        AutoOffsetReset = AutoOffsetReset.Earliest
    }).Build();

    consumer.Subscribe(topic);

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        var received = consumer.Consume(timeout.Token);
        var recovered = JsonSerializer.Deserialize<MessageEnvelope<OrderCreated>>(received.Message.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (received.Message.Key != orderId.ToString() || recovered != envelope)
            throw new InvalidOperationException("NuGet consumer received a different message.");
        consumer.Commit(received);
        Console.WriteLine("NuGet consumer: key, envelope and payload verified against real Kafka.");
    }
    finally { consumer.Close(); }
}
finally { await admin.DeleteTopicsAsync([topic]); }

internal sealed record OrderCreated(Guid OrdemServicoId);
