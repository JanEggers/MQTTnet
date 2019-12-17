using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MQTTnet.AspNetCore.Topics
{
    public class SubscriptionNode
    {
        public class Key 
        {
            public ReadOnlyMemory<byte> Data { get; }

            public Key(ReadOnlyMemory<byte> data)
            {
                Data = data;
            }

            public override string ToString()
            {
                return Encoding.UTF8.GetString(Data.Span);
            }
            
            public class Comparer : IEqualityComparer<object>
            {
                public bool Equals([AllowNull] object x, [AllowNull] object y)
                {
                    var left = GetData(x);
                    var right = GetData(y);
                    return left.SequenceEqual(right);
                }

                private ReadOnlySpan<byte> GetData(object obj)
                {
                    switch (obj)
                    {
                        case Key key:
                            return key.Data.Span;
                        case ReadOnlyMemory<byte> mem:
                            return mem.Span;
                        case byte[] arr:
                            return arr.AsSpan();
                        default:
                            return ReadOnlySpan<byte>.Empty;
                    }
                }

                public int GetHashCode([DisallowNull] object obj)
                {
                    return 42;
                }
            }
        }

        public Key Id { get; }
        public Dictionary<object, SubscriptionNode> Children { get; }

        public SubscriptionNode(Key id)
        {
            Id = id;
            Children = new Dictionary<object, SubscriptionNode>(new Key.Comparer());
        }

        public static void Subscribe(TopicFilter filter, Dictionary<object, SubscriptionNode> items)
        {
            ReadOnlySpan<byte> topic = filter.Topic.AsSpan();
            var currentItems = items;

            foreach (var segment in topic.SplitSegments())
            {
                currentItems = Subscribe(segment, currentItems);
            }
        }

        private static Dictionary<object, SubscriptionNode> Subscribe(ReadOnlySpan<byte> topicSegment, Dictionary<object, SubscriptionNode> items)
        {
            var key = new Key(topicSegment.ToArray());
            if (!items.TryGetValue(key, out var node))
            {
                node = new SubscriptionNode(key);
                items.Add(key, node);
            }

            return node.Children;
        }

        public static bool IsMatch(ReadOnlySpan<byte> topic, Dictionary<object, SubscriptionNode> items)
        {
            var currentItems = items;
            foreach (var token in topic.SplitSegments())
            {
                if (!currentItems.TryGetValue(token.ToArray(), out var node))
                {
                    return false;
                }

                currentItems = node.Children;
            }

            return true;
        }
    }
}
