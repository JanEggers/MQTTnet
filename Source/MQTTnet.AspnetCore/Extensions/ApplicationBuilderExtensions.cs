using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using MQTTnet.Server;
using System.Collections.Generic;
using MQTTnet.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMqttEndpoint(this IApplicationBuilder app, string path = "/mqtt")
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (!context.WebSockets.IsWebSocketRequest || context.Request.Path != path)
                {
                    await next();
                    return;
                }

                string subProtocol = null;

                if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                {
                    subProtocol = SubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                }

                var adapter = app.ApplicationServices.GetRequiredService<MqttWebSocketServerAdapter>();
                using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false))
                {
                    await adapter.RunWebSocketConnectionAsync(webSocket, context);
                }
            });

            return app;
        }

        public static IApplicationBuilder UseMqttServer(this IApplicationBuilder app, Action<IMqttServer> configure)
        {
            var server = app.ApplicationServices.GetRequiredService<IMqttServer>();

            configure(server);

            return app;
        }
    }
}
