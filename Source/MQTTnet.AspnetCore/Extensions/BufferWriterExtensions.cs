using System.Buffers.Binary;
using System.Text;

namespace System.Buffers
{
    public static class BufferWriterExtensions 
    {
        public static void Write(this IBufferWriter<byte> writer, byte data)
        {
            var written = 1;
            writer.GetSpan(written)[0] = data;
            writer.Advance(written);
        }

        public static void Write(this IBufferWriter<byte> writer, ushort data)
        {
            var written = 2;
            var buffer = writer.GetSpan(written);
            BinaryPrimitives.WriteUInt16BigEndian(buffer, data);
            writer.Advance(written);
        }

        public static void WriteWithLengthPrefix(this IBufferWriter<byte> writer, byte[] data) 
        {
            var written = data.Length + 2;
            var buffer = writer.GetSpan(written);
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)data.Length);
            data.CopyTo(buffer.Slice(2));
            writer.Advance(written);
        }
               
        public static void WriteWithLengthPrefix(this IBufferWriter<byte> writer, string data)
        {
            var finalData = (data ?? string.Empty).AsSpan();
            var bytesLength = Encoding.UTF8.GetByteCount(finalData);
            var written = bytesLength + 2;
            var buffer = writer.GetSpan(written);

            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)data.Length);
            Encoding.UTF8.GetBytes(finalData, buffer.Slice(2));
            writer.Advance(written);
        }

        public static void WriteVariableLengthInteger(this Span<byte> buffer, int value)
        {
            var x = value;
            var position = 0;
            do
            {
                var encodedByte = x % 128;
                x = x / 128;
                if (x > 0)
                {
                    encodedByte = encodedByte | 128;
                }

                buffer[position] = (byte)encodedByte;
                position++;
            } while (x > 0);
        }
    }
}
