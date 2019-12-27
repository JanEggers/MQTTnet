using MQTTnet.AspNetCore.V3;
using MQTTnet.Formatter;
using System;

namespace Microsoft.AspNetCore.Connections
{
    public static class ConnectionContextExtensions
    {
        public static MqttV310Writer CreateWriter(this MqttProtocolVersion protocolVersion)
        {
            switch (protocolVersion)
            {
                case MqttProtocolVersion.V310:
                    return new MqttV310Writer();
                case MqttProtocolVersion.V311:
                    return new MqttV311Writer();
                case MqttProtocolVersion.Unknown:
                case MqttProtocolVersion.V500:
                default:
                    throw new NotSupportedException();
            }
        }
                     
        public static MqttV310Reader CreateReader(this MqttProtocolVersion protocolVersion)
        {
            switch (protocolVersion)
            {
                case MqttProtocolVersion.V310:
                    return new MqttV310Reader();
                case MqttProtocolVersion.V311:
                    return new MqttV311Reader();
                case MqttProtocolVersion.Unknown:
                case MqttProtocolVersion.V500:
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
