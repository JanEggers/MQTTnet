using System.Collections.Immutable;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServer 
    {
        public ImmutableArray<MqttServerConnection> Connections { get; set; } = ImmutableArray<MqttServerConnection>.Empty;
    }
}
