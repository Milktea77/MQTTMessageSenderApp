using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using NLog;

namespace MQTTMessageSenderApp
{
    public class MqttClientManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private IMqttClient mqttClient;
        private MqttClientOptions mqttOptions;
        private Dictionary<string, string> configuredValues = new Dictionary<string, string>(); // 存储配置的 `m-v`

        public MqttClientManager()
        {
            var factory = new MqttClientFactory();
            mqttClient = factory.CreateMqttClient();
        }

        // 新增方法：设置 `m-v` 值
        public void SetConfiguredValues(Dictionary<string, string> values)
        {
            configuredValues = new Dictionary<string, string>(values);
        }

        public async Task StartSendingAsync(string broker, string portStr, string keepaliveStr, string topic, string intervalStr, CancellationToken token)
        {
            if (!int.TryParse(portStr, out int port) || !int.TryParse(keepaliveStr, out int keepalive) || !int.TryParse(intervalStr, out int interval))
            {
                throw new ArgumentException("端口、KeepAlive 和间隔时间必须是整数！");
            }

            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }

            mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive))
                .Build();

            await mqttClient.ConnectAsync(mqttOptions, token);

            while (!token.IsCancellationRequested)
            {
                string message = await MessageFileHandler.ReadMessageAsync(configuredValues);
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .Build();

                await mqttClient.PublishAsync(mqttMessage, token);
                Logger.Info($"发送数据到 '{topic}'，时间：{DateTimeOffset.UtcNow}");
                await Task.Delay(interval, token);
            }
        }

        public async Task StopSendingAsync()
        {
            if (mqttClient != null && mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }

    }
}
