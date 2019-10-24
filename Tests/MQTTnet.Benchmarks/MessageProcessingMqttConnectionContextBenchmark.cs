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
        private ProtocolReader<MqttBasePacket> _reader;
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

            _writer = connection.CreateMqttWriter(Formatter.MqttProtocolVersion.V311);
            _reader = connection.CreateMqttReader(Formatter.MqttProtocolVersion.V311);
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


            var connack = (MqttConnAckPacket)_reader.ReadAsync().GetAwaiter().GetResult();
            var suback = (MqttSubAckPacket)_reader.ReadAsync().GetAwaiter().GetResult();
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

            var j = 0;

            var wait = _mqttServer.Packets
                   .Take(10000)
                   .Do(x => j++)
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


            List<MqttPublishPacket> publishes = new List<MqttPublishPacket>(10000);

            for (int i = 0; i < 10000; i++)
            {
                publishes.Add((MqttPublishPacket) await _reader.ReadAsync());
            }

            var pingResponse =(MqttPingRespPacket)await _reader.ReadAsync();
        }
    }
}
