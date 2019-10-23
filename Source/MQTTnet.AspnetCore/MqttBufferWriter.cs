using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttBufferWriter : IBufferWriter<byte>
    {        
        private readonly MemoryPool<byte> _memoryPool;

        private IMemoryOwner<byte> _owner;
        private Memory<byte> _total;
        private Memory<byte> _remaining;
        private Memory<byte> _written;

        public Memory<byte> Written => _written;

        public MqttBufferWriter()
        {
            _memoryPool = MemoryPool<byte>.Shared;
            _total = _remaining = _written = Memory<byte>.Empty;
        }

        /// <inheritdoc />
        public void Advance(int count)
        {
            var newWritten = _written.Length + count;
            _remaining = _total.Slice(newWritten);
            _written = _total.Slice(0, newWritten);
        }

        /// <inheritdoc />
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureInitialized(sizeHint);
            return _remaining;
        }

        /// <inheritdoc />
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureInitialized(sizeHint);
            return _remaining.Span;
        }

        public void Reset()
        {
            _owner.Dispose();
            _total = _remaining = _written = Memory<byte>.Empty;
        }

        private void EnsureInitialized(int sizeHint)
        {
            if (_remaining.Length >= sizeHint)
            {
                return;
            }

            var newTotal = _total.Length + sizeHint - _remaining.Length;
            var newOwner = _memoryPool.Rent(newTotal);
            _written.CopyTo(newOwner.Memory);
            _owner?.Dispose();
            _owner = newOwner;
            _total = newOwner.Memory;
            _remaining = _total.Slice(_written.Length);
            _written = _total.Slice(0, _written.Length);
        }
    }
}