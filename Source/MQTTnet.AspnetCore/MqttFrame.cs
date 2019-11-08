using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.AspNetCore
{
    public ref struct MqttFrame
    {
        public MqttFrame(byte header, byte[] body)
        {
            Header = header;
            Body = body;
        }

        public byte Header { get; }

        public byte[] Body { get; }

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
