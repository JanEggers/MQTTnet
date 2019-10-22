using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using System.Threading.Tasks;
using MQTTnet.Packets;
using System;
using Bedrock.Framework.Protocols;

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
            var protocolVersion = await connection.ReadMqttVersion().ReadAsync(ct);

            var reader = connection.ReadMqtt(protocolVersion);
            var writer = connection.WriteMqtt(protocolVersion);

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
