using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class ArrayBufferWriter : IBufferWriter<byte>
    {
        public ArrayBufferWriter(int initialCapacity = 256)
        {
            _remaining = _buffer = _arr = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _written = _buffer.Slice(0, 0);
        }

        private byte[] _arr;
        private Memory<byte> _buffer;
        private Memory<byte> _remaining;
        private Memory<byte> _written;

        public void Advance(int count) 
        { 
            var split = _written.Length + count;

            _remaining = _buffer.Slice(split);
            _written = _buffer.Slice(0, split);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Grow(sizeHint);

            return _remaining;
        }

        private void Grow(int sizeHint)
        {
            if (sizeHint > _remaining.Length)
            {
                var temp = ArrayPool<byte>.Shared.Rent(_buffer.Length - _remaining.Length + sizeHint);
                _buffer.CopyTo(temp);
                ArrayPool<byte>.Shared.Return(_arr);
                _buffer = _arr = temp;
                _remaining = _buffer.Slice(_written.Length);
                _written = _buffer.Slice(0, _written.Length);
            }
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Grow(sizeHint);

            return _remaining.Span;
        }

        public Memory<byte> Output => _written;
    }
}
