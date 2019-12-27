using Microsoft.AspNetCore.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore.Tests.Mockups;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using Bedrock.Framework.Protocols;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class MqttConnectionContextTest
    {
        [TestMethod]
        public async Task TestReceivePacketAsyncThrowsWhenReaderCompleted()
        {
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var reader = connection.CreateReader();

            pipe.Receive.Writer.Complete();

            await Assert.ThrowsExceptionAsync<MqttCommunicationException>(() => reader.ReadAsync(MqttProtocolVersion.V311.CreateReader(), CancellationToken.None).AsTask());
        }
        
        [TestMethod]
        public async Task TestParallelWrites()
        {
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var writer = connection.CreateWriter();
            var mqttWriter = MqttProtocolVersion.V311.CreateWriter();

            var tasks = Enumerable.Range(1, 10).Select(_ => Task.Run(async () => 
            {
                for (int i = 0; i < 100; i++)
                {
                    await writer.WriteAsync(mqttWriter, new MqttPublishPacket(), CancellationToken.None).ConfigureAwait(false);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }


        [TestMethod]
        public async Task TestLargePacket()
        {
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var writer = connection.CreateWriter();
            var mqttWriter = MqttProtocolVersion.V311.CreateWriter();

            await writer.WriteAsync(mqttWriter, new MqttPublishPacket() { Payload = new byte[20_000] }, CancellationToken.None).ConfigureAwait(false);

            var readResult = await pipe.Send.Reader.ReadAsync();
            Assert.IsTrue(readResult.Buffer.Length > 20000);
        }
    }
}
