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

            var protocolVersion = await connection.CreateMqttVersionReader().ReadAsync(connection.ConnectionClosed);

            var mqttConnection = new MqttServerConnection(connection, Server, protocolVersion);

            await mqttConnection.Run();
        }
    }
}
