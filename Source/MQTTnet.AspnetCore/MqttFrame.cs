using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public readonly struct MqttFrame
    {
        public MqttFrame(byte header, ReadOnlySequence<byte> body)
        {
            Header = header;
            Body = body;
        }

        public byte Header { get; }
        public ReadOnlySequence<byte> Body { get; }
               
        public MqttControlPacketType PacketType 
        {
            get 
            {
                var controlPacketType = (MqttControlPacketType)(Header >> 4);
                if (controlPacketType < MqttControlPacketType.Connect || controlPacketType > MqttControlPacketType.Auth)
                {
                    throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
                }

                return controlPacketType;
            }
        }
    }
}
