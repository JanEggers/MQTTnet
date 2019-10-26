using System;
using System.Buffers;

namespace MQTTnet.AspNetCore
{
    public class MqttBufferWriter : IBufferWriter<byte>
    {        
        private readonly ArrayPool<byte> _arrayPool;

        private byte[] _rented;
        private Memory<byte> _total;
        private Memory<byte> _remaining;
        private Memory<byte> _written;

        public Memory<byte> Written => _written;

        public MqttBufferWriter()
        {
            _arrayPool = ArrayPool<byte>.Create();
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
            if (_rented.Length <= 1024)
            {
                _remaining = _total;
                _written = Memory<byte>.Empty;
                return;
            }

            _arrayPool.Return(_rented);
            _rented = null;
            _total = _remaining = _written = Memory<byte>.Empty;
        }

        private void EnsureInitialized(int sizeHint)
        {
            if (_remaining.Length >= sizeHint)
            {
                return;
            }

            if (_total.Length == 0 && sizeHint < 1024)
            {
                sizeHint = 1024;
            }

            var newTotal = _total.Length + sizeHint - _remaining.Length;
            var newBuffer = _arrayPool.Rent(newTotal);
            _written.CopyTo(newBuffer);
            if (_rented != null)
            {
                _arrayPool.Return(_rented);
            }
            _rented = newBuffer;
            _total = newBuffer.AsMemory();
            _remaining = _total.Slice(_written.Length);
            _written = _total.Slice(0, _written.Length);
        }
    }
}