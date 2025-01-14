using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using NLog;

// 解决二义性的别名设定
//using CustomForm = MQTTMessageSenderApp.MainForm;

namespace MQTTMessageSenderApp
{
    public partial class MainForm : Form
    {
        private IMqttClient mqttClient;
        private MqttClientOptions mqttOptions;
        private CancellationTokenSource cts;
        private string messageFile = Path.Combine(Directory.GetCurrentDirectory(), "sim_message.txt");

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MainForm(string title)
        {
            setupControl();
            this.Text = title;

            // 显示当前所在路径
            // 创建 Label 和 ToolTip
            Label pathLabel = new Label { Left = 20, AutoSize = true, Cursor = Cursors.Hand }; // 添加手型光标
            ToolTip pathToolTip = new ToolTip();

            // 设置 Label 的文本和 ToolTip 的文本
            string currentPath = Directory.GetCurrentDirectory();
            pathLabel.Text = $"Software Path: {currentPath}";
            pathToolTip.SetToolTip(pathLabel, currentPath);

            // 限制 Label 最大宽度并显示省略号
            pathLabel.MaximumSize = new System.Drawing.Size(this.ClientSize.Width - 40, 0);
            pathLabel.AutoEllipsis = true;

            // 动态计算 Label 的 Top 位置
            pathLabel.Top = this.ClientSize.Height - pathLabel.Height - 10;

            // 添加点击事件处理程序
            pathLabel.Click += (sender, e) =>
            {
                try
                {
                    Clipboard.SetText(currentPath);
                    pathToolTip.Show("路径已复制到剪贴板", pathLabel, pathLabel.Width / 2, -pathLabel.Height, 1000); // 显示提示信息
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制路径失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Controls.Add(pathLabel);
        }

        private void setupControl()
        {
            this.Text = "MQTT Message Sender";
            this.ClientSize = new System.Drawing.Size(400, 500);

            var labelBroker = new Label { Text = "Broker IP:", Top = 20, Left = 20, Width = 100 };
            var labelPort = new Label { Text = "Port:", Top = 70, Left = 20, Width = 100 };
            var labelKeepalive = new Label { Text = "Keepalive (sec):", Top = 120, Left = 20, Width = 120 };
            var labelTopic = new Label { Text = "Topic:", Top = 170, Left = 20, Width = 100 };
            var labelInterval = new Label { Text = "Interval (ms):", Top = 220, Left = 20, Width = 120 };
            var labelFileHint = new Label { Text = "请确保软件同一目录下存在sim_message.txt文件", Top = 370, Left = 20, Width = 300 };

            var textBoxBroker = new TextBox { Top = 20, Left = 150, Width = 200 };
            var textBoxPort = new TextBox { Top = 70, Left = 150, Width = 200, Text = "1883" };
            var textBoxKeepalive = new TextBox { Top = 120, Left = 150, Width = 200, Text = "60" };
            var textBoxTopic = new TextBox { Top = 170, Left = 150, Width = 200 };
            var textBoxInterval = new TextBox { Top = 220, Left = 150, Width = 200, Text = "60000" }; // 默认值为 60 秒

            var buttonSend = new Button { Text = "Send", Top = 300, Left = 150, Width = 100 };
            buttonSend.Click += async (sender, e) =>
                await ToggleSendAsync(buttonSend, textBoxBroker.Text, textBoxPort.Text, textBoxKeepalive.Text, textBoxTopic.Text, textBoxInterval.Text);

            this.Controls.Add(labelBroker);
            this.Controls.Add(labelPort);
            this.Controls.Add(labelKeepalive);
            this.Controls.Add(labelTopic);
            this.Controls.Add(labelInterval);
            this.Controls.Add(labelFileHint);
            this.Controls.Add(textBoxBroker);
            this.Controls.Add(textBoxPort);
            this.Controls.Add(textBoxKeepalive);
            this.Controls.Add(textBoxTopic);
            this.Controls.Add(textBoxInterval);
            this.Controls.Add(buttonSend);
        }


        private async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr)
        {
            if (mqttClient == null || !mqttClient.IsConnected)
            {
                if (!int.TryParse(portStr, out int port) ||
                    !int.TryParse(keepaliveStr, out int keepalive) ||
                    !int.TryParse(intervalStr, out int interval) ||
                    string.IsNullOrWhiteSpace(broker) ||
                    string.IsNullOrWhiteSpace(topic))
                {
                    MessageBox.Show("请填写所有输入字段，并确保输入有效数字。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    mqttClient = new MqttClientFactory().CreateMqttClient();

                    mqttOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer(broker, port)
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepalive))
                        .Build();

                    await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);

                    button.Text = "Stop";
                    cts = new CancellationTokenSource();
                    _ = SendMessagesAsync(topic, interval, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接失败： {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                cts.Cancel();
                await mqttClient.DisconnectAsync();
                button.Text = "Send";
            }
        }


        // 报错时重置按钮为Send
        //private void ResetButtonState(Button button)
        //{
        //    // Ensure UI updates are done on the main thread.
        //    if (button.InvokeRequired)
        //    {
        //        button.Invoke((MethodInvoker)(() => ResetButtonState(button)));
        //        return;
        //    }

        //    button.Text = "Send";
        //    cts = null; // Clear the cancellation token source.
        //}

        private async Task SendMessagesAsync(string topic, int interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!File.Exists(messageFile))
                    {
                        MessageBox.Show($"消息文件 '{messageFile}' 在同目录中不存在！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    var message = await File.ReadAllTextAsync(messageFile);
                    var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

                    if (jsonObj.ContainsKey("ts"))
                    {
                        jsonObj["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                    else
                    {
                        jsonObj.Add("ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    }

                    message = JsonSerializer.Serialize(jsonObj);

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(message)
                        .Build();

                    var result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);
                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        MessageBox.Show($"推送失败： {result.ReasonCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    Logger.Info($"Message sent to topic '{topic}' at {DateTime.Now}");
                    await Task.Delay(interval, cancellationToken); // 使用动态间隔
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发送消息失败或被手动中止: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }


        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm("MQTT Message Sender"));
        }
    }
}