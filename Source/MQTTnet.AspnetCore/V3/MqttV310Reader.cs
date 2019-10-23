using System;
using System.Buffers;
using Bedrock.Framework.Protocols;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.AspNetCore.V3
{
    public class MqttV310Reader : IProtocolReader<MqttBasePacket>
    {
        private static readonly MqttPingReqPacket PingReqPacket = new MqttPingReqPacket();
        private static readonly MqttPingRespPacket PingRespPacket = new MqttPingRespPacket();
        private static readonly MqttDisconnectPacket DisconnectPacket = new MqttDisconnectPacket();
          

        public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out MqttBasePacket message)
        {
            message = null;
            examined = input.End;
            if (!MqttProtocolReader.TryReadMessage(input, out var fixedheader, out var body, out consumed))
            {
                return false;
            }
            message = Decode(fixedheader, body.Span);
            examined = consumed;
            return true;
        }

        private MqttBasePacket Decode(byte fixedheader, ReadOnlySpan<byte> body)
        {
            var controlPacketType = fixedheader >> 4;
            if (controlPacketType < 1 || controlPacketType > 14)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect: return DecodeConnectPacket(body);
                case MqttControlPacketType.ConnAck: return DecodeConnAckPacket(body);
                case MqttControlPacketType.Disconnect: return DisconnectPacket;
                case MqttControlPacketType.Publish: return DecodePublishPacket(fixedheader, body);
                case MqttControlPacketType.PubAck: return DecodePubAckPacket(body);
                case MqttControlPacketType.PubRec: return DecodePubRecPacket(body);
                case MqttControlPacketType.PubRel: return DecodePubRelPacket(body);
                case MqttControlPacketType.PubComp: return DecodePubCompPacket(body);
                case MqttControlPacketType.PingReq: return PingReqPacket;
                case MqttControlPacketType.PingResp: return PingRespPacket;
                case MqttControlPacketType.Subscribe: return DecodeSubscribePacket(body);
                case MqttControlPacketType.SubAck: return DecodeSubAckPacket(body);
                case MqttControlPacketType.Unsubscibe: return DecodeUnsubscribePacket(body);
                case MqttControlPacketType.UnsubAck: return DecodeUnsubAckPacket(body);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        private static MqttBasePacket DecodeUnsubAckPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DecodePubCompPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DecodePubRelPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DecodePubRecPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DecodePubAckPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DecodeUnsubscribePacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger(),
            };

            while (body.Length > 0)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DecodeSubscribePacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            while (body.Length > 0)
            {
                var topicFilter = new TopicFilter
                {
                    Topic = body.ReadWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)body.ReadByte()
                };

                packet.TopicFilters.Add(topicFilter);
            }

            return packet;
        }

        private static MqttBasePacket DecodePublishPacket(byte fixedHeader, ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var retain = (fixedHeader & 0x1) > 0;
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)(fixedHeader >> 1 & 0x3);
            var dup = (fixedHeader & 0x8) > 0;

            var topic = body.ReadWithLengthPrefix();

            ushort? packetIdentifier = null;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = body.ReadTwoByteInteger();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            if (body.Length > 0)
            {
                packet.Payload = body.ReadRemainingData();
            }

            return packet;
        }

        private MqttBasePacket DecodeConnectPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var protocolName = body.ReadStringWithLengthPrefix();
            var protocolVersion = body.ReadByte();

            if (protocolName != "MQTT" && protocolName != "MQIsdp")
            {
                throw new MqttProtocolViolationException("MQTT protocol name do not match MQTT v3.");
            }

            if (protocolVersion != 3 && protocolVersion != 4)
            {
                throw new MqttProtocolViolationException("MQTT protocol version do not match MQTT v3.");
            }

            var packet = new MqttConnectPacket();

            var connectFlags = body.ReadByte();
            if ((connectFlags & 0x1) > 0)
            {
                throw new MqttProtocolViolationException("The first bit of the Connect Flags must be set to 0.");
            }

            packet.CleanSession = (connectFlags & 0x2) > 0;

            var willFlag = (connectFlags & 0x4) > 0;
            var willQoS = (connectFlags & 0x18) >> 3;
            var willRetain = (connectFlags & 0x20) > 0;
            var passwordFlag = (connectFlags & 0x40) > 0;
            var usernameFlag = (connectFlags & 0x80) > 0;

            packet.KeepAlivePeriod = body.ReadTwoByteInteger();
            packet.ClientId = body.ReadStringWithLengthPrefix();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage
                {
                    Topic = body.ReadStringWithLengthPrefix(),
                    Payload = body.ReadWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)willQoS,
                    Retain = willRetain
                };
            }

            if (usernameFlag)
            {
                packet.Username = body.ReadStringWithLengthPrefix();
            }

            if (passwordFlag)
            {
                packet.Password = body.ReadWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private static MqttBasePacket DecodeSubAckPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            while (body.Length > 0)
            {
                packet.ReturnCodes.Add((MqttSubscribeReturnCode)body.ReadByte());
            }

            return packet;
        }

        protected virtual MqttBasePacket DecodeConnAckPacket(ReadOnlySpan<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            body.ReadByte(); // Reserved.
            packet.ReturnCode = (MqttConnectReturnCode)body.ReadByte();

            return packet;
        }

        protected void ValidateConnectPacket(MqttConnectPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        protected static void ThrowIfBodyIsEmpty(ReadOnlySpan<byte> body)
        {
            if (body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
