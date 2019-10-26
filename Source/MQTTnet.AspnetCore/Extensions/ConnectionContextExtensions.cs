
using Bedrock.Framework.Protocols;
using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.V3;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Threading;

namespace Microsoft.AspNetCore.Connections
{
    public static class ConnectionContextExtensions
    {
        public static ProtocolWriter<MqttBasePacket> CreateMqttPacketWriter(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.CreateWriter(CreateWriter(protocolVersion));

        public static ProtocolWriter<MqttBasePacket> CreateMqttPacketWriter(this ConnectionContext connection, MqttProtocolVersion protocolVersion, SemaphoreSlim semaphore)
              => connection.CreateWriter(CreateWriter(protocolVersion), semaphore);
        public static ProtocolWriter<MqttFrame> CreateMqttFrameWriter(this ConnectionContext connection, SemaphoreSlim semaphore)
              => connection.CreateWriter(new MqttFrameWriter(), semaphore);

        public static ProtocolReader<MqttBasePacket> CreateMqttPacketReader(this ConnectionContext connection, MqttProtocolVersion protocolVersion)
              => connection.CreateReader(CreateReader(protocolVersion));

        public static ProtocolReader<MqttFrame> CreateMqttFrameReader(this ConnectionContext connection)
              => connection.CreateReader(new MqttFrameReader());

        public static ProtocolReader<MqttProtocolVersion> CreateMqttVersionReader(this ConnectionContext connection)
            => connection.CreateReader(new MqttProtocolVersionReader());

        public static IProtocolWriter<MqttBasePacket> CreateWriter(this MqttProtocolVersion protocolVersion)
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
