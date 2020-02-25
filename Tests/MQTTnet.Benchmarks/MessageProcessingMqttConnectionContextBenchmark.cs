﻿using BenchmarkDotNet.Attributes;

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

        private const int iterations = 10000;
        private static byte[] topic = Encoding.UTF8.GetBytes("A");
        private static byte[] payload = new byte[1_000];

        private async Task Read()
        {
            await Task.Yield();

            var count = iterations // publish
                      + iterations // ack
                      + 1 // ping
                      ;
                      
            for (int i = 0; i < count; i++)
            {
                var readresult = await _connection.ReadFrame().ConfigureAwait(false);

                switch (readresult.Message.PacketType)
                {
                    case Protocol.MqttControlPacketType.PubAck:
                        _connection.Acknowledge(readresult.Message);
                        break;
                    default:
                        break;
                }

                _connection.Advance();
                readcount++;
            }
        }

        private int readcount = 0;
        private int writecount = 0;
        private List<MqttPubAckPacket> acks = new List<MqttPubAckPacket>(iterations);

        private async Task Write()
        {
            await Task.Yield();

            //var msgs = new MqttPublishPacket[10000];

            //for (var i = 0; i < iterations; i++)
            //{
            //    msgs[i] = _message;
            //}
            //await _connection.MqttWriter.WriteManyAsync(msgs);

            for (int i = 0; i < iterations; i++)
            {
                var ack = await _connection.PublishAtLeastOnce(topic, payload).ConfigureAwait(false);

                acks.Add(ack);
                writecount++;
            }

            await _connection.WritePacket(new MqttPingReqPacket()).ConfigureAwait(false);
        }
    }
}
