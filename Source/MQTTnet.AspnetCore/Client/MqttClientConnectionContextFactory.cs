using System;
using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;

namespace MQTTnet.AspNetCore.Client
{
    public class MqttClientConnectionContextFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return MqttConnectionChannelAdapter.ForClient(options);
        }
    }
}
