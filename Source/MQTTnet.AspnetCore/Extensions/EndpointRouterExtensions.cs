using Microsoft.AspNetCore.Routing;
using MQTTnet.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouterExtensions
    {
        public static ConnectionEndpointRouteBuilder MapMqtt(this IEndpointRouteBuilder endpoint, string pattern) 
        {
            return endpoint.MapConnectionHandler<MqttConnectionHandler>(pattern, options =>
            {
                options.WebSockets.SubProtocolSelector = SubProtocolSelector.SelectSubProtocol;
            });
        }
    }
}
