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

        public async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr, bool retain, string username, string password, bool useMqtts, bool sslSecure)
        {
            if (!isSending)
            {
                // 检查输入是否合法
                if (!int.TryParse(portStr, out int port) ||
                    !int.TryParse(keepaliveStr, out int keepalive) ||
                    !int.TryParse(intervalStr, out int interval) ||
                    string.IsNullOrWhiteSpace(broker) || string.IsNullOrWhiteSpace(topic))
                {
                    MessageBox.Show("请填写完整的连接参数，确保输入有效的数字。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetButtonState(button);
                    return;
                }

                try
                {
                    isSending = true;
                    button.Text = "Stop";
                    UIManager.SetConfigButtonEnabled(false); // 禁用配置按钮
                    trayManager.UpdateTopic(topic); // 更新托盘的Topic
                    trayManager.SetRunningStatus(true); // 更新运行状态为"运行中"

                    cts?.Cancel(); // 取消之前未释放的 `CancellationTokenSource`
                    cts = new CancellationTokenSource();

                    // 调用 MQTT 消息发送
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, retain, username, password, useMqtts, sslSecure, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 用户取消发送操作时的提示
                    MessageBox.Show("消息发送已取消", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接消息失败！\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 无论异常或取消都确保按钮恢复状态
                    ResetButtonState(button);
                    trayManager.SetRunningStatus(false); // 更新运行状态为"未运行"
                }
            }
            else
            {
                // 取消发送
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
                UIManager.SetConfigButtonEnabled(true); // 停止发送后重新启用配置按钮
                trayManager.SetRunningStatus(false); // 更新运行状态为"未运行"
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
            UIManager.SetConfigButtonEnabled(true); // 确保异常处理时重新启用配置按钮
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
