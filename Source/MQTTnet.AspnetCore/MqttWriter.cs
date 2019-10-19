using Bedrock.Framework.Protocols;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttWriter : IProtocolWriter<MqttBasePacket>
    {
        private readonly MqttPacketFormatterAdapter _packetFormatterAdapter;
        public long BytesSent { get; set; }

        public MqttWriter(MqttPacketFormatterAdapter packetFormatterAdapter)
        {
            _packetFormatterAdapter = packetFormatterAdapter;
        }

        public void WriteMessage(MqttBasePacket message, IBufferWriter<byte> output)
        {
            var buffer = _packetFormatterAdapter.Encode(message);
            var msg = buffer.AsSpan();
            output.Write(msg);
            BytesSent += msg.Length;
            _packetFormatterAdapter.FreeBuffer();
        }
    }
}
