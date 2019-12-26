// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.AspNetCore.Topics;
using System.Buffers;

namespace System
{
    public static partial class MemoryExtensions
    {
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span)
            => new SpanSplitEnumerator<char>(span, ' ');

        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
            => new SpanSplitEnumerator<char>(span, separator);

        public static SpanSplitSequenceEnumerator<char> Split(this ReadOnlySpan<char> span, string separator)
            => new SpanSplitSequenceEnumerator<char>(span, separator);


        public static SpanSplitEnumerator<byte> Split(this ReadOnlySpan<byte> span)
            => new SpanSplitEnumerator<byte>(span, (byte)' ');

        public static SpanSplitEnumerator<byte> Split(this ReadOnlySpan<byte> span, byte separator)
            => new SpanSplitEnumerator<byte>(span, separator);

        public static SpanSplitEnumerator<byte> SplitSegments(this ReadOnlySpan<byte> span)
            => span.Split((byte)TopicToken.LevelSeparator);


        public static SequenceSplitEnumerator<byte> Split(this ReadOnlySequence<byte> sequence, byte separator)
            => new SequenceSplitEnumerator<byte>(sequence, separator);
        public static SequenceSplitEnumerator<byte> SplitSegments(this ReadOnlySequence<byte> sequence)
            => sequence.Split((byte)TopicToken.LevelSeparator);
    }

    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly T _separator;
        private int _offset;
        private int _index;

        public SpanSplitEnumerator<T> GetEnumerator() => this;

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            _sequence = span;
            _separator = separator;
            _index = 0;
            _offset = 0;
        }

        public ReadOnlySpan<T> Current => _sequence.Slice(_offset, _index - 1);

        public bool MoveNext()
        {
            if (_sequence.Length - _offset < _index) { return false; }
            var slice = _sequence.Slice(_offset += _index);

            var nextIdx = slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + 1;
            return true;
        }
    }

    public ref struct SpanSplitSequenceEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly ReadOnlySpan<T> _separator;
        private int _offset;
        private int _index;

        public SpanSplitSequenceEnumerator<T> GetEnumerator() => this;

        internal SpanSplitSequenceEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator)
        {
            _sequence = span;
            _separator = separator;
            _index = 0;
            _offset = 0;
        }

        public ReadOnlySpan<T> Current => _sequence.Slice(_offset, _index - 1);

        public bool MoveNext()
        {
            if (_sequence.Length - _offset < _index) { return false; }
            var slice = _sequence.Slice(_offset += _index);

            var nextIdx = slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + 1;
            return true;
        }
    }

    public ref struct SequenceSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySequence<T> _sequence;
        private readonly T _separator;
        private SequencePosition _offset;
        private SequencePosition _index;

        public SequenceSplitEnumerator<T> GetEnumerator() => this;

        internal SequenceSplitEnumerator(ReadOnlySequence<T> sequence, T separator)
        {
            _sequence = sequence;
            _separator = separator;
            _index = _sequence.Start;
            _offset = _sequence.Start;
        }

        public ReadOnlySequence<T> Current => _sequence.Slice(_offset, _index);

        public bool MoveNext()
        {
            if (_index.Equals(_sequence.End)) { return false; }

            if (!_index.Equals(_sequence.Start))
            {
                _offset = _sequence.GetPosition(1, _index);
            }
            
            var nextIdx = _sequence.Slice(_offset).PositionOf(_separator);
            _index = nextIdx.HasValue ? nextIdx.Value : _sequence.End;
            return true;
        }
    }
}