using Bedrock.Framework.Protocols;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttProtocolVersionReader : IMessageReader<MqttProtocolVersion>
    {
        private readonly MqttFrameReader _frameReader = new MqttFrameReader();

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out MqttProtocolVersion version)
        {
            version = MqttProtocolVersion.V310;
            var consumedCopy = consumed;
            var examinedCopy = examined;
            if (!_frameReader.TryParseMessage(input, ref consumedCopy, ref examinedCopy, out var frame))
            {
                return false;
            }

            ReadOnlySpan<byte> buffer = frame.Body.ToSpan();
            var protocolName = buffer.ReadStringWithLengthPrefix();
            var protocolLevel = buffer.ReadByte();

            switch ((protocolName, protocolLevel))
            {
                case ("MQTT", 5):
                    version = MqttProtocolVersion.V500;
                    break;
                case ("MQTT", 4):
                    version = MqttProtocolVersion.V311;
                    break;
                case ("MQIsdp", 3):
                    version = MqttProtocolVersion.V310;
                    break;
                default:
                    throw new MqttProtocolViolationException($"Protocol '{protocolName}' level '{protocolLevel}' is not supported.");
            }

            return true;
        }
    }
}
