using MQTTnet.Exceptions;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public static class MqttProtocolReader
    {
        public static bool TryReadMessage(in ReadOnlySequence<byte> input, out byte header, out ReadOnlyMemory<byte> body, out SequencePosition consumed) 
        {
            header = default;
            body = default;
            consumed = input.Start;

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

            header = input.First.Span[0];
            var bodySlice = copy.Slice(0, bodyLength);
            body = GetMemory(bodySlice);
            consumed = bodySlice.End;
            return true;
        }

        private static ReadOnlyMemory<byte> GetMemory(in ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                return input.First;
            }

            // Should be rare
            return input.ToArray();
        }

        public static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int bodyLength)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            var index = 1;
            bodyLength = 0;

            var temp = GetMemory(input.Slice(0, Math.Min(5, input.Length)));

            do
            {
                if (index == temp.Length)
                {
                    return false;
                }
                encodedByte = temp.Span[index];
                index++;

                value += (byte)(encodedByte & 127) * multiplier;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", temp.Slice(1, index).ToArray())}).");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            input = input.Slice(index);
            bodyLength = value;
            return true;
        }
    }
}
