using Bedrock.Framework.Protocols;
using MQTTnet.Formatter;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttFrameWriter : IProtocolWriter<MqttFrame>
    {
        public void WriteMessage(MqttFrame message, IBufferWriter<byte> output)
        {
            var remainingLengthSize = MqttPacketWriter.GetLengthOfVariableInteger(message.Body.Length);
            var totalSize = 1 + remainingLengthSize + message.Body.Length;
            var buffer = output.GetSpan(totalSize);

            buffer[0] = message.Header;
            buffer.Slice(1).WriteVariableLengthInteger(message.Body.Length);
            message.Body.AsSpan().CopyTo(buffer.Slice(1 + remainingLengthSize));
            output.Advance(totalSize);
        }
    }
}
