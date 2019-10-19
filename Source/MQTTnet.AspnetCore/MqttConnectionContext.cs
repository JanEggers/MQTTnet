using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Protocols = Bedrock.Framework.Protocols;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionContext : Protocols.Protocol<MqttReader, MqttWriter, MqttBasePacket, MqttBasePacket>, IMqttChannelAdapter
    {
        private readonly MqttReader _reader;
        private readonly MqttWriter _writer;

        public MqttConnectionContext(
            MqttPacketFormatterAdapter packetFormatterAdapter, 
            ConnectionContext connection)
               : this(
                     packetFormatterAdapter,
                     connection, 
                     new MqttReader(packetFormatterAdapter),
                     new MqttWriter(packetFormatterAdapter))
        {
        }


        public MqttConnectionContext(
            MqttPacketFormatterAdapter packetFormatterAdapter, 
            ConnectionContext connection, 
            MqttReader reader, 
            MqttWriter writer)
            : base(connection, reader, writer, null)
        {
            PacketFormatterAdapter = packetFormatterAdapter ?? throw new ArgumentNullException(nameof(packetFormatterAdapter));

            _reader = reader;
            _writer = writer;
        }
        
        public string Endpoint
        {
            get
            {
                var endpoint = Connection.RemoteEndPoint;
                return $"{endpoint}";
            }
        }

        public bool IsSecureConnection => Http?.HttpContext?.Request?.IsHttps ?? false;

        public X509Certificate2 ClientCertificate => Http?.HttpContext?.Connection?.ClientCertificate;

        private IHttpContextFeature Http => Connection.Features.Get<IHttpContextFeature>();

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        public long BytesSent { get => _writer.BytesSent; set => _writer.BytesSent = value; }
        public long BytesReceived { get => _reader.BytesReceived; set => _reader.BytesReceived = value; }

        public Action ReadingPacketStartedCallback { get => _reader.ReadingPacketStartedCallback; set => _reader.ReadingPacketStartedCallback = value; }
        public Action ReadingPacketCompletedCallback { get => _reader.ReadingPacketCompletedCallback; set => _reader.ReadingPacketCompletedCallback = value; }
        
        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Connection is TcpConnection tcp && !tcp.IsConnected)
            {
                await tcp.StartAsync().ConfigureAwait(false);
            }
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connection.Transport?.Input?.Complete();
            Connection.Transport?.Output?.Complete();
            return Task.CompletedTask;
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var result = await ReadAsync(cancellationToken);
            if (result == null)
            {
                throw new MqttCommunicationException("Connection Aborted");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        public void ResetStatistics()
        {
            BytesReceived = 0;
            BytesSent = 0;
        }

        public Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return WriteAsync(packet, cancellationToken).AsTask();
        }

        public void Dispose()
        {
        }
    }
}
