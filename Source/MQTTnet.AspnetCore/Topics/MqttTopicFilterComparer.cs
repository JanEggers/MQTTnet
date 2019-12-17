using System;

namespace MQTTnet.AspNetCore.Topics
{
    public static class MqttTopicFilterComparer
    {

        public static bool IsMatch(ReadOnlySpan<byte> topic, ReadOnlySpan<byte> filter)
        {
            var sPos = 0;
            var sLen = filter.Length;
            var tPos = 0;
            var tLen = topic.Length;

            while (sPos < sLen && tPos < tLen)
            {
                if (filter[sPos] == topic[tPos])
                {
                    if (tPos == tLen - 1)
                    {
                        // Check for e.g. foo matching foo/#
                        if (sPos == sLen - 3
                                && filter[sPos + 1] == TopicToken.LevelSeparator
                                && filter[sPos + 2] == TopicToken.MultiLevelWildcard)
                        {
                            return true;
                        }
                    }

                    sPos++;
                    tPos++;

                    if (sPos == sLen && tPos == tLen)
                    {
                        return true;
                    }

                    if (tPos == tLen && sPos == sLen - 1 && filter[sPos] == TopicToken.SingleLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != TopicToken.LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                }
                else
                {
                    if (filter[sPos] == TopicToken.SingleLevelWildcard)
                    {
                        // Check for bad "+foo" or "a/+foo" subscription
                        if (sPos > 0 && filter[sPos - 1] != TopicToken.LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        // Check for bad "foo+" or "foo+/a" subscription
                        if (sPos < sLen - 1 && filter[sPos + 1] != TopicToken.LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        sPos++;
                        while (tPos < tLen && topic[tPos] != TopicToken.LevelSeparator)
                        {
                            tPos++;
                        }

                        if (tPos == tLen && sPos == sLen)
                        {
                            return true;
                        }
                    }
                    else if (filter[sPos] == TopicToken.MultiLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != TopicToken.LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        if (sPos + 1 != sLen)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        // Check for e.g. foo/bar matching foo/+/#
                        if (sPos > 0
                                && sPos + 2 == sLen
                                && tPos == tLen
                                && filter[sPos - 1] == TopicToken.SingleLevelWildcard
                                && filter[sPos] == TopicToken.LevelSeparator
                                && filter[sPos + 1] == TopicToken.MultiLevelWildcard)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }
    }
}
