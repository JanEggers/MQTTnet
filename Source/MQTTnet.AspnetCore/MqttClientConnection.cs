using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttClientConnection : MqttConnection
    {
        private readonly IOptions<Options> _options;

        public class Options
        {
            public MqttProtocolVersion MqttProtocolVersion { get; set; } = MqttProtocolVersion.V311;
            public MqttConnectPacket ConnectPacket { get; set; } = new MqttConnectPacket() { ClientId = "client" };
        }
        
        public MqttClientConnection(ConnectionContext connection, IOptions<Options> options)
            : base(connection, options.Value.MqttProtocolVersion)
        {
            _options = options;
        }

        public async ValueTask<ProtocolReadResult<MqttFrame>> SendConnectAsync(MqttConnectPacket connectPacket, CancellationToken cancellationToken = default)
        {
            await WritePacket(connectPacket, cancellationToken).ConfigureAwait(false);
            var result = await ReadFrame(cancellationToken).ConfigureAwait(false);
            Advance();
            return result;
        }

        public async ValueTask SubscribeAsync(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken = default)
        {
            await WritePacket(subscribePacket, cancellationToken).ConfigureAwait(false);
        }

        public virtual async ValueTask RunConnectionAsync()
        {
            await SendConnectAsync(_options.Value.ConnectPacket);
        }

        public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default) 
        {
            await WritePacket(new MqttDisconnectPacket(), cancellationToken).ConfigureAwait(false);
            await Connection.DisposeAsync();
        }
    }
}
