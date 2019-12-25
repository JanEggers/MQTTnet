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
            var bodyLength = (int)message.Body.Length;
            var remainingLengthSize = MqttPacketWriter.GetLengthOfVariableInteger(bodyLength);
            var totalSize = 1 + remainingLengthSize + bodyLength;
            var buffer = output.GetSpan(totalSize);

            buffer[0] = message.Header;
            buffer.Slice(1).WriteVariableLengthInteger(bodyLength);
            message.Body.CopyTo(buffer.Slice(1 + remainingLengthSize));
            output.Advance(totalSize);
        }
    }
}
