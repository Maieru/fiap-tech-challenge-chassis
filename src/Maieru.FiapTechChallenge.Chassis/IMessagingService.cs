namespace Maieru.FiapTechChallenge.Chassis;

/// <summary>Publishes an envelope and awaits the broker's delivery confirmation.</summary>
/// <remarks>Does not coordinate database transactions or guarantee atomic delivery with a database.</remarks>
public interface IMessagingService
{
    Task PublishAsync<T>(string topic, string key, MessageEnvelope<T> message,
        CancellationToken cancellationToken = default);
}
