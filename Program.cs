using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using NLog;

// ��������Եı����趨
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
        }

        private void setupControl()
        {
            this.Text = "MQTT Message Sender";
            this.ClientSize = new System.Drawing.Size(400, 500);

            var labelBroker = new Label { Text = "Broker IP:", Top = 20, Left = 20, Width = 100 };
            var labelPort = new Label { Text = "Port:", Top = 70, Left = 20, Width = 100 };
            var labelKeepalive = new Label { Text = "Keepalive (sec):", Top = 120, Left = 20, Width = 120 };
            var labelTopic = new Label { Text = "Topic:", Top = 170, Left = 20, Width = 100 };
            var labelFileHint = new Label { Text = "��ȷ�����ͬһĿ¼�´���sim_message.txt�ļ�", Top = 320, Left = 20, Width = 300 };

            var textBoxBroker = new TextBox { Top = 20, Left = 150, Width = 200 };
            var textBoxPort = new TextBox { Top = 70, Left = 150, Width = 200, Text = "1883" };
            var textBoxKeepalive = new TextBox { Top = 120, Left = 150, Width = 200, Text = "60" };
            var textBoxTopic = new TextBox { Top = 170, Left = 150, Width = 200 };

            var buttonSend = new Button { Text = "Send", Top = 250, Left = 150, Width = 100 };
            buttonSend.Click += async (sender, e) => await ToggleSendAsync(buttonSend, textBoxBroker.Text, textBoxPort.Text, textBoxKeepalive.Text, textBoxTopic.Text);

            this.Controls.Add(labelBroker);
            this.Controls.Add(labelPort);
            this.Controls.Add(labelKeepalive);
            this.Controls.Add(labelTopic);
            this.Controls.Add(labelFileHint);
            this.Controls.Add(textBoxBroker);
            this.Controls.Add(textBoxPort);
            this.Controls.Add(textBoxKeepalive);
            this.Controls.Add(textBoxTopic);
            this.Controls.Add(buttonSend);
        }

        private async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic)
        {
            if (mqttClient == null || !mqttClient.IsConnected)
            {
                if (!int.TryParse(portStr, out int port) || !int.TryParse(keepaliveStr, out int keepalive) || string.IsNullOrWhiteSpace(broker) || string.IsNullOrWhiteSpace(topic))
                {
                    MessageBox.Show("����д���������ֶΡ�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    _ = SendMessagesAsync(topic, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"����ʧ�ܣ� {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                cts.Cancel();
                await mqttClient.DisconnectAsync();
                button.Text = "Send";
            }
        }

        // ����ʱ���ð�ťΪSend
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

        private async Task SendMessagesAsync(string topic, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!File.Exists(messageFile))
                    {
                        MessageBox.Show($"��Ϣ�ļ� '{messageFile}' ��ͬĿ¼�в����ڣ�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    var message = await File.ReadAllTextAsync(messageFile);
                    try
                    {
                        JsonDocument.Parse(message); // Validate JSON
                    }
                    catch
                    {
                        MessageBox.Show("���ļ���JSON��ʽ��Ч��'sim_message.txt'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(message)
                        .Build();

                    var result = await mqttClient.PublishAsync(mqttMessage, cancellationToken);
                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        MessageBox.Show($"����ʧ�ܣ� {result.ReasonCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    //Console.WriteLine($"Message sent to topic '{topic}' at {DateTime.Now}");
                    Logger.Info($"Message sent to topic '{topic}' at {DateTime.Now}");
                    await Task.Delay(60 * 1000, cancellationToken); // Wait for 60 seconds
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������Ϣʧ�ܻ��ֶ���ֹ: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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