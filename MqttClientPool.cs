using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTMessageSenderApp
{
    public class MqttClientPool
    {
        private readonly string broker;
        private readonly int port;
        private readonly int keepalive;
        private readonly string username;
        private readonly string password;
        private readonly SemaphoreSlim semaphore;
        private readonly ConcurrentBag<IMqttClient> clients = new();
        private readonly MqttClientFactory factory = new();

        public MqttClientPool(string broker, int port, int keepalive, string username, string password, int maxConnections)
        {
            this.broker = broker;
            this.port = port;
            this.keepalive = keepalive;
            this.username = username;
            this.password = password;
            semaphore = new SemaphoreSlim(maxConnections);
        }

        public async Task<IMqttClient> GetClientAsync(CancellationToken token)
        {
            await semaphore.WaitAsync(token);

            if (clients.TryTake(out var client) && client.IsConnected)
            {
                return client;
            }

            var mqttClient = factory.CreateMqttClient();
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive));
            if (!string.IsNullOrWhiteSpace(username))
                builder = builder.WithCredentials(username, password);
            var options = builder.Build();
            await mqttClient.ConnectAsync(options, token);
            return mqttClient;
        }

        public void ReturnClient(IMqttClient client)
        {
            if (client != null && client.IsConnected)
            {
                clients.Add(client);
            }
            else
            {
                client?.Dispose();
            }
            semaphore.Release();
        }

        public async Task DisposeAsync()
        {
            while (clients.TryTake(out var client))
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync();
                }
                catch { }
                client.Dispose();
            }
        }
    }

    public static class MqttClientPoolManager
    {
        private static readonly ConcurrentDictionary<string, MqttClientPool> pools = new();

        public static MqttClientPool GetPool(string broker, int port, int keepalive, string username, string password, int maxConnections)
        {
            string key = $"{broker}:{port}:{username}:{password}";
            return pools.GetOrAdd(key, _ => new MqttClientPool(broker, port, keepalive, username, password, maxConnections));
        }

        public static async Task DisposeAllAsync()
        {
            foreach (var pool in pools.Values)
            {
                await pool.DisposeAsync();
            }
            pools.Clear();
        }
    }
}
