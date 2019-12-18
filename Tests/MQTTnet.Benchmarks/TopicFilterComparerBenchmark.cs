using BenchmarkDotNet.Attributes;
using MQTTnet.AspNetCore.Topics;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using MqttTopicFilterComparer = MQTTnet.Server.MqttTopicFilterComparer;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class TopicFilterComparerBenchmark
    {
        private static readonly char[] TopicLevelSeparator = { '/' };

        private List<TopicFilter> subscriptions = new List<TopicFilter>();


        private SubscriptionNode.Index _subscriptionIndex = new SubscriptionNode.Index();

        [GlobalSetup]
        public void Setup()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                var c = Convert.ToChar(i);
                if (char.IsLetter(c) && !char.IsUpper(c))
                {
                    subscriptions.Add(new TopicFilter() { Topic = Encoding.UTF8.GetBytes(new string(c, 1)) });
                }
            }


            subscriptions.RemoveRange(1000, subscriptions.Count - 1000);

            foreach (var subscription in subscriptions)
            {
                SubscriptionNode.Subscribe(subscription, _subscriptionIndex);
            }
            
            Console.WriteLine($"{subscriptions.Count} subscriptions");
        }

        //[Benchmark]
        //public void MqttTopicFilterComparer_10000_StringSplitMethod()
        //{
        //    for (var i = 0; i < 10000; i++)
        //    {
        //        LegacyMethodByStringSplit("sport/tennis/player1", "sport/#");
        //        LegacyMethodByStringSplit("sport/tennis/player1/ranking", "sport/#/ranking");
        //        LegacyMethodByStringSplit("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
        //        LegacyMethodByStringSplit("sport/tennis/player1", "sport/tennis/+");
        //        LegacyMethodByStringSplit("/finance", "+/+");
        //        LegacyMethodByStringSplit("/finance", "/+");
        //        LegacyMethodByStringSplit("/finance", "+");
        //    }
        //}

        [Benchmark]
        public void LinearSearch()
        {
            var topic = Encoding.UTF8.GetBytes("z/z");

            foreach (var subscription in subscriptions)
            {
                MqttTopicFilterComparer.IsMatch(topic, subscription.Topic);
            }
        }

        [Benchmark]
        public void IndexSearch()
        {
            var topic = Encoding.UTF8.GetBytes("z/z");

            SubscriptionNode.IsMatch(topic.AsSpan(), _subscriptionIndex);
        }


        //[Benchmark]
        //public void MqttTopicFilterComparer_10000_LoopMethod()
        //{
        //    for (var i = 0; i < 10000; i++)
        //    {
        //        //MqttTopicFilterComparer.IsMatch("sport/tennis/player1", "sport/#");
        //        //MqttTopicFilterComparer.IsMatch("sport/tennis/player1/ranking", "sport/#/ranking");
        //        //MqttTopicFilterComparer.IsMatch("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
        //        //MqttTopicFilterComparer.IsMatch("sport/tennis/player1", "sport/tennis/+");
        //        //MqttTopicFilterComparer.IsMatch("/finance", "+/+");
        //        //MqttTopicFilterComparer.IsMatch("/finance", "/+");
        //        //MqttTopicFilterComparer.IsMatch("/finance", "+");
        //    }
        //}

        private static bool LegacyMethodByStringSplit(string topic, string filter)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (string.Equals(topic, filter, StringComparison.Ordinal))
            {
                return true;
            }

            var fragmentsTopic = topic.Split(TopicLevelSeparator, StringSplitOptions.None);
            var fragmentsFilter = filter.Split(TopicLevelSeparator, StringSplitOptions.None);

            // # > In either case it MUST be the last character specified in the Topic Filter [MQTT-4.7.1-2].
            for (var i = 0; i < fragmentsFilter.Length; i++)
            {
                if (fragmentsFilter[i] == "+")
                {
                    continue;
                }

                if (fragmentsFilter[i] == "#")
                {
                    return true;
                }

                if (i >= fragmentsTopic.Length)
                {
                    return false;
                }

                if (!string.Equals(fragmentsFilter[i], fragmentsTopic[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return fragmentsTopic.Length == fragmentsFilter.Length;
        }
    }
}
