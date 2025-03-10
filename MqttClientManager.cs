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

        public async Task StartSendingAsync(string broker, string portStr, string keepaliveStr, string topic, string intervalStr, string modifiedJson, CancellationToken token)
        {
            if (!int.TryParse(portStr, out int port) ||
                !int.TryParse(keepaliveStr, out int keepalive) ||
                !int.TryParse(intervalStr, out int interval))
            {
                throw new ArgumentException("端口、保活时间、间隔时间必须是有效的整数。");
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
                // 发送 `modifiedJson`
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(modifiedJson)
                    .Build();

                await mqttClient.PublishAsync(mqttMessage, token);
                Console.WriteLine($"发送消息成功: {modifiedJson}");

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
