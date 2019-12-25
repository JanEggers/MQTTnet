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
        private readonly SemaphoreSlim _semaphore;

        public ProtocolWriter<MqttBasePacket> MqttWriter { get; }
        public ProtocolWriter<MqttFrame> FrameWriter { get; }
        public ProtocolReader<MqttFrame> FrameReader { get; }
        
        public MqttClientConnection(ConnectionContext connection, IOptions<Options> options)
        {
            _connection = connection;
            _options = options;
            _semaphore = new SemaphoreSlim(1);

            MqttWriter = connection.CreateMqttPacketWriter(options.Value.MqttProtocolVersion, _semaphore);
            FrameWriter = connection.CreateMqttFrameWriter(_semaphore);
            FrameReader = connection.CreateMqttFrameReader();
        }

        public async ValueTask<ReadResult<MqttFrame>> SendConnectAsync(MqttConnectPacket connectPacket, CancellationToken cancellationToken = default)
        {
            await MqttWriter.WriteAsync(connectPacket, cancellationToken);
            return await FrameReader.ReadAsync(cancellationToken);
        }

        public async ValueTask SubscribeAsync(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken = default)
        {
            await MqttWriter.WriteAsync(subscribePacket, cancellationToken);
        }

        public virtual async ValueTask RunConnectionAsync()
        {
            await SendConnectAsync(_options.Value.ConnectPacket);
        }

        public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default) 
        {
            await MqttWriter.WriteAsync(new MqttDisconnectPacket(), cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}
