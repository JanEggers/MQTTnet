using System.Reactive.Subjects;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServer 
    {
        public Subject<MqttPublishPacket> Packets { get; } = new Subject<MqttPublishPacket>();
    }
}
