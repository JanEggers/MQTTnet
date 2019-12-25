using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MQTTnet.AspNetCore.Topics
{
    public class SubscriptionNode
    {
        public class Comparer : IEqualityComparer<byte[]>
        {
            bool IEqualityComparer<byte[]>.Equals([AllowNull] byte[] left, [AllowNull] byte[] right)
            {
                if (left.Length == 1 && left[0] == TopicToken.SingleLevelWildcard || left[0] == TopicToken.MultiLevelWildcard)
                {
                    return true;
                }

                return left.AsSpan().SequenceEqual(right);
            }

            int IEqualityComparer<byte[]>.GetHashCode([DisallowNull] byte[] data)
            {
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

        public class Index : Dictionary<byte[], SubscriptionNode>
        {
            public Index()
                : base(new Comparer())
            {
            }

            public SubscriptionNode SingleLevelWildcard { get; set; }

            public bool IsMultiLevelWildCard { get; set; }
        }

        public byte[] Id { get; }
        public Index Children { get; }

        public SubscriptionNode(byte[] id)
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
            var key = topicSegment.ToArray();

            if (topicSegment.Length == 1)
            {
                switch (topicSegment[0])
                {
                    case (byte)TopicToken.SingleLevelWildcard:
                        if (items.SingleLevelWildcard == null)
                        {
                            items.SingleLevelWildcard = new SubscriptionNode(Array.Empty<byte>());
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
            get { return Id.Length > 0 && Id[0] == TopicToken.MultiLevelWildcard; }
        }
    }
}
