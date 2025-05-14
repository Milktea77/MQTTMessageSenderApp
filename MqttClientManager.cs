using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task StartSendingAsync(string broker, string portStr, string keepaliveStr, string topic, string intervalStr, bool retain, string username, string password, CancellationToken token)
        {
            if (!int.TryParse(portStr, out int port) ||
                !int.TryParse(keepaliveStr, out int keepalive) ||
                !int.TryParse(intervalStr, out int interval))
            {
                throw new ArgumentException("端口、保活时间、间隔时间必须是有效的整数。");
            }

            var factory = new MqttClientFactory();
            mqttClient = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive));

            if (!string.IsNullOrWhiteSpace(username))
            {
                optionsBuilder = optionsBuilder.WithCredentials(username, password ?? "");
            }

            mqttOptions = optionsBuilder.Build();

            await mqttClient.ConnectAsync(mqttOptions, token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 每次发送前重新读取并更新消息内容，确保 ts 更新
                    string modifiedJson = await MessageFileHandler.ReadMessageAsync();

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(modifiedJson)
                        .WithRetainFlag(retain)
                        .Build();

                    await mqttClient.PublishAsync(mqttMessage, token);
                    // Trace.WriteLine($"发送消息成功: {modifiedJson}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"发送消息失败: {ex.Message}");
                }

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
