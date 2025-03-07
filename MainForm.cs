using System;
using System.Drawing;
using System.IO;
using System.Linq;
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

        public MainForm(string title)
        {
            InitializeComponent();
            UIManager.SetupControl(this);
            this.Text = title;

            mqttManager = new MqttClientManager();
            trayManager = new TrayManager(this);
        }

        public async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr)
        {
            if (!isSending) // 当前未发送，点击后启动发送任务
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
                    cts = new CancellationTokenSource();
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接失败： {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                }
            }
            else // 停止任务
            {
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
            }
        }

        public void ResetButtonState(Button button)
        {
            if (button?.IsDisposed == true) return; // 按钮已经被销毁，直接返回

            if (button?.InvokeRequired == true)
            {
                button.Invoke((MethodInvoker)(() => ResetButtonState(button)));
                return;
            }

            button.Text = "Send";
            isSending = false; // 确保状态同步为未发送
            cts = null;
        }

    }
}
