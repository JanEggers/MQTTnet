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
    public class MqttServerConnection
    {
        private readonly ConnectionContext _connection;
        private readonly AspNetMqttServer _server;
        private readonly MqttProtocolVersion _protocolVersion;
        private readonly ProtocolReader<MqttFrame> _frameReader;
        private readonly ProtocolWriter<MqttBasePacket> _packetWriter;
        private readonly ProtocolWriter<MqttFrame> _frameWriter;
        private readonly MqttV310Reader _reader;

        private ImmutableList<TopicFilter> _subscriptions;
        private SubscriptionNode.Index _subscriptionIndex = new SubscriptionNode.Index();

        public MqttServerConnection(ConnectionContext connection, AspNetMqttServer server, MqttProtocolVersion protocolVersion)
        {
            _connection = connection;
            _server = server;
            _protocolVersion = protocolVersion;
            _subscriptions = ImmutableList<TopicFilter>.Empty;
            _frameReader = _connection.CreateMqttFrameReader();
            _reader = _protocolVersion.CreateReader();

            var semaphore = new SemaphoreSlim(1);

            _packetWriter = _connection.CreateMqttPacketWriter(_protocolVersion, semaphore);
            _frameWriter = _connection.CreateMqttFrameWriter(semaphore);
        }


        public async ValueTask Run()
        {
            var ct = _connection.ConnectionClosed;
            var reader = _frameReader;
            var packetWriter = _packetWriter;

            while (!ct.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(ct);
                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
                try
                {
                    switch (result.Message.PacketType)
                    {
                        case MqttControlPacketType.Connect:
                            await packetWriter.WriteAsync(new MqttConnAckPacket()
                            {
                                ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                            });
                            break;
                        case MqttControlPacketType.Publish:
                            foreach (var connection in _server.Connections)
                            {
                                if (!connection.PacketFilter(result.Message))
                                {
                                    continue;
                                }
                                await connection._frameWriter.WriteAsync(result.Message);
                            }
                            break;
                        case MqttControlPacketType.Subscribe:
                            var sub = _reader.DecodeSubscribePacket(result.Message.Body.ToSpan());
                            _subscriptions = _subscriptions.AddRange(sub.TopicFilters);

                            foreach (var topic in sub.TopicFilters)
                            {
                                SubscriptionNode.Subscribe(topic, _subscriptionIndex);
                            }

                            await packetWriter.WriteAsync(new MqttSubAckPacket()
                            {
                                PacketIdentifier = sub.PacketIdentifier
                            });
                            break;
                        case MqttControlPacketType.PingReq:
                            await packetWriter.WriteAsync(new MqttPingRespPacket());
                            break;
                        case MqttControlPacketType.Disconnect:
                            return;
                        default:
                            break;
                    }
                }
                finally
                {
                    reader.Advance();
                }
            }
        }

        private bool PacketFilter(MqttFrame frame)
        {
            return SubscriptionNode.IsMatch(new MqttV3PublishPacket(frame).Topic, _subscriptionIndex);
        }
    }
}
