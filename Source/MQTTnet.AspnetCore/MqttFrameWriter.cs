using Bedrock.Framework.Protocols;
using MQTTnet.Formatter;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttFrameWriter : IMessageWriter<MqttFrame>
    {
        public void WriteMessage(MqttFrame message, IBufferWriter<byte> output)
        {
            var bodyLength = (int)message.Body.Length;
            var remainingLengthSize = MqttPacketWriter.GetLengthOfVariableInteger(bodyLength);
            var headerSize = 1 + remainingLengthSize;


            var buffer = output.GetSpan(headerSize);
            buffer[0] = message.Header;
            buffer.Slice(1).WriteVariableLengthInteger(bodyLength);
            output.Advance(headerSize);
            
            if (message.Body.IsSingleSegment)
            {
                var remainingSize = bodyLength;
                var start = 0;

                const int chunkSize = 4096;
                while (remainingSize > chunkSize)
                {
                    buffer = output.GetSpan(chunkSize);

                    message.Body.Slice(start, chunkSize).CopyTo(buffer);
                    output.Advance(chunkSize);
                    start += chunkSize;
                    remainingSize -= chunkSize;
                }

                buffer = output.GetSpan(remainingSize);
                message.Body.Slice(start, remainingSize).CopyTo(buffer);
                output.Advance(remainingSize);
            }
            else
            {
                foreach (var mem in message.Body)
                {
                    buffer = output.GetSpan(mem.Length);
                    mem.Span.CopyTo(buffer);
                    output.Advance(mem.Length);
                }
            }
        }
    }
}
