using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;
using MQTTnet.Packets;
using System;
using Bedrock.Framework.Protocols;
using System.Collections.Immutable;
using System.Reactive.Linq;
using MQTTnet.Server;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.AspNetCore.V3;
using System.Threading;
using System.Collections.Generic;
using MQTTnet.AspNetCore.Topics;

namespace MQTTnet.AspNetCore
{
    public class MqttServerConnection
    {
        private readonly ConnectionContext _connection;
        private readonly AspNetMqttServer _server;
        private readonly MqttProtocolVersion _protocolVersion;
        private readonly Subject<MqttBasePacket> _buffer;
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
            _buffer = new Subject<MqttBasePacket>();
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

            using (_buffer
                .Do(WritePacket)
                .Subscribe())
            using (_server.Packets
                .ObserveOn(Scheduler.Default)
                .Where(PacketFilter)
                .Do(WriteFrame)
                .Subscribe())
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var frame = await reader.ReadAsync(ct);
                        switch (frame.PacketType)
                        {
                            case MqttControlPacketType.Connect:
                                _buffer.OnNext(new MqttConnAckPacket()
                                {
                                    ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                                });
                                break;
                            case MqttControlPacketType.Publish:
                                _server.Packets.OnNext(frame);
                                break;
                            case MqttControlPacketType.Subscribe:
                                var sub = _reader.DecodeSubscribePacket(frame.Body);
                                _subscriptions = _subscriptions.AddRange(sub.TopicFilters);

                                foreach (var topic in sub.TopicFilters)
                                {
                                    SubscriptionNode.Subscribe(topic, _subscriptionIndex);
                                }

                                _buffer.OnNext(new MqttSubAckPacket()
                                {
                                    PacketIdentifier = sub.PacketIdentifier
                                });
                                break;
                            case MqttControlPacketType.PingReq:
                                _buffer.OnNext(new MqttPingRespPacket());
                                break;
                            case MqttControlPacketType.Disconnect:
                                return;
                            default:
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        private void WritePacket(MqttBasePacket packet) 
        {
            _packetWriter.WriteAsync(packet).GetAwaiter().GetResult();
        }
               
        private void WriteFrame(MqttFrame frame)
        {
            _frameWriter.WriteAsync(frame).GetAwaiter().GetResult();
        }

        private void WriteFrames(IList<MqttFrame> frames)
        {
            _frameWriter.WriteManyAsync(frames).GetAwaiter().GetResult();
        }

        private bool PacketFilter(MqttFrame frame)
        {
            return SubscriptionNode.IsMatch(new MqttV3PublishPacket(frame).Topic, _subscriptionIndex);
        }
    }
}
