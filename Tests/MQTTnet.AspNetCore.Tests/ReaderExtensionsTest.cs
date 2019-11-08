#if NETCOREAPP
using System;
using System.Buffers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class ReaderExtensionsTest
    {
        public class TestReadOnlySequenceSegment : ReadOnlySequenceSegment<byte>
        {
            public TestReadOnlySequenceSegment(ReadOnlyMemory<byte> memory, long runningIndex = 0, TestReadOnlySequenceSegment next = null)
            {
                RunningIndex = runningIndex;
                Memory = memory;
                Next = next;
            }
        }

        [TestMethod]
        public void TestTryCombine()
        {
            var end = new TestReadOnlySequenceSegment(new ReadOnlyMemory<byte>(new byte[5]), runningIndex: 5);
            var start = new TestReadOnlySequenceSegment(new ReadOnlyMemory<byte>(new byte[5]), next: end);

            var sequence = new ReadOnlySequence<byte>(start, 0, end, end.Memory.Length);

            Assert.AreEqual(10, sequence.Length);
        }



        [TestMethod]
        public void TestTryDeserialize()
        {
        //    var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311);

        //    var buffer = serializer.Encode(new MqttPublishPacket() {Topic = Encoding.UTF8.GetBytes("a"), Payload = new byte[5]});

        //    var sequence = new ReadOnlySequence<byte>(buffer.Array, buffer.Offset, buffer.Count);

        //    var part = sequence;
        //    var reader = new MqttProtocolReader(serializer);

        //    part = sequence.Slice(sequence.Start, 0); // empty message should fail
        //    bool result = reader.TryParseMessage(part, out _, out _, out _);
        //    Assert.IsFalse(result);


        //    part = sequence.Slice(sequence.Start, 1); // partial fixed header should fail
        //    result = reader.TryParseMessage(part, out _, out _, out _);
        //    Assert.IsFalse(result);

        //    part = sequence.Slice(sequence.Start, 4); // partial body should fail
        //    result = reader.TryParseMessage(part, out _, out _, out _);
        //    Assert.IsFalse(result);

        //    part = sequence; // complete msg should work
        //    result = reader.TryParseMessage(part, out _, out _, out _);
        //    Assert.IsTrue(result);
        }
    }
}
#endif