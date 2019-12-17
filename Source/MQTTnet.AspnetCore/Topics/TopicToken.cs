using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore.Topics
{
    public ref struct TopicToken
    {
        public const char LevelSeparator = '/';
        public const char MultiLevelWildcard = '#';
        public const char SingleLevelWildcard = '+';

        public ReadOnlySpan<byte> Topic { get; set; }
    }
}
