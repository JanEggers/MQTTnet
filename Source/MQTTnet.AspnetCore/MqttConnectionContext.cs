using Microsoft.AspNetCore.Connections;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;

using Protocols = Bedrock.Framework.Protocols;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionContext : Protocols.Protocol<MqttProtocolReader, MqttProtocolWriter, MqttBasePacket, MqttBasePacket>
    {
        private readonly MqttProtocolReader _reader;
        private readonly MqttProtocolWriter _writer;

        public MqttConnectionContext(
            ConnectionContext connection,
            MqttPacketFormatterAdapter adapter,
            MqttProtocolReader reader, 
            MqttProtocolWriter writer)
            : base(connection, reader, writer, null)
        {
            _reader = reader;
            _writer = writer;

            PacketFormatterAdapter = adapter;
        }

        public static MqttConnectionContext Create(ConnectionContext connection, MqttProtocolVersion? protocolVersion = null)
        {
            var writer = new SpanBasedMqttPacketWriter();
            MqttPacketFormatterAdapter formatter = null;

            if (protocolVersion.HasValue)
            {
                formatter = new MqttPacketFormatterAdapter(protocolVersion.Value, writer);
            }
            else
            {
                formatter = new MqttPacketFormatterAdapter(writer);
            }

            var reader = new MqttProtocolReader(formatter);
            var pwriter = new MqttProtocolWriter(formatter);

            return new MqttConnectionContext(connection, formatter, reader, pwriter);
        }

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent { get => _writer.BytesSent; set => _writer.BytesSent = value; }
        public long BytesReceived { get => _reader.BytesReceived; set => _reader.BytesReceived = value; }

        public Action ReadingPacketStartedCallback { get => _reader.ReadingPacketStartedCallback; set => _reader.ReadingPacketStartedCallback = value; }
        public Action ReadingPacketCompletedCallback { get => _reader.ReadingPacketCompletedCallback; set => _reader.ReadingPacketCompletedCallback = value; }
    }
}
