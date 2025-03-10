using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public partial class MainForm : Form
    {
        private MqttClientManager mqttManager;
        private TrayManager trayManager;
        private CancellationTokenSource cts;
        private bool isSending = false;
        private Dictionary<string, string> configuredValues = new Dictionary<string, string>();

        public MainForm(string title)
        {
            InitializeComponent();
            UIManager.SetupControl(this);
            this.Text = title;

            mqttManager = new MqttClientManager();
            trayManager = new TrayManager(this);

            // ���� "���ù���ֵ" ��ť
            Button configButton = new Button
            {
                Text = "���ù���ֵ",
                Dock = DockStyle.Top
            };
            configButton.Click += OpenConfigWindow;

            this.Controls.Add(configButton);
        }

        public async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr)
        {
            if (!isSending)
            {
                if (!int.TryParse(portStr, out int port) ||
                    !int.TryParse(keepaliveStr, out int keepalive) ||
                    !int.TryParse(intervalStr, out int interval) ||
                    string.IsNullOrWhiteSpace(broker) || string.IsNullOrWhiteSpace(topic))
                {
                    MessageBox.Show("����д���������ֶΣ���ȷ��������Ч���֡�", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                    return;
                }

                try
                {
                    if (MessageFileHandler.IsMessageFileEmpty())
                    {
                        DialogResult result = MessageBox.Show(
                            "��Ϣ����Ϊ�գ��Ƿ�������ͣ�",
                            "����",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );

                        if (result == DialogResult.No)
                        {
                            ResetButtonState(button);
                            return;
                        }
                    }

                    string message = await MessageFileHandler.ReadMessageAsync(configuredValues);
                    isSending = true;
                    button.Text = "Stop";
                    cts = new CancellationTokenSource();
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������Ϣʧ�ܣ�\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                }
            }
            else
            {
                if (mqttManager != null)
                {
                    await mqttManager.StopSendingAsync(); // �����Ͽ� MQTT ����
                }

                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
            }
        }


        public void ResetButtonState(Button button)
        {
            if (button?.IsDisposed == true) return;

            if (button?.InvokeRequired == true)
            {
                button.Invoke((MethodInvoker)(() => ResetButtonState(button)));
                return;
            }

            button.Text = "Send";
            isSending = false;
            cts = null;
        }

        private void OpenConfigWindow(object sender, EventArgs e)
        {
            ConfigForm configForm = new ConfigForm(configuredValues);
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                configuredValues = configForm.GetConfiguredValues();
                mqttManager.SetConfiguredValues(configuredValues); // ȷ�� `m-v` ֵ���ݸ� `mqttManager`
            }
        }

    }
}
