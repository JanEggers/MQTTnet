#if NETCOREAPP
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