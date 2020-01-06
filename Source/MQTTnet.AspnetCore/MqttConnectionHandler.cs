using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using System.Threading.Tasks;

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

            var reader = connection.CreateReader();

            var versionResult = await reader.ReadAsync(new MqttProtocolVersionReader(), connection.ConnectionClosed).ConfigureAwait(false);
            if (versionResult.IsCanceled || versionResult.IsCompleted)
            {
                return;
            }

            reader.Advance();

            var mqttConnection = new MqttServerConnection(connection, reader, versionResult.Message, Server);

            try
            {
                Server.Connections = Server.Connections.Add(mqttConnection);

                await mqttConnection.Run(connection.ConnectionClosed).ConfigureAwait(false);
            }
            finally
            {
                Server.Connections = Server.Connections.Remove(mqttConnection);
            }
        }
    }
}
