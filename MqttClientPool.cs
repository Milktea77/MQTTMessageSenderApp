using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTMessageSenderApp
{
    public class MqttClientPool
    {
        private readonly ConcurrentBag<IMqttClient> clients = new();
        private readonly MqttClientOptions options;
        private readonly MqttClientFactory factory = new();

        private MqttClientPool(string broker, int port, int keepalive, string username, string password)
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive));

            if (!string.IsNullOrEmpty(username))
            {
                builder = builder.WithCredentials(username, password);
            }

            options = builder.Build();
        }

        public async Task<IMqttClient> GetClientAsync(CancellationToken token)
        {
            if (clients.TryTake(out var client))
            {
                if (!client.IsConnected)
                {
                    try
                    {
                        await client.ConnectAsync(options, token);
                    }
                    catch
                    {
                        client.Dispose();
                        client = null;
                    }
                }

                if (client != null)
                    return client;
            }

            var newClient = factory.CreateMqttClient();
            await newClient.ConnectAsync(options, token);
            return newClient;
        }

        public void ReturnClient(IMqttClient client)
        {
            if (client != null)
            {
                clients.Add(client);
            }
        }

        private static readonly ConcurrentDictionary<string, MqttClientPool> pools = new();

        public static MqttClientPool GetPool(string broker, int port, int keepalive, string username, string password)
        {
            string key = $"{broker}:{port}:{keepalive}:{username}:{password}";
            return pools.GetOrAdd(key, _ => new MqttClientPool(broker, port, keepalive, username, password));
        }

        public static void ClearAll()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.clients.TryTake(out var client))
                {
                    try
                    {
                        if (client.IsConnected)
                        {
                            client.DisconnectAsync().Wait();
                        }
                    }
                    catch { }
                    finally
                    {
                        client.Dispose();
                    }
                }
            }

            pools.Clear();
        }
    }
}
