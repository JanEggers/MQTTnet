using MQTTnet.Exceptions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public static class ReadOnlySpanExtensions
    {
        public static ReadOnlySpan<byte> ToSpan(this in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return buffer.FirstSpan;
            }
            return buffer.ToArray();
        }

        public static ReadOnlySequence<byte> Read(this ref ReadOnlySequence<byte> buffer, int count)
        {
            var result = buffer.Slice(0, count);
            buffer = buffer.Slice(count);
            return result;
        }

        public static ReadOnlySpan<byte> Read(this ref ReadOnlySpan<byte> buffer, int count)
        {
            var result = buffer.Slice(0, count);
            buffer = buffer.Slice(count);
            return result;
        }

        public static byte ReadByte(this ref ReadOnlySpan<byte> buffer)
        {
            var result = buffer[0];
            buffer = buffer.Slice(1);
            return result;
        }

        public static byte[] ReadRemainingData(this ref ReadOnlySpan<byte> buffer)
        {
            return buffer.ToArray();
        }

        public static byte[] ReadWithLengthPrefix(this ref ReadOnlySpan<byte> buffer)
        {
            return buffer.ReadSegmentWithLengthPrefix().ToArray();
        }

        public static ReadOnlySequence<byte> ReadSegmentWithLengthPrefix(this ref ReadOnlySequence<byte> buffer)
        {
            ushort length = 0;

            var lengthSlice = buffer.Slice(0, 2);

            if (lengthSlice.IsSingleSegment)
            {
                length = BinaryPrimitives.ReadUInt16BigEndian(lengthSlice.FirstSpan);
            }
            else
            {
                Span<byte> temp = stackalloc byte[2];
                lengthSlice.CopyTo(temp);
                length = BinaryPrimitives.ReadUInt16BigEndian(temp);
            }


            var result = buffer.Slice(2, length);
            buffer = buffer.Slice(2 + length);
            return result;
        }

        public static ReadOnlySpan<byte> ReadSegmentWithLengthPrefix(this ref ReadOnlySpan<byte> buffer)
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(buffer);

            var result = buffer.Slice(2, length);
            buffer = buffer.Slice(2 + length);
            return result;
        }
        
        public static string ReadStringWithLengthPrefix(this ref ReadOnlySpan<byte> buffer)
        {
            var raw = buffer.ReadSegmentWithLengthPrefix();
            if (raw.Length == 0)
            {
                return string.Empty;
            }

            var result = Encoding.UTF8.GetString(raw);
            return result;
        }

        public static ushort ReadTwoByteInteger(this ref ReadOnlySpan<byte> buffer)
        {
            var result = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            buffer = buffer.Slice(2);
            return result;
        }

        public static uint ReadFourByteInteger(this ref ReadOnlySpan<byte> buffer)
        {
            var result = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            buffer = buffer.Slice(4);
            return result;
        }

        public static uint ReadVariableLengthInteger(this ref ReadOnlySpan<byte> buffer)
        {
            var multiplier = 1;
            var value = 0U;
            byte encodedByte;

            do
            {
                encodedByte = buffer.ReadByte();
                value += (uint)((encodedByte & 127) * multiplier);

                if (multiplier > 2097152)
                {
                    throw new MqttProtocolViolationException("Variable length integer is invalid.");
                }

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }

        public static bool ReadBoolean(this ref ReadOnlySpan<byte> buffer)
        {
            var raw = buffer.ReadByte();

            if (raw == 0)
            {
                return false;
            }

            if (raw == 1)
            {
                return true;
            }

            throw new MqttProtocolViolationException("Boolean values can be 0 or 1 only.");
        }
    }
}
