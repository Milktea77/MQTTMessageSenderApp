using System;
using System.Diagnostics;
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
        private Button configButton;

        public MainForm(string title)
        {
            InitializeComponent();
            UIManager.SetupControl(this);
            this.Text = title;

            mqttManager = new MqttClientManager();
            trayManager = new TrayManager(this);

            // 新增 "配置功能值" 按钮
            configButton = new Button
            {
                Text = "配置功能值",
                Dock = DockStyle.Top,
                Enabled = true // 初始状态可点击
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
                    MessageBox.Show("请填写所有输入字段，并确保输入有效数字。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                    return;
                }

                try
                {
                    isSending = true;
                    button.Text = "Stop";
                    configButton.Enabled = false; // 发送期间禁用配置按钮
                    cts = new CancellationTokenSource();

                    // 启动 MQTT 消息发送
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发送消息失败：\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                }
            }
            else
            {
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
                configButton.Enabled = true; // 停止发送后启用配置按钮
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
            configButton.Enabled = true; // 确保发生错误时重新启用配置按钮
        }

        private void OpenConfigWindow(object sender, EventArgs e)
        {
            ConfigForm configForm = new ConfigForm(configuredValues);
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                configuredValues = configForm.GetConfiguredValues();
                mqttManager.SetConfiguredValues(configuredValues); // 确保 `m-v` 值传递给 `mqttManager`
            }
        }
    }
}