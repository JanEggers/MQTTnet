using BenchmarkDotNet.Attributes;

using MQTTnet.AspNetCore;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Bedrock.Framework;
using System.Net;
using MQTTnet.Packets;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Bedrock.Framework.Protocols;
using System.Threading;
using System;
using System.Collections.Generic;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class MessageProcessingMqttConnectionContextBenchmark
    {
        private IWebHost _host;
        private ProtocolWriter<MqttBasePacket> _writer;
        private ProtocolReader<MqttFrame> _reader;
        private AspNetMqttServer _mqttServer;
        private MqttPublishPacket _message;

        [GlobalSetup]
        public void Setup()
        {
            _host = WebHost.CreateDefaultBuilder()
                   .UseKestrel(o => o.ListenAnyIP(1883, l => l.UseMqtt()))
                   .ConfigureServices(services => {
                       services
                           .AddMqttServer();
                   })
                   .ConfigureLogging(logging => {
                       foreach (var item in logging.Services.Where(s => s.ServiceType == typeof(ILoggerProvider)).ToList())
                       {
                           logging.Services.Remove(item);
                       }                              
                   })
                   .Configure(app => {})
                   .Build();


            var client = new ClientBuilder(_host.Services)
                .UseSockets()
                .Build();

            _mqttServer = _host.Services.GetRequiredService<AspNetMqttServer>();

            _host.Start();

            var endpoint = new DnsEndPoint("localhost", 1883);
            var connection = client.ConnectAsync(endpoint).GetAwaiter().GetResult();

            _writer = connection.CreateMqttPacketWriter(Formatter.MqttProtocolVersion.V311);
            _reader = connection.CreateMqttFrameReader();
            _writer.WriteAsync(new MqttConnectPacket() { 
                ClientId = "client"                
            }).GetAwaiter().GetResult();
            _writer.WriteAsync(new MqttSubscribePacket()
            {
                PacketIdentifier = 1,
                TopicFilters = new System.Collections.Generic.List<TopicFilter>()
                {
                    new TopicFilter() { QualityOfServiceLevel = Protocol.MqttQualityOfServiceLevel.AtMostOnce, Topic = Encoding.UTF8.GetBytes("A") }
                }
            }).GetAwaiter().GetResult();

            _message = new MqttPublishPacket() 
            {
                Topic = Encoding.UTF8.GetBytes("A"),
            };


            var connack = _reader.ReadAsync().GetAwaiter().GetResult(); // MqttConnAckPacket
            var suback = _reader.ReadAsync().GetAwaiter().GetResult(); // MqttSubAckPacket
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _writer.Connection.DisposeAsync().GetAwaiter().GetResult();

            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        [Benchmark]
        public async ValueTask Send_10000_Messages()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var wait = _mqttServer.Packets
                   .Take(10000)
                   .ToTask(cts.Token);


            for (var i = 0; i < 10000; i++)
            {
                await _writer.WriteAsync(_message);
            }
            await _writer.WriteAsync(new MqttPingReqPacket());


            try
            {
                await wait;
            }
            catch (Exception)
            {

                throw;
            }

            var count = 10000 // publish
                      + 1 // ping
                      ;

            for (int i = 0; i < count; i++)
            {
                await _reader.ReadAsync();
            }
        }
    }
}
