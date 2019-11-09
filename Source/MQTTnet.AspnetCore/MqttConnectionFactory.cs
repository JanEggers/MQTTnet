using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttConnectionFactory
    {
        private readonly IServiceProvider serviceProvider;

        public MqttConnectionFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        
        public async ValueTask MaintainConnection<T>(EndPoint endpoint, CancellationToken ct = default)
            where T : MqttClientConnection
        {
            var clientBuilder = new ClientBuilder(serviceProvider);
            var logger = serviceProvider.GetRequiredService<ILogger<MqttConnectionFactory>>();

            switch (endpoint)
            {
                case IPEndPoint _:
                case DnsEndPoint _:
                    clientBuilder.UseSockets();
                    break;
                default:
                    break;
            }

            var client = clientBuilder.Build();

            while (!ct.IsCancellationRequested)
            {
                ConnectionContext connection;
                try
                {
                    connection = await client.ConnectAsync(endpoint, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "failed to connect to {Endpoint}", endpoint);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    continue;
                }

                using (logger.BeginScope(connection.ConnectionId))
                using (var scope = serviceProvider.CreateScope())
                {
                    logger.LogInformation("connected to {Endpoint}", endpoint);
                    var handler = ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider, connection);

                    try
                    {
                        await handler.RunConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "connection {Connection} failed", connection);
                    }
                }
            }
        }

        public async ValueTask<T> ConnectAsync<T>(EndPoint endpoint, CancellationToken ct = default)
        {
            var clientBuilder = new ClientBuilder(serviceProvider);

            switch (endpoint)
            {
                case IPEndPoint _:
                case DnsEndPoint _:
                    clientBuilder.UseSockets();
                    break;
                default:
                    break;
            }

            var client = clientBuilder.Build();
            var connection = await client.ConnectAsync(endpoint, ct);
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, connection);
        }
    }
}
