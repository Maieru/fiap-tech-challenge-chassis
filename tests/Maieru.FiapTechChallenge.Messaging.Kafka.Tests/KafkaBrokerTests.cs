using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Maieru.FiapTechChallenge.Chassis;
using Microsoft.Extensions.DependencyInjection;

namespace Maieru.FiapTechChallenge.Messaging.Kafka.Tests;

[Category("Kafka")]
public sealed class KafkaBrokerTests
{
    [Test]
    public async Task Publish_ShouldDeliverKeyEnvelopeAndPayload_ToRealKafka()
    {
        var bootstrap = Environment.GetEnvironmentVariable("KAFKA_TEST_BOOTSTRAP_SERVERS");
        if (string.IsNullOrWhiteSpace(bootstrap))
            Assert.Ignore("Set KAFKA_TEST_BOOTSTRAP_SERVERS to run the real Kafka integration test.");

        var topic = $"messaging-test-{Guid.NewGuid():N}";
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
        await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);
        try
        {
            var services = new ServiceCollection();
            services.AddKafkaMessaging(config =>
            {
                config.BootstrapServers = bootstrap;
                config.Debug = Environment.GetEnvironmentVariable("KAFKA_TEST_DEBUG");
            });
            await using var provider = services.BuildServiceProvider();
            var envelope = new MessageEnvelope<KafkaMessagingTests.OrderCreated>(Guid.NewGuid(), "os.ordem-servico-criada",
                1, DateTimeOffset.UtcNow, "integration-test", null, null, null,
                new KafkaMessagingTests.OrderCreated(Guid.NewGuid(), "Motor com falha"));
            var key = envelope.Payload.OrdemServicoId.ToString();
            await provider.GetRequiredService<IMessagingService>().PublishAsync(topic, key, envelope);

            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = bootstrap, GroupId = $"test-{Guid.NewGuid():N}",
                EnableAutoCommit = false, EnableAutoOffsetStore = false, AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();
            consumer.Subscribe(topic);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                var received = consumer.Consume(timeout.Token);
                var recovered = JsonSerializer.Deserialize<MessageEnvelope<KafkaMessagingTests.OrderCreated>>(
                    received.Message.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                Assert.Multiple(() =>
                {
                    Assert.That(received.Message.Key, Is.EqualTo(key));
                    Assert.That(recovered, Is.EqualTo(envelope));
                });
                consumer.Commit(received);
            }
            finally { consumer.Close(); }
        }
        finally { await admin.DeleteTopicsAsync([topic]); }
    }
}
