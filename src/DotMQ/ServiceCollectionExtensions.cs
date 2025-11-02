using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Frontend;
using Broker.Storage;

namespace DotMQ;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrokerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Broker.Core.BrokerOptions>().Bind(configuration.GetSection("Broker"));

        services.AddSingleton<IBrokerMetricsService, BrokerMetricsService>()
            .AddSingleton<PacketIdManager>()
            .AddSingleton<ISubscriptionManager, SubscriptionManager>()
            .AddSingleton<IMessageRouter, MessageRouter>()
            .AddScoped<IConnectService, ConnectService>()
            .AddScoped<IPublishService, PublishService>()
            .AddScoped<ISubscribeService, SubscribeService>()
            .AddScoped<IUnsubscribeService, UnsubscribeService>()
            .AddScoped<IQosFlowEngine, QosFlowEngine>()
            .AddScoped<PacketDispatcher>()
            .AddSingleton<IMqttTcpServer, MqttTcpServer>()
            .AddMqttClientConnectionFactory()
            .AddBrokerHostedServices()
            .ConfigureStorageServices(configuration);

        return services;
    }

    private static IServiceCollection ConfigureStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection("Storage"));
        services.AddSingleton<LiteDbProvider>();
        services.AddSingleton<IPendingStore, LiteDbPendingStore>();
        services.AddSingleton<ISessionStore, LiteDbSessionStore>();
        services.AddSingleton<IRetainedStore, LiteDbRetainedStore>();
        return services;
    }

    private static IServiceCollection AddBrokerHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<MqttTcpServerHostedService>();
        services.AddHostedService<Qos1RetryBackgroundService>();
        services.AddHostedService<StorageCleanupBackgroundService>();
        return services;
    }
}

