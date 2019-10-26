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

namespace MQTTnet.AspNetCore
{
    public class MqttServerConnection
    {
        private readonly ConnectionContext _connection;
        private readonly AspNetMqttServer _server;
        private readonly MqttProtocolVersion _protocolVersion;
        private readonly Subject<MqttBasePacket> _outputBuffer;
        private readonly ProtocolReader<MqttBasePacket> _reader;
        private readonly ProtocolWriter<MqttBasePacket> _writer;

        private ImmutableList<TopicFilter> _subscriptions;

        public MqttServerConnection(ConnectionContext connection, AspNetMqttServer server, MqttProtocolVersion protocolVersion)
        {
            _connection = connection;
            _server = server;
            _protocolVersion = protocolVersion;
            _subscriptions = ImmutableList<TopicFilter>.Empty;
            _outputBuffer = new Subject<MqttBasePacket>();
            _reader = _connection.CreateMqttReader(_protocolVersion);
            _writer = _connection.CreateMqttWriter(_protocolVersion);
        }


        public async ValueTask Run()
        {
            var ct = _connection.ConnectionClosed;
            var reader = _reader;

            using (_outputBuffer
                .Do(WritePacket)
                .Subscribe())
            using (_server.Packets
                .ObserveOn(Scheduler.Default)
                .Where(PacketFilter)
                .Do(_outputBuffer.OnNext)
                .Subscribe())
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var packet = await reader.ReadAsync(ct);
                        HandlePacket(packet);
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
            _writer.WriteAsync(packet).GetAwaiter().GetResult();
        }

        private void HandlePacket(MqttBasePacket packet)
        {
            var outputBuffer = _outputBuffer;
            switch (packet)
            {
                case MqttConnectPacket connect:
                    outputBuffer.OnNext(new MqttConnAckPacket()
                    {
                        ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                    });
                    break;
                case MqttPublishPacket pub:
                    _server.Packets.OnNext(pub);
                    break;
                case MqttSubscribePacket sub:
                    _subscriptions = _subscriptions.AddRange(sub.TopicFilters);
                    outputBuffer.OnNext(new MqttSubAckPacket()
                    {
                        PacketIdentifier = sub.PacketIdentifier
                    });
                    break;
                case MqttPingReqPacket _:
                    outputBuffer.OnNext(new MqttPingRespPacket());
                    break;
                default:
                    break;
            }
        }

        private bool PacketFilter(MqttPublishPacket p)
        {
            //dont _subscriptions.Find it allocates delegate

            foreach (var f in _subscriptions)
            {
                if (MqttTopicFilterComparer.IsMatch(p.Topic, f.Topic))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
