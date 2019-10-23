
using Bedrock.Framework.Protocols;
using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.V3;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;

namespace Microsoft.AspNetCore.Connections
{
    public static class ConnectionContextExtensions
    {
        public static ProtocolWriter<MqttBasePacket> CreateMqttWriter(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.CreateWriter(CreateWriter(protocolVersion));
        
        public static ProtocolReader<MqttBasePacket> CreateMqttReader(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.CreateReader(CreateReader(protocolVersion));
        
        public static ProtocolReader<MqttProtocolVersion> CreateMqttVersionReader(this ConnectionContext connection)
            => connection.CreateReader(new MqttProtocolVersionReader());

        private static IProtocolWriter<MqttBasePacket> CreateWriter(MqttProtocolVersion protocolVersion)
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
                     
        private static IProtocolReader<MqttBasePacket> CreateReader(MqttProtocolVersion protocolVersion)
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
