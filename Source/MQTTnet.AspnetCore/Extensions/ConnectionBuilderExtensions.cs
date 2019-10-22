using MQTTnet.AspNetCore;

namespace Microsoft.AspNetCore.Connections
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseMqtt(this IConnectionBuilder builder)
        {
            return builder.UseConnectionHandler<MqttConnectionHandler>();
        }
    }
}
