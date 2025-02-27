using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using NLog;
using System.Drawing;
using System.Linq;

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
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MainForm(string title)
        {
            InitializeComponent(); // 初始化窗体组件
            setupControl();
            this.Text = title;
            InitializeTrayIcon(); // 初始化托盘图标
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "MQTT Message Sender";
            trayIcon.Icon = SystemIcons.Application; // 设置托盘图标
            trayIcon.Visible = false; // 初始时隐藏

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示", null, OnTrayMenuShowClick);
            trayMenu.Items.Add("退出", null, OnTrayMenuExitClick);
            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.DoubleClick += OnTrayIconDoubleClick;
            this.Resize += OnFormResize;
        }

        private void setupControl()
        {
            this.Text = "MQTT Message Sender";
            this.ClientSize = new System.Drawing.Size(400, 600);
            this.BackColor = Color.White; // 统一背景颜色

            // 使用 TableLayoutPanel 实现整齐的布局
            TableLayoutPanel layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 7,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // 第一列固定宽度
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 第二列占满剩余空间

            // 创建控件
            var labelBroker = new Label { Text = "Broker IP:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var labelPort = new Label { Text = "Port:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var labelKeepalive = new Label { Text = "Keepalive (sec):", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var labelTopic = new Label { Text = "Topic:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var labelInterval = new Label { Text = "Interval (ms):", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };

            var textBoxBroker = new TextBox { Dock = DockStyle.Fill };
            var textBoxPort = new TextBox { Dock = DockStyle.Fill, Text = "1883" };
            var textBoxKeepalive = new TextBox { Dock = DockStyle.Fill, Text = "60" };
            var textBoxTopic = new TextBox { Dock = DockStyle.Fill };
            var textBoxInterval = new TextBox { Dock = DockStyle.Fill, Text = "60000" };

            var buttonSend = new Button
            {
                Name = "buttonSend", // 确保在其他方法中可以找到这个按钮
                Text = "Send",
                Dock = DockStyle.Top,
                Height = 40
            };
            buttonSend.Click += async (sender, e) =>
                await ToggleSendAsync(buttonSend, textBoxBroker.Text, textBoxPort.Text, textBoxKeepalive.Text, textBoxTopic.Text, textBoxInterval.Text);

            // 将控件添加到布局
            layout.Controls.Add(labelBroker, 0, 0);
            layout.Controls.Add(textBoxBroker, 1, 0);
            layout.Controls.Add(labelPort, 0, 1);
            layout.Controls.Add(textBoxPort, 1, 1);
            layout.Controls.Add(labelKeepalive, 0, 2);
            layout.Controls.Add(textBoxKeepalive, 1, 2);
            layout.Controls.Add(labelTopic, 0, 3);
            layout.Controls.Add(textBoxTopic, 1, 3);
            layout.Controls.Add(labelInterval, 0, 4);
            layout.Controls.Add(textBoxInterval, 1, 4);

            // 添加文件提示信息
            var labelFileHint = new Label
            {
                Text = "请确保软件同一目录下存在 sim_message.txt 文件",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Color.DarkRed,
                Padding = new Padding(10, 5, 10, 5)
            };

            // 添加最小化提示
            var labelMinimizeHint = new Label
            {
                Text = "点击最小化，软件将被置于托盘",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Color.Gray,
                Padding = new Padding(10, 0, 10, 5)
            };

            // 添加路径标签
            Label pathLabel = new Label
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                ForeColor = Color.Blue,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10),
                MaximumSize = new System.Drawing.Size(this.ClientSize.Width - 20, 0),
                AutoEllipsis = true
            };

            string currentPath = Directory.GetCurrentDirectory();
            pathLabel.Text = $"Software Path: {currentPath}";

            ToolTip pathToolTip = new ToolTip();
            pathToolTip.SetToolTip(pathLabel, currentPath);

            pathLabel.Click += (sender, e) =>
            {
                try
                {
                    Clipboard.SetText(currentPath);
                    pathToolTip.Show("路径已复制到剪贴板", pathLabel, pathLabel.Width / 2, -pathLabel.Height, 1000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制路径失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // 监听窗口大小变化，确保 Path 标签自适应
            this.Resize += (sender, e) =>
            {
                pathLabel.MaximumSize = new System.Drawing.Size(this.ClientSize.Width - 20, 0);
            };

            // 创建主布局 Panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            mainPanel.Controls.Add(layout);
            mainPanel.Controls.Add(buttonSend);
            mainPanel.Controls.Add(labelFileHint);
            mainPanel.Controls.Add(labelMinimizeHint);
            mainPanel.Controls.Add(pathLabel);

            // 确保路径始终在最底部
            this.Controls.Add(pathLabel);
            this.Controls.Add(mainPanel);
        }


        // 异常后重置按钮
        // 重置按钮状态的方法
        private void ResetButtonState(Button button)
        {
            if (button?.IsDisposed == true) return; // 按钮已经被销毁，直接返回

            if (button?.InvokeRequired == true)
            {
                button.Invoke((MethodInvoker)(() => ResetButtonState(button)));
                return;
            }

            if (button != null)
            {
                button.Text = "Send";
            }

            isSending = false; // 确保状态同步为未发送
            cts = null;
        }


        private bool isSending = false;

        private async Task ToggleSendAsync(Button button, string broker, string portStr, string keepaliveStr, string topic, string intervalStr)
        {
            if (!isSending) // 当前未发送，点击后启动发送任务
            {
                if (!int.TryParse(portStr, out int port) ||
                    !int.TryParse(keepaliveStr, out int keepalive) ||
                    !int.TryParse(intervalStr, out int interval) ||
                    string.IsNullOrWhiteSpace(broker) ||
                    string.IsNullOrWhiteSpace(topic))
                {
                    MessageBox.Show("请填写所有输入字段，并确保输入有效数字。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (button != null)
                    {
                        ResetButtonState(button);
                    }
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

                    if (button != null)
                    {
                        button.Text = "Stop";
                    }

                    isSending = true; // 更新状态为正在发送
                    cts = new CancellationTokenSource();
                    _ = SendMessagesAsync(topic, interval, cts.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接失败： {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    if (button != null)
                    {
                        ResetButtonState(button);
                    }
                }
            }
            else // 当前正在发送，点击后停止发送任务
            {
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }

                if (mqttClient != null && mqttClient.IsConnected)
                {
                    await mqttClient.DisconnectAsync();
                }

                if (button != null)
                {
                    ResetButtonState(button);
                }
                isSending = false; // 更新状态为未发送
            }
        }



        private async Task SendMessagesAsync(string topic, int interval, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!File.Exists(messageFile))
                    {
                        MessageBox.Show($"消息文件 '{messageFile}' 在同目录中不存在！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        var stopButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "buttonSend");
                        ResetButtonState(stopButton);
                        return;
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

                        var stopButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "buttonSend");
                        ResetButtonState(stopButton);
                        return;
                    }

                    Logger.Info($"Message sent to topic '{topic}' at {DateTime.Now}");
                    await Task.Delay(interval, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送消息失败或被手动中止: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                var stopButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "buttonSend");
                ResetButtonState(stopButton);
            }
        }


        private void OnTrayMenuShowClick(object sender, EventArgs e)
        {
            ShowFormFromTray();
        }

        private void OnTrayMenuExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            ShowFormFromTray();
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                HideFormToTray();
            }
        }

        private void HideFormToTray()
        {
            this.Hide();
            trayIcon.Visible = true;
        }

        private void ShowFormFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
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