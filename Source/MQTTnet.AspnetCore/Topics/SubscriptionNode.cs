using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MQTTnet.AspNetCore.Topics
{
    public class SubscriptionNode
    {
        public class Index : SpanKeyDictionary<SubscriptionNode>
        {
            public Index()
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

        public static bool IsMatch(in ReadOnlySequence<byte> topic, Index items)
        {
            var currentItems = items;
            foreach (var token in topic.SplitSegments())
            {
                if (currentItems.IsMultiLevelWildCard)
                {
                    return true;
                }

                ReadOnlySpan<byte> key = token.IsSingleSegment ? token.FirstSpan : token.ToArray();
                
                if (!currentItems.TryGetValue(key, out var node))
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
