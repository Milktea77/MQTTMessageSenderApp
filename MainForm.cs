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

        public MainForm(string title)
        {
            InitializeComponent();
            UIManager.SetupControl(this, OpenConfigWindow, ToggleSendAsync);
            this.Text = title;

            mqttManager = new MqttClientManager();
            trayManager = new TrayManager(this);
        }

        public async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr, bool retain)
        {
            if (!isSending)
            {
                // 检查输入是否合法
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
                    UIManager.SetConfigButtonEnabled(false); // 发送期间禁用配置按钮
                    trayManager.UpdateTopic(topic); // 更新托盘目标Topic
                    trayManager.SetRunningStatus(true); // 设置托盘状态为"运行中"

                    cts?.Cancel(); // 取消之前可能未释放的 `CancellationTokenSource`
                    cts = new CancellationTokenSource();

                    // 启动 MQTT 消息发送
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, retain, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 用户主动取消，不弹出错误
                    MessageBox.Show("消息发送已取消！", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"发送消息失败：\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 发生异常或取消后确保按钮恢复状态
                    ResetButtonState(button);
                    trayManager.SetRunningStatus(false); // 设置托盘状态为"未运行"
                }
            }
            else
            {
                // 取消发送
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
                UIManager.SetConfigButtonEnabled(true); // 停止发送后启用配置按钮
                trayManager.SetRunningStatus(false); // 设置托盘状态为"未运行"
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
            UIManager.SetConfigButtonEnabled(true); // 确保发生错误时重新启用配置按钮
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
