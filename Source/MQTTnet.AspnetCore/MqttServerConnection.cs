using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;
using MQTTnet.Packets;
using Bedrock.Framework.Protocols;
using System.Collections.Immutable;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.AspNetCore.V3;
using System.Threading;
using MQTTnet.AspNetCore.Topics;

namespace MQTTnet.AspNetCore
{
    public class MqttServerConnection : MqttConnection
    {
        private readonly AspNetMqttServer _server;
        private ImmutableList<TopicFilter> _subscriptions;
        private SubscriptionNode.Index _subscriptionIndex = new SubscriptionNode.Index();

        public MqttServerConnection(ConnectionContext connection, ProtocolReader reader, MqttProtocolVersion protocolVersion, AspNetMqttServer server)
            : base(connection, reader, protocolVersion)
        {
            _server = server;
            _subscriptions = ImmutableList<TopicFilter>.Empty;
        }
        
        public async ValueTask Run(CancellationToken ct)
        {
            var running = true;
            while (running)
            {
                var result = await ReadFrame(ct).ConfigureAwait(false);
                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
                try
                {
                    running = await HandleMqttFrame(result.Message, ct).ConfigureAwait(false);
                }
                finally
                {
                    Advance();
                }
            }
        }

        private bool PacketFilter(MqttFrame frame)
        {
            var packet = new MqttV3PublishPacket(frame);
            return SubscriptionNode.IsMatch(packet.Topic, _subscriptionIndex);
        }

        private async ValueTask<bool> HandleMqttFrame(MqttFrame frame, CancellationToken ct) 
        {
            switch (frame.PacketType)
            {
                case MqttControlPacketType.Connect:
                    await WritePacket(new MqttConnAckPacket()
                    {
                        ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                    }, ct).ConfigureAwait(false);
                    break;
                case MqttControlPacketType.Publish:
                    switch (MqttV3PublishPacket.Qos(frame.Header))
                    {
                        case MqttQualityOfServiceLevel.AtLeastOnce:
                            await WritePacket(new MqttPubAckPacket()
                            {
                                PacketIdentifier = MqttV3PublishPacket.ReadPacketIdentifier(frame)
                            }, ct).ConfigureAwait(false);
                            break;
                        default:
                            break;
                    }

                    foreach (var connection in _server.Connections)
                    {
                        if (!connection.PacketFilter(frame))
                        {
                            continue;
                        }
                        await connection.WriteFrame(frame, ct).ConfigureAwait(false);
                    }
                    break;
                case MqttControlPacketType.Subscribe:
                    var sub = _mqttMessageReader.DecodeSubscribePacket(frame.Body.ToSpan());
                    _subscriptions = _subscriptions.AddRange(sub.TopicFilters);

                    foreach (var topic in sub.TopicFilters)
                    {
                        SubscriptionNode.Subscribe(topic, _subscriptionIndex);
                    }

                    await WritePacket(new MqttSubAckPacket()
                    {
                        PacketIdentifier = sub.PacketIdentifier
                    }, ct).ConfigureAwait(false);
                    break;
                case MqttControlPacketType.PingReq:
                    await WritePacket(new MqttPingRespPacket(), ct).ConfigureAwait(false);
                    break;
                case MqttControlPacketType.Disconnect:
                    return false;
                default:
                    break;
            }
            return true;
        }
    }
}
