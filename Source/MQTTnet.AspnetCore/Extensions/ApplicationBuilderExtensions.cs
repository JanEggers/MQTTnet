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
        public static IApplicationBuilder UseMqttServer(this IApplicationBuilder app, Action<IMqttServer> configure)
        {
            var server = app.ApplicationServices.GetRequiredService<IMqttServer>();

            configure(server);

            return app;
        }
    }
}
