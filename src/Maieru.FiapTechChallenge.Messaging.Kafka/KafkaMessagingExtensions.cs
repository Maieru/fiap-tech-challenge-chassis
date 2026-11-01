using Confluent.Kafka;
using Maieru.FiapTechChallenge.Chassis;
using Microsoft.Extensions.DependencyInjection;

namespace Maieru.FiapTechChallenge.Messaging.Kafka;

public static class KafkaMessagingExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, Action<ProducerConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var config = new ProducerConfig { MessageTimeoutMs = 10_000 };
        configure(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.BootstrapServers);

        // These delivery settings are part of the shared publication contract.
        config.EnableIdempotence = true;
        config.Acks = Acks.All;
        _ = services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(config).Build());
        _ = services.AddSingleton<IMessagingService, KafkaMessagingService>();
        return services;
    }
}
