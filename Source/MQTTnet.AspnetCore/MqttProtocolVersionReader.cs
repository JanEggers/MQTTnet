using Bedrock.Framework.Protocols;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttProtocolVersionReader : IProtocolReader<MqttProtocolVersion>
    {
        public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out MqttProtocolVersion version)
        {
            consumed = input.Start;
            examined = input.Start;
            version = MqttProtocolVersion.V310;
            if (!MqttProtocolReader.TryReadMessage(input, out var fixedheader, out var body, out _))
            {
                return false;
            }

            var reader = new SpanBasedMqttPacketBodyReader();
            reader.SetBuffer(body);
            var protocolName = reader.ReadStringWithLengthPrefix();
            var protocolLevel = reader.ReadByte();

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
