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
        public static IServiceCollection AddMqttClient<T>(this IServiceCollection services)
            where T : class
        {
            services.AddConnections();
            services.AddScoped<T>();

            return services;
        }
    }
}
