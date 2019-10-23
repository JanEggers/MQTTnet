using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;

namespace MQTTnet.AspNetCore.V3
{
    public class MqttV311Reader : MqttV310Reader
    {
        protected override MqttBasePacket DecodeConnAckPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = body.ReadByte();

            packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            packet.ReturnCode = (MqttConnectReturnCode)body.ReadByte();

            return packet;
        }
    }
}
