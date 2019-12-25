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

            var versionReader = connection.CreateMqttVersionReader();

            var readResult = await versionReader.ReadAsync(connection.ConnectionClosed);
            if (readResult.IsCanceled || readResult.IsCompleted)
            {
                return;
            }

            versionReader.Advance();

            var mqttConnection = new MqttServerConnection(connection, Server, readResult.Message);

            await mqttConnection.Run();
        }
    }
}
