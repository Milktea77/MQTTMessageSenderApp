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
                // ��������Ƿ�Ϸ�
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
                    isSending = true;
                    button.Text = "Stop";
                    UIManager.SetConfigButtonEnabled(false); // �����ڼ�������ð�ť
                    trayManager.UpdateTopic(topic); // ��������Ŀ��Topic
                    trayManager.SetRunningStatus(true); // ��������״̬Ϊ"������"

                    cts?.Cancel(); // ȡ��֮ǰ����δ�ͷŵ� `CancellationTokenSource`
                    cts = new CancellationTokenSource();

                    // ���� MQTT ��Ϣ����
                    await mqttManager.StartSendingAsync(broker, portStr, keepaliveStr, topic, intervalStr, retain, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // �û�����ȡ��������������
                    MessageBox.Show("��Ϣ������ȡ����", "֪ͨ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������Ϣʧ�ܣ�\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // �����쳣��ȡ����ȷ����ť�ָ�״̬
                    ResetButtonState(button);
                    trayManager.SetRunningStatus(false); // ��������״̬Ϊ"δ����"
                }
            }
            else
            {
                // ȡ������
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
                UIManager.SetConfigButtonEnabled(true); // ֹͣ���ͺ��������ð�ť
                trayManager.SetRunningStatus(false); // ��������״̬Ϊ"δ����"
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
            UIManager.SetConfigButtonEnabled(true); // ȷ����������ʱ�����������ð�ť
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
