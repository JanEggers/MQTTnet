using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Serializer;

namespace MQTTnet.Adapter
{
    public interface IMqttChannelAdapter : IDisposable
    {
        string Endpoint { get; }

        IMqttPacketSerializer PacketSerializer { get; }

        event EventHandler ReadingPacketStarted;

        event EventHandler<MqttBasePacket> ReadingPacketCompleted;

        Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken);

        Task ReceivePacketAsync(CancellationToken cancellationToken);
    }
}
