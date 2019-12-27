using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using MQTTnet.AspNetCore.V3;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public abstract class MqttConnection
    {
        public ConnectionContext Connection { get; }
        private readonly ProtocolWriter _writer;
        private readonly ProtocolReader _reader;
        private readonly MqttV310Writer _mqttMessageWriter;
        protected readonly MqttV310Reader _mqttMessageReader;

        protected MqttConnection(ConnectionContext connection, MqttProtocolVersion protocolVersion)
        {
            Connection = connection;

            _writer = connection.CreateWriter();
            _reader = connection.CreateReader();

            _mqttMessageWriter = protocolVersion.CreateWriter();
            _mqttMessageReader = protocolVersion.CreateReader();
        }
               
        public ValueTask WritePacket(MqttBasePacket packet, CancellationToken ct)
        {
            return _writer.WriteAsync(_mqttMessageWriter, packet, ct);
        }

        public ValueTask WritePacket(MqttBasePacket packet)
            => WritePacket(packet, Connection.ConnectionClosed);

        public ValueTask WriteFrame(in MqttFrame frame, CancellationToken ct)
        {
            return _writer.WriteAsync(_mqttMessageWriter.FrameWriter, frame, ct);
        }

        public ValueTask<ReadResult<MqttFrame>> ReadFrame(CancellationToken ct)
        {
            return _reader.ReadAsync(_mqttMessageReader.FrameReader, ct);
        }

        public ValueTask<ReadResult<MqttFrame>> ReadFrame()
            => ReadFrame(Connection.ConnectionClosed);

        public void Advance()
        {
            _reader.Advance();
        }
    }
}
