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
            while (true)
            {
                var result = await ReadFrame(ct).ConfigureAwait(false);
                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
                try
                {
                    switch (result.Message.PacketType)
                    {
                        case MqttControlPacketType.Connect:
                            await WritePacket(new MqttConnAckPacket()
                            {
                                ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                            }, ct).ConfigureAwait(false);
                            break;
                        case MqttControlPacketType.Publish:
                            foreach (var connection in _server.Connections)
                            {
                                if (!connection.PacketFilter(result.Message))
                                {
                                    continue;
                                }
                                await connection.WriteFrame(result.Message, ct).ConfigureAwait(false);
                            }
                            break;
                        case MqttControlPacketType.Subscribe:
                            var sub = _mqttMessageReader.DecodeSubscribePacket(result.Message.Body.ToSpan());
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
                            return;
                        default:
                            break;
                    }
                }
                finally
                {
                    Advance();
                }
            }
        }

        private bool PacketFilter(MqttFrame frame)
        {
            return SubscriptionNode.IsMatch(new MqttV3PublishPacket(frame).Topic, _subscriptionIndex);
        }
    }
}
