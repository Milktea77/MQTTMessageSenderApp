using MQTTnet;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MQTTMessageSenderApp
{
    public class MqttClientManager
    {
        private IMqttClient mqttClient;
        private MqttClientOptions mqttOptions;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public async Task StartSendingAsync(string broker, string portStr, string keepaliveStr, string topic, string intervalStr, CancellationToken token)
        {
            if (!int.TryParse(portStr, out int port) ||
                !int.TryParse(keepaliveStr, out int keepalive) ||
                !int.TryParse(intervalStr, out int interval))
            {
                throw new ArgumentException("端口、保活时间、间隔必须是整数");
            }

            var factory = new MqttClientFactory();
            mqttClient = factory.CreateMqttClient();

            mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive))
                .Build();

            await mqttClient.ConnectAsync(mqttOptions, token);

            while (!token.IsCancellationRequested)
            {
                string message = await MessageFileHandler.ReadMessageAsync();
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .Build();

                await mqttClient.PublishAsync(mqttMessage, token);
                Logger.Info($"Message sent to topic '{topic}' at {DateTime.Now}");
                await Task.Delay(interval, token);
            }
        }
    }
}
