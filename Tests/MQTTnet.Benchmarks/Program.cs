using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"MQTTnet - BenchmarkApp.{TargetFrameworkInfoProvider.TargetFramework}");
            //Console.WriteLine("1 = MessageProcessingBenchmark");
            //Console.WriteLine("2 = SerializerBenchmark");
            //Console.WriteLine("3 = LoggerBenchmark");
            //Console.WriteLine("4 = TopicFilterComparerBenchmark");
            //Console.WriteLine("5 = ChannelAdapterBenchmark");
            //Console.WriteLine("6 = MqttTcpChannelBenchmark");
            //Console.WriteLine("7 = MessageProcessingMqttConnectionContextBenchmark");

            //var pressedKey = Console.ReadKey(true);
            //switch (pressedKey.KeyChar)
            //{
            //    case '1':
            //        BenchmarkRunner.Run<MessageProcessingBenchmark>();
            //        break;
            //    case '2':
            //        BenchmarkRunner.Run<SerializerBenchmark>();
            //        break;
            //    case '3':
            //        BenchmarkRunner.Run<LoggerBenchmark>();
            //        break;
            //    case '4':
            //        BenchmarkRunner.Run<TopicFilterComparerBenchmark>();
            //        break;
            //    case '5':
            //        BenchmarkRunner.Run<ChannelAdapterBenchmark>();
            //        break;
            //    case '6':
            //        BenchmarkRunner.Run<MqttTcpChannelBenchmark>();
            //        break;
            //    case '7':
            BenchmarkRunner.Run<MessageProcessingMqttConnectionContextBenchmark>(
                new DebugInProcessConfig()
                //DefaultConfig.Instance
                //.With(new ConcurrencyVisualizerProfiler())
                //.With(Job.InProcess)
                );
            //        break;
            //}

            //BenchmarkRunner.Run<TopicFilterComparerBenchmark>(
            //            new DebugInProcessConfig()
            //            //DefaultConfig.Instance
            //            //.With(new ConcurrencyVisualizerProfiler())
            //            //.With(Job.InProcess)
            //            );

            Console.ReadLine();
        }
    }
}
