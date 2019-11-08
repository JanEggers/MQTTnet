using Bedrock.Framework.Protocols;
using MQTTnet.Exceptions;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttFrameReader : IProtocolReader<MqttFrame>
    {
        public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out MqttFrame message)
        {
            message = default;
            consumed = input.Start;
            examined = input.End;

            if (input.Length < 2)
            {
                return false;
            }

            var copy = input;
            if (!TryReadBodyLength(ref copy, out var bodyLength))
            {
                return false;
            }

            if (copy.Length < bodyLength)
            {
                return false;
            }

            var bodySlice = copy.Slice(0, bodyLength);

            var body = new byte[bodyLength];
            bodySlice.CopyTo(body);
            message = new MqttFrame(input.First.Span[0], body);
            consumed = bodySlice.End;
            examined = bodySlice.End;
            return true;
        }
        
        public static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int bodyLength)
        {
            var length = Math.Min(5, (int)input.Length);
            var temp = input.Slice(0, length);

            if (temp.IsSingleSegment)
            {
                var result = TryReadBodyLength(temp.FirstSpan, out var headerLength, out bodyLength);
                input = input.Slice(headerLength);
                return result;
            }

            {
                Span<byte> span = stackalloc byte[length];
                temp.CopyTo(span);
                var result = TryReadBodyLength(span, out var headerLength, out bodyLength);
                input = input.Slice(headerLength);
                return result;
            }
        }

        private static bool TryReadBodyLength(in ReadOnlySpan<byte> input, out int headerLength, out int bodyLength)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            headerLength = 1;
            bodyLength = 0;
            byte encodedByte;
            do
            {
                if (headerLength == input.Length)
                {
                    return false;
                }
                encodedByte = input[headerLength];
                headerLength++;

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", input.Slice(1, headerLength).ToArray())}).");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            bodyLength = value;
            return true;
        }
    }
}
