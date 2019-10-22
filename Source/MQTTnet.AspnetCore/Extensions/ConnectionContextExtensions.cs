
using Bedrock.Framework.Protocols;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace Microsoft.AspNetCore.Connections
{
    public static class ConnectionContextExtensions
    {
        public static ProtocolWriter<MqttBasePacket> WriteMqtt(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.WriteProtocol(CreateWriter(protocolVersion));
        
        public static ProtocolReader<MqttBasePacket> ReadMqtt(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.ReadProtocol(CreateReader(protocolVersion));
        
        public static ProtocolReader<MqttProtocolVersion> ReadMqttVersion(this ConnectionContext connection)
            => connection.ReadProtocol(new MqttProtocolVersionReader());

        private static IProtocolWriter<MqttBasePacket> CreateWriter(MqttProtocolVersion protocolVersion)
        {
            return new MqttProtocolWriter(new MqttPacketFormatterAdapter(protocolVersion, new SpanBasedMqttPacketWriter()));
        }
                     
        private static IProtocolReader<MqttBasePacket> CreateReader(MqttProtocolVersion protocolVersion)
        {
            return new MqttProtocolReader(new MqttPacketFormatterAdapter(protocolVersion, new SpanBasedMqttPacketWriter()));
        }
    }
}
