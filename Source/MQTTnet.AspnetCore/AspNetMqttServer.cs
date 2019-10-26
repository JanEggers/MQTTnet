using System.Reactive.Subjects;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServer 
    {
        public Subject<MqttFrame> Packets { get; } = new Subject<MqttFrame>();
    }
}
