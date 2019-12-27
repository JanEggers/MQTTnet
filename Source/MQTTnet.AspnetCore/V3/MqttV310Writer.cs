using System;
using System.Buffers;
using System.Linq;
using Bedrock.Framework.Protocols;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.AspNetCore.V3
{
    public class MqttV310Writer : IMessageWriter<MqttBasePacket>
    {
        private MqttBufferWriter _writer = new MqttBufferWriter();
        public MqttFrameWriter FrameWriter { get; } = new MqttFrameWriter();

        public void WriteMessage(MqttBasePacket message, IBufferWriter<byte> output)
        {
            var fixedHeader = EncodePacket(message, _writer);
            var written = _writer.Written;

            var frame = new MqttFrame(fixedHeader, new ReadOnlySequence<byte>(written));
            FrameWriter.WriteMessage(frame, output);
            _writer.Reset();
        }

        private byte EncodePacket(MqttBasePacket packet, IBufferWriter<byte> packetWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return EncodeConnectPacket(connectPacket, packetWriter);
                case MqttConnAckPacket connAckPacket: return EncodeConnAckPacket(connAckPacket, packetWriter);
                case MqttDisconnectPacket _: return EncodeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return EncodeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return EncodeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return EncodePublishPacket(publishPacket, packetWriter);
                case MqttPubAckPacket pubAckPacket: return EncodePubAckPacket(pubAckPacket, packetWriter);
                case MqttPubRecPacket pubRecPacket: return EncodePubRecPacket(pubRecPacket, packetWriter);
                case MqttPubRelPacket pubRelPacket: return EncodePubRelPacket(pubRelPacket, packetWriter);
                case MqttPubCompPacket pubCompPacket: return EncodePubCompPacket(pubCompPacket, packetWriter);
                case MqttSubscribePacket subscribePacket: return EncodeSubscribePacket(subscribePacket, packetWriter);
                case MqttSubAckPacket subAckPacket: return EncodeSubAckPacket(subAckPacket, packetWriter);
                case MqttUnsubscribePacket unsubscribePacket: return EncodeUnsubscribePacket(unsubscribePacket, packetWriter);
                case MqttUnsubAckPacket unsubAckPacket: return EncodeUnsubAckPacket(unsubAckPacket, packetWriter);

                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }



        protected virtual byte EncodeConnectPacket(MqttConnectPacket packet, IBufferWriter<byte> packetWriter)
        {
            ValidateConnectPacket(packet);

            packetWriter.WriteWithLengthPrefix("MQIsdp");
            packetWriter.Write(3); // Protocol Level 3

            byte connectFlags = 0x0;
            if (packet.CleanSession)
            {
                connectFlags |= 0x2;
            }

            if (packet.WillMessage != null)
            {
                connectFlags |= 0x4;
                connectFlags |= (byte)((byte)packet.WillMessage.QualityOfServiceLevel << 3);

                if (packet.WillMessage.Retain)
                {
                    connectFlags |= 0x20;
                }
            }

            if (packet.Password != null && packet.Username == null)
            {
                throw new MqttProtocolViolationException("If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].");
            }

            if (packet.Password != null)
            {
                connectFlags |= 0x40;
            }

            if (packet.Username != null)
            {
                connectFlags |= 0x80;
            }

            packetWriter.Write(connectFlags);
            packetWriter.Write(packet.KeepAlivePeriod);
            packetWriter.WriteWithLengthPrefix(packet.ClientId);

            if (packet.WillMessage != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.WillMessage.Topic);
                packetWriter.WriteWithLengthPrefix(packet.WillMessage.Payload);
            }

            if (packet.Username != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.Username);
            }

            if (packet.Password != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.Password);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        protected virtual byte EncodeConnAckPacket(MqttConnAckPacket packet, IBufferWriter<byte> packetWriter)
        {
            packetWriter.Write(0); // Reserved.
            packetWriter.Write((byte)packet.ReturnCode.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static byte EncodePubRelPacket(MqttPubRelPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        private static byte EncodePublishPacket(MqttPublishPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }

            packetWriter.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (!packet.PacketIdentifier.HasValue)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                packetWriter.Write(packet.PacketIdentifier.Value);
            }
            else
            {
                if (packet.PacketIdentifier > 0)
                {
                    throw new MqttProtocolViolationException("Packet identifier must be empty if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

            if (packet.Payload?.Length > 0)
            {
                packetWriter.WriteByteArray(packet.Payload);
            }

            byte fixedHeader = 0;

            if (packet.Retain)
            {
                fixedHeader |= 0x01;
            }

            fixedHeader |= (byte)((byte)packet.QualityOfServiceLevel << 1);

            if (packet.Dup)
            {
                fixedHeader |= 0x08;
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Publish, fixedHeader);
        }

        private static byte EncodePubAckPacket(MqttPubAckPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte EncodePubRecPacket(MqttPubRecPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte EncodePubCompPacket(MqttPubCompPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte EncodeSubscribePacket(MqttSubscribePacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Subscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter.Topic);
                    packetWriter.Write((byte)topicFilter.QualityOfServiceLevel);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        private static byte EncodeSubAckPacket(MqttSubAckPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("SubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            if (packet.ReturnCodes?.Any() == true)
            {
                foreach (var packetSubscribeReturnCode in packet.ReturnCodes)
                {
                    packetWriter.Write((byte)packetSubscribeReturnCode);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        private static byte EncodeUnsubscribePacket(MqttUnsubscribePacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Unsubscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        private static byte EncodeUnsubAckPacket(MqttUnsubAckPacket packet, IBufferWriter<byte> packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("UnsubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        private static byte EncodeEmptyPacket(MqttControlPacketType type)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }

        protected void ValidateConnectPacket(MqttConnectPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }
        }
    }
}
