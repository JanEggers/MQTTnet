using MQTTnet.Protocol;
using System;

namespace MQTTnet.AspNetCore.V3
{
    public ref struct MqttV3PublishPacket
    {
        public MqttV3PublishPacket(in MqttFrame frame)
        {
            _header = frame.Header;

            ReadOnlySpan<byte> body = frame.Body;

            Topic = body.ReadSegmentWithLengthPrefix();

            var packetIdentifier = ReadOnlySpan<byte>.Empty;
            if (Qos(frame.Header) > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = body.Read(2);
            }

            PacketIdentifier = packetIdentifier;
            Payload = body;
        }

        private byte _header;

        public ReadOnlySpan<byte> PacketIdentifier { get; }
        public bool Retain => (_header & 0x1) > 0;

        public MqttQualityOfServiceLevel QualityOfServiceLevel => Qos(_header);

        private static MqttQualityOfServiceLevel Qos(byte header)
        {
            return (MqttQualityOfServiceLevel)(header >> 1 & 0x3);
        }

        public bool Dup => (_header & 0x8) > 0;

        public ReadOnlySpan<byte> Topic { get; }

        public ReadOnlySpan<byte> Payload { get; }
    }
}
