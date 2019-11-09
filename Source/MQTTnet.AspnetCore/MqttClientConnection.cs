using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttClientConnection 
    {
        public class Options
        {
            public MqttProtocolVersion MqttProtocolVersion { get; set; } = MqttProtocolVersion.V311;
            public MqttConnectPacket ConnectPacket { get; set; } = new MqttConnectPacket() { ClientId = "client" };
        }

        private readonly ConnectionContext _connection;
        private readonly IOptions<Options> _options;
        public ProtocolWriter<MqttBasePacket> Writer { get; }
        public ProtocolReader<MqttFrame> Reader { get; }
        
        public MqttClientConnection(ConnectionContext connection, IOptions<Options> options)
        {
            _connection = connection;
            _options = options;
            Writer = connection.CreateMqttPacketWriter(options.Value.MqttProtocolVersion);
            Reader = connection.CreateMqttFrameReader();
        }

        public async ValueTask<MqttFrame> SendConnectAsync(MqttConnectPacket connectPacket, CancellationToken cancellationToken = default)
        {
            await Writer.WriteAsync(connectPacket, cancellationToken);
            return await Reader.ReadAsync(cancellationToken);
        }

        public async ValueTask SubscribeAsync(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken = default)
        {
            await Writer.WriteAsync(subscribePacket, cancellationToken);
        }

        public virtual async ValueTask RunConnectionAsync()
        {
            await SendConnectAsync(_options.Value.ConnectPacket);
        }

        public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default) 
        {
            await Writer.WriteAsync(new MqttDisconnectPacket(), cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}
