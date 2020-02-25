using MQTTnet.Protocol;
using System;
using System.Buffers;

namespace MQTTnet.AspNetCore.V3
{
    public ref struct MqttV3PublishPacket
    {
        public MqttV3PublishPacket(in MqttFrame frame)
        {
            _header = frame.Header;
            
            var body = frame.Body;
            
            Topic = body.ReadSegmentWithLengthPrefix();

            var packetIdentifier = ReadOnlySequence<byte>.Empty;
            if (Qos(frame.Header) > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = body.Read(2);
            }

            PacketIdentifier = packetIdentifier;
            Payload = body;
        }

        private byte _header;

        public ReadOnlySequence<byte> PacketIdentifier { get; }
        public bool Retain => (_header & 0x1) > 0;

        public MqttQualityOfServiceLevel QualityOfServiceLevel => Qos(_header);

        public static MqttQualityOfServiceLevel Qos(byte header)
        {
            return (MqttQualityOfServiceLevel)(header >> 1 & 0x3);
        }

        public static ushort ReadPacketIdentifier(in MqttFrame frame)
        {
            ReadOnlySpan<byte> span = new MqttV3PublishPacket(frame).PacketIdentifier.ToSpan();
            return span.ReadTwoByteInteger();
        }

        public bool Dup => (_header & 0x8) > 0;

        public ReadOnlySequence<byte> Topic { get; }

        public ReadOnlySequence<byte> Payload { get; }
    }
}
