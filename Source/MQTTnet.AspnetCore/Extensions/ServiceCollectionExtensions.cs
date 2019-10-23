using MQTTnet.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttServer(this IServiceCollection services)
        {
            services.AddConnections();
            services.AddSingleton<MqttConnectionHandler>();
            services.AddSingleton<AspNetMqttServer>();

            return services;
        }
    }
}
