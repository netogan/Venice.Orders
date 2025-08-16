using Venice.Orders.Application.Services;
using Venice.Orders.Infrastructure.Kafka;

namespace Venice.Orders.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddScoped<IPedidoEventService, KafkaPedidoEventService>();
        
        return services;
    }
}
