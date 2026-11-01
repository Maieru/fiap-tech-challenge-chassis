using System.Text.Json;
using Confluent.Kafka;
using Maieru.FiapTechChallenge.Chassis;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maieru.FiapTechChallenge.Messaging.Kafka.Tests;

public sealed class KafkaMessagingTests
{
    private static MessageEnvelope<OrderCreated> Envelope() => new(
        Guid.NewGuid(), "os.ordem-servico-criada", 1, DateTimeOffset.UtcNow,
        "order-correlation", Guid.NewGuid(), "00-12345678901234567890123456789012-1234567890123456-01",
        "vendor=value", new OrderCreated(Guid.NewGuid(), "Motor com falha"));

    [Test]
    public async Task Publish_ShouldAwaitConfirmationAndPreserveWireContract()
    {
        var producer = new Mock<IProducer<string, string>>(MockBehavior.Strict);
        var confirmation = new TaskCompletionSource<DeliveryResult<string, string>>();
        Message<string, string>? sent = null;
        using var cancellation = new CancellationTokenSource();
        producer.Setup(x => x.ProduceAsync("orders", It.IsAny<Message<string, string>>(), cancellation.Token))
            .Callback<string, Message<string, string>, CancellationToken>((_, message, _) => sent = message)
            .Returns(confirmation.Task);
        var envelope = Envelope();
        var publication = new KafkaMessagingService(producer.Object)
            .PublishAsync("orders", envelope.Payload.OrdemServicoId.ToString(), envelope, cancellation.Token);

        Assert.That(publication.IsCompleted, Is.False);
        confirmation.SetResult(new DeliveryResult<string, string> { Status = PersistenceStatus.Persisted });
        await publication;
        var recovered = JsonSerializer.Deserialize<MessageEnvelope<OrderCreated>>(sent!.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var json = JsonDocument.Parse(sent.Value);
        Assert.Multiple(() =>
        {
            Assert.That(sent.Key, Is.EqualTo(envelope.Payload.OrdemServicoId.ToString()));
            Assert.That(recovered, Is.EqualTo(envelope));
            Assert.That(json.RootElement.GetProperty("payload").GetProperty("descricaoProblema").GetString(),
                Is.EqualTo("Motor com falha"));
        });
        producer.VerifyAll();
    }

    [TestCase(PersistenceStatus.NotPersisted)]
    [TestCase(PersistenceStatus.PossiblyPersisted)]
    public void Publish_ShouldFail_WhenPersistenceIsNotConfirmed(PersistenceStatus status)
    {
        var producer = new Mock<IProducer<string, string>>();
        producer.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Status = status });
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            new KafkaMessagingService(producer.Object).PublishAsync("orders", "key", Envelope()));
    }

    [Test]
    public void Publish_ShouldPropagateDeliveryFailure()
    {
        var producer = new Mock<IProducer<string, string>>();
        var failure = new ProduceException<string, string>(new Error(ErrorCode.Local_MsgTimedOut),
            new DeliveryResult<string, string> { Status = PersistenceStatus.PossiblyPersisted });
        producer.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var actual = Assert.ThrowsAsync<ProduceException<string, string>>(() =>
            new KafkaMessagingService(producer.Object).PublishAsync("orders", "key", Envelope()));
        Assert.That(actual, Is.SameAs(failure));
    }

    [Test]
    public void Publish_ShouldNotSend_WhenAlreadyCancelled()
    {
        var producer = new Mock<IProducer<string, string>>(MockBehavior.Strict);
        Assert.ThrowsAsync<OperationCanceledException>(() => new KafkaMessagingService(producer.Object)
            .PublishAsync("orders", "key", Envelope(), new CancellationToken(true)));
        producer.VerifyNoOtherCalls();
    }

    [TestCase("", "key")]
    [TestCase("orders", " ")]
    public void Publish_ShouldRejectMissingRouting(string topic, string key)
    {
        var producer = new Mock<IProducer<string, string>>(MockBehavior.Strict);
        Assert.ThrowsAsync<ArgumentException>(() => new KafkaMessagingService(producer.Object)
            .PublishAsync(topic, key, Envelope()));
        producer.VerifyNoOtherCalls();
    }

    [Test]
    public void Publish_ShouldRejectNullEnvelope()
    {
        var producer = new Mock<IProducer<string, string>>(MockBehavior.Strict);
        Assert.ThrowsAsync<ArgumentNullException>(() => new KafkaMessagingService(producer.Object)
            .PublishAsync<OrderCreated>("orders", "key", null!));
        producer.VerifyNoOtherCalls();
    }

    [Test]
    public void Registration_ShouldEnforceDeliverySettingsAndOwnSingletonProducer()
    {
        var services = new ServiceCollection();
        ProducerConfig? configured = null;
        services.AddKafkaMessaging(config =>
        {
            configured = config;
            config.BootstrapServers = "localhost:9092";
            config.EnableIdempotence = false;
            config.Acks = Acks.None;
        });
        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagingService>();
        Assert.Multiple(() =>
        {
            Assert.That(publisher, Is.TypeOf<KafkaMessagingService>());
            Assert.That(provider.GetRequiredService<IMessagingService>(), Is.SameAs(publisher));
            Assert.That(configured!.EnableIdempotence, Is.True);
            Assert.That(configured.Acks, Is.EqualTo(Acks.All));
            Assert.That(configured.MessageTimeoutMs, Is.EqualTo(10_000));
        });
    }

    [Test]
    public void Registration_ShouldRejectMissingBroker()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddKafkaMessaging(_ => { }));
    }

    public sealed record OrderCreated(Guid OrdemServicoId, string DescricaoProblema);
}
