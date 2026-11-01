using Confluent.Kafka;
using Maieru.FiapTechChallenge.Chassis;
using System.Text.Json;

namespace Maieru.FiapTechChallenge.Messaging.Kafka;

public sealed class KafkaMessagingService(IProducer<string, string> producer) : IMessagingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync<T>(string topic, string key, MessageEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(message, JsonOptions)
        }, cancellationToken);

        if (result.Status != PersistenceStatus.Persisted)
            throw new InvalidOperationException("Kafka did not confirm persistence of the message. Delivery may be uncertain.");
    }
}
