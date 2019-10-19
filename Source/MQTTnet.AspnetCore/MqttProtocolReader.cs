using Bedrock.Framework.Protocols;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttProtocolReader : IProtocolReader<MqttBasePacket>
    {
        private readonly MqttPacketFormatterAdapter _packetFormatterAdapter;
        private readonly SpanBasedMqttPacketBodyReader _reader = new SpanBasedMqttPacketBodyReader();

        public MqttProtocolReader(MqttPacketFormatterAdapter packetFormatterAdapter)
        {
            _packetFormatterAdapter = packetFormatterAdapter;
        }
        
        public long BytesReceived { get; set; }

        public Action ReadingPacketStartedCallback { get; set; }
        public Action ReadingPacketCompletedCallback { get; set; }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out MqttBasePacket message)
        {
            message = null;
            consumed = input.Start;
            examined = input.End;
            var copy = input;

            if (copy.Length < 2)
            {
                // we did receive something but the message is not yet complete
                ReadingPacketStartedCallback?.Invoke();
                return false;
            }

            var fixedheader = copy.First.Span[0];
            if (!TryReadBodyLength(ref copy, out int headerLength, out var bodyLength))
            {
                // we did receive something but the message is not yet complete
                ReadingPacketStartedCallback?.Invoke();
                return false;
            }

            if (copy.Length < bodyLength)
            {
                // we did receive something but the message is not yet complete
                ReadingPacketStartedCallback?.Invoke();
                return false;
            }

            var bodySlice = copy.Slice(0, bodyLength);
            var buffer = GetMemory(bodySlice);
            _reader.SetBuffer(buffer);

            var receivedMqttPacket = new ReceivedMqttPacket(fixedheader, _reader, buffer.Length + 2);

            if (_packetFormatterAdapter.ProtocolVersion == MqttProtocolVersion.Unknown)
            {
                _packetFormatterAdapter.DetectProtocolVersion(receivedMqttPacket);
            }

            message = _packetFormatterAdapter.Decode(receivedMqttPacket);
            consumed = bodySlice.End;
            examined = bodySlice.End;
            BytesReceived += headerLength + bodySlice.Length;
            ReadingPacketCompletedCallback?.Invoke();
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

        private static bool TryReadBodyLength(ref ReadOnlySequence<byte> input, out int headerLength, out int bodyLength)
        {
            // Alorithm taken from https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            var index = 1;
            headerLength = 0;
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

            headerLength = index;
            bodyLength = value;
            return true;
        }
    }
}
