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

                    if (left.Length == 1 && left[0] == TopicToken.SingleLevelWildcard || left[0] == TopicToken.MultiLevelWildcard)
                    {
                        return true;
                    }

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
                    var data = GetData(obj);

                    unchecked
                    {
                        const int p = 16777619;
                        int hash = (int)2166136261;

                        for (int i = 0; i < data.Length; i++)
                            hash = (hash ^ data[i]) * p;

                        hash += hash << 13;
                        hash ^= hash >> 7;
                        hash += hash << 3;
                        hash ^= hash >> 17;
                        hash += hash << 5;
                        return hash;
                    }
                }
            }
        }

        public class Index : Dictionary<object, SubscriptionNode>
        {
            public Index()
                : base(new Key.Comparer())
            {
            }

            public SubscriptionNode SingleLevelWildcard { get; set; }

            public bool IsMultiLevelWildCard { get; set; }
        }

        public Key Id { get; }
        public Index Children { get; }

        public SubscriptionNode(Key id)
        {
            Id = id;
            Children = new Index();
        }

        public static void Subscribe(TopicFilter filter, Index items)
        {
            ReadOnlySpan<byte> topic = filter.Topic.AsSpan();
            var currentItems = items;

            foreach (var segment in topic.SplitSegments())
            {
                currentItems = Subscribe(segment, currentItems);
                if (currentItems == null)
                {
                    break;
                }
            }
        }

        private static Index Subscribe(ReadOnlySpan<byte> topicSegment, Index items)
        {
            var key = new Key(topicSegment.ToArray());

            if (topicSegment.Length == 1)
            {
                switch (topicSegment[0])
                {
                    case (byte)TopicToken.SingleLevelWildcard:
                        if (items.SingleLevelWildcard == null)
                        {
                            items.SingleLevelWildcard = new SubscriptionNode(new Key(Memory<byte>.Empty));
                        }

                        return items.SingleLevelWildcard.Children;
                    case (byte)TopicToken.MultiLevelWildcard:
                        items.IsMultiLevelWildCard = true;
                        return null;
                    default:
                        break;
                }
            }


            if (!items.TryGetValue(key, out var node))
            {
                node = new SubscriptionNode(key);
                items.Add(key, node);
            }

            return node.Children;
        }

        public static bool IsMatch(ReadOnlySpan<byte> topic, Index items)
        {
            var currentItems = items;
            foreach (var token in topic.SplitSegments())
            {
                if (currentItems.IsMultiLevelWildCard)
                {
                    return true;
                }
                
                if (!currentItems.TryGetValue(token.ToArray(), out var node))
                {
                    if (currentItems.SingleLevelWildcard == null)
                    {
                        return false;
                    }
                    else 
                    {
                        node = currentItems.SingleLevelWildcard;
                    }
                }

                currentItems = node.Children;
            }

            return true;
        }

        public bool IsMultiLevelWildCard 
        {
            get { return Id.Data.Length > 0 && Id.Data.Span[0] == TopicToken.MultiLevelWildcard; }
        }
    }
}
