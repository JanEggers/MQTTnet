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
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace MQTTnet.Benchmarks
{
    //[ConcurrencyVisualizerProfiler]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    public class MessageProcessingMqttConnectionContextBenchmark
    {
        private IWebHost _host;
        private AspNetMqttServer _mqttServer;
        private MqttPublishPacket _message;
        private MqttClientConnection _connection;

        private async ValueTask InitConnection(IServiceProvider serviceProvider)
        {
            var factory = new MqttConnectionFactory(serviceProvider);

            _connection = await factory.ConnectAsync<MqttClientConnection>(new DnsEndPoint("localhost", 1883));
            var connack = await _connection.SendConnectAsync(new MqttConnectPacket() { ClientId = "client" });
            await _connection.SubscribeAsync(new MqttSubscribePacket()
            {
                PacketIdentifier = 1,
                TopicFilters = new System.Collections.Generic.List<TopicFilter>()
                {
                    new TopicFilter() { QualityOfServiceLevel = Protocol.MqttQualityOfServiceLevel.AtMostOnce, Topic = Encoding.UTF8.GetBytes("A") }
                }
            });
        }

        [GlobalSetup]
        public void Setup()
        {
            _host = WebHost.CreateDefaultBuilder()
                   .UseKestrel(o => o.ListenAnyIP(1883, l => l.UseMqtt()))
                   .ConfigureServices(services => {
                       services
                           .AddMqttServer()
                           .AddMqttClient<MqttClientConnection>();
                   })
                   .ConfigureLogging(logging => {
                       foreach (var item in logging.Services.Where(s => s.ServiceType == typeof(ILoggerProvider)).ToList())
                       {
                           logging.Services.Remove(item);
                       }                              
                   })
                   .Configure(app => {})
                   .Build();


            _mqttServer = _host.Services.GetRequiredService<AspNetMqttServer>();

            _host.Start();

            InitConnection(_host.Services).GetAwaiter().GetResult();

            _message = new MqttPublishPacket() 
            {
                Topic = Encoding.UTF8.GetBytes("A"),
            };
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _connection.DisconnectAsync().GetAwaiter().GetResult();

            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        [Benchmark]
        public void Send_10000_Messages()
        {
            Task.WhenAll(Write(), Read()).GetAwaiter().GetResult();
        }

        private async Task Read()
        {
            await Task.Yield();

            var count = 10000 // publish
                      + 1 // ping
                      ;

            var reader = _connection.FrameReader;

            for (int i = 0; i < count; i++)
            {
                await reader.ReadAsync();
            }
        }

        private async Task Write()
        {
            await Task.Yield();

            var msgs = new MqttPublishPacket[10000];

            for (var i = 0; i < 10000; i++)
            {
                msgs[i] = _message;
            }
            await _connection.MqttWriter.WriteManyAsync(msgs);
            await _connection.MqttWriter.WriteAsync(new MqttPingReqPacket());
        }
    }
}
