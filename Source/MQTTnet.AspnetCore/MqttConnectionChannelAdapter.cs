using Bedrock.Framework;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionChannelAdapter : IMqttChannelAdapter
    {
        private MqttConnectionContext _mqttConnection;
        private readonly IMqttClientOptions _options;

        private MqttConnectionChannelAdapter(MqttConnectionContext mqttConnection)
        {
            _mqttConnection = mqttConnection;
        }

        private MqttConnectionChannelAdapter(IMqttClientOptions options)
        {
            _options = options;
        }

        public static MqttConnectionChannelAdapter ForConnection(MqttConnectionContext mqttConnection) 
        {
            return new MqttConnectionChannelAdapter(mqttConnection);
        }

        public static MqttConnectionChannelAdapter ForClient(IMqttClientOptions options)
        {
            return new MqttConnectionChannelAdapter(options);
        }
        
        public bool IsSecureConnection => Http?.HttpContext?.Request?.IsHttps ?? false;

        public X509Certificate2 ClientCertificate => Http?.HttpContext?.Connection?.ClientCertificate;

        private IHttpContextFeature Http => _mqttConnection?.Connection.Features.Get<IHttpContextFeature>();



        public string Endpoint
        {
            get
            {
                var endpoint = _mqttConnection?.Connection.RemoteEndPoint;
                return $"{endpoint}";
            }
        }

        public MqttPacketFormatterAdapter PacketFormatterAdapter => _mqttConnection?.PacketFormatterAdapter;

        public long BytesSent { get => _mqttConnection?.BytesSent ?? 0; set => _mqttConnection.BytesSent = value; }
        public long BytesReceived { get => _mqttConnection?.BytesReceived ?? 0; set => _mqttConnection.BytesReceived = value; }

        public Action ReadingPacketStartedCallback { get => _mqttConnection?.ReadingPacketStartedCallback; set => _mqttConnection.ReadingPacketStartedCallback = value; }
        public Action ReadingPacketCompletedCallback { get => _mqttConnection?.ReadingPacketCompletedCallback; set => _mqttConnection.ReadingPacketCompletedCallback = value; }

        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_options != null)
            {
                var services = new ServiceCollection()
                    .AddConnections()
                    .BuildServiceProvider();

                var clientBuilder = new ClientBuilder(services);
                

                switch (_options.ChannelOptions)
                {
                    case MqttClientTcpOptions tcpOptions:
                        {
                            var endpoint = new DnsEndPoint(tcpOptions.Server, tcpOptions.GetPort());

                            clientBuilder
                                .UseSockets();

                            var client = clientBuilder.Build();
                            var connection = await client.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

                            _mqttConnection = MqttConnectionContext.Create(connection, _options.ProtocolVersion);
                            break;
                        }
                    case MqttClientWebSocketOptions webSocketOptions:

                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            }
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_options != null)
            {
                _mqttConnection?.Connection.Transport?.Input?.Complete();
                _mqttConnection?.Connection.Transport?.Output?.Complete();
            }
            return Task.CompletedTask;
        }
               
        public void ResetStatistics()
        {
            BytesReceived = 0;
            BytesSent = 0;
        }

        public void Dispose()
        {
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var result = await _mqttConnection.ReadAsync(cancellationToken);
            if (result == null)
            {
                throw new MqttCommunicationException("Connection Aborted");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
               
        public Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _mqttConnection.WriteAsync(packet, cancellationToken).AsTask();
        }
    }
}
