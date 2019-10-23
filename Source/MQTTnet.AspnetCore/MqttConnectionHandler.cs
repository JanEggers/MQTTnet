using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using System.Threading.Tasks;
using MQTTnet.Packets;
using System;
using Bedrock.Framework.Protocols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Threading;
using MQTTnet.Server;
using System.Reactive.Concurrency;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionHandler : ConnectionHandler
    {
        public MqttConnectionHandler(AspNetMqttServer server)
        {
            Server = server;
        }

        public AspNetMqttServer Server { get; }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            // required for websocket transport to work
            var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
            if (transferFormatFeature != null)
            {
                transferFormatFeature.ActiveFormat = TransferFormat.Binary;
            }

            var ct = connection.ConnectionClosed;
            var protocolVersion = await connection.CreateMqttVersionReader().ReadAsync(ct);

            var reader = connection.CreateMqttReader(protocolVersion);
            var writer = connection.CreateMqttWriter(protocolVersion);

            var subscriptions = ImmutableList<TopicFilter>.Empty;

            using (Server.Packets
                .ObserveOn(Scheduler.Default)
                .Where(p => subscriptions.Find(f => MqttTopicFilterComparer.IsMatch(p.Topic, f.Topic)) != null)
                .Do(p => writer.WriteAsync(p))
                .Subscribe())
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var packet = await reader.ReadAsync(ct);

                        switch (packet)
                        {
                            case MqttConnectPacket connect:
                                await writer.WriteAsync(new MqttConnAckPacket()
                                {
                                    ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted
                                });
                                break;
                            case MqttPublishPacket pub:
                                Server.Packets.OnNext(pub);
                                break;
                            case MqttSubscribePacket sub:
                                subscriptions = subscriptions.AddRange(sub.TopicFilters);
                                break;
                            case MqttPingReqPacket ping:
                                await writer.WriteAsync(new MqttPingRespPacket());
                                break;
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
    }
}
