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
            if (!isSending) // ��ǰδ���ͣ������������������
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
                    // �ȼ�� `sim_message.txt` �Ƿ�Ϊ��
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
                            return; // �û�ѡ�񡰷񡱣�ȡ������
                        }
                    }

                    string message = await MessageFileHandler.ReadMessageAsync(); // ��ȡ��Ϣ
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
                cts?.Cancel();
                cts = null;
                isSending = false;
                button.Text = "Send";
            }
        }

        public void ResetButtonState(Button button)
        {
            if (button?.IsDisposed == true) return; // ��ť�Ѿ������٣�ֱ�ӷ���

            if (button?.InvokeRequired == true)
            {
                button.Invoke((MethodInvoker)(() => ResetButtonState(button)));
                return;
            }

            button.Text = "Send";
            isSending = false; // ȷ��״̬ͬ��Ϊδ����
            cts = null;
        }

    }
}
