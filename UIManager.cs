using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class UIManager
    {
        private static Button configButton;
        private static Button sendButton;

        // 统一配色方案
        private static readonly Color PrimaryColor = Color.FromArgb(59, 130, 246);     // 主色调 - 蓝色
        private static readonly Color SecondaryColor = Color.FromArgb(107, 33, 168);   // 次要色 - 深蓝
        private static readonly Color SuccessColor = Color.FromArgb(16, 185, 129);      // 成功色 - 绿色
        private static readonly Color WarningColor = Color.FromArgb(245, 158, 11);      // 警告色 - 橙色
        private static readonly Color DangerColor = Color.FromArgb(239, 68, 68);       // 危险色 - 红色
        private static readonly Color LightGray = Color.FromArgb(248, 250, 252);      // 浅灰背景
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);     // 边框颜色
        private static readonly Color TextColor = Color.FromArgb(51, 65, 85);         // 文字颜色
        private static readonly Color TextMuted = Color.FromArgb(107, 114, 128);     // 弱化文字
        private static readonly Color White = Color.FromArgb(255, 255, 255);           // 白色

        public static void SetupControl(MainForm form, EventHandler configClickHandler, Func<Button, string, string, string, string, string, bool, string, string, bool, bool, Task> sendClickHandler)
        {
            form.Text = "MQTT Message Sender v0.5.1.0";
            form.ClientSize = new Size(850, 720);
            form.BackColor = LightGray;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Font = new Font("Segoe UI", 9F);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                BackColor = LightGray,
                Padding = new Point(0, 0)
            };

            TabPage tabSingle = new TabPage("单线程")
            {
                BackColor = LightGray,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None
            };
            tabSingle.Controls.Add(BuildSingleThreadPanel(configClickHandler, sendClickHandler));

            TabPage tabMulti = new TabPage("多线程")
            {
                BackColor = LightGray,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None
            };
            tabMulti.Controls.Add(MultiThreadPanelBuilder.BuildCsvBased());

            tabControl.TabPages.Add(tabSingle);
            tabControl.TabPages.Add(tabMulti);

            form.Controls.Add(tabControl);
        }

        private static FlowLayoutPanel BuildSingleThreadPanel(EventHandler configClickHandler, Func<Button, string, string, string, string, string, bool, string, string, bool, bool, Task> sendClickHandler)
        {
            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(20, 20, 20, 20),
                WrapContents = false,
                BackColor = LightGray
            };

            var txtBroker = CreateStyledTextBox("", "输入Broker地址", false);
            var txtPort = CreateStyledTextBox("1883", "输入端口号", false);
            var txtKeepAlive = CreateStyledTextBox("60", "输入保活时间(秒)", false);
            var txtTopic = CreateStyledTextBox("", "输入Topic", false);
            var txtInterval = CreateStyledTextBox("60000", "输入发送间隔(毫秒)", false);
            var txtUsername = CreateStyledTextBox("", "输入用户名(可选)", false);
            var txtPassword = CreateStyledTextBox("", "输入密码(可选)", true);

            var chkRetain = CreateStyledCheckBox("启用Retain消息");

            var chkUseMqtts = CreateStyledCheckBox("使用MQTTS (SSL/TLS)", false);
            var chkSslSecure = CreateStyledCheckBox("启用SSL证书验证", false, true);

            // MQTTS联动
            chkUseMqtts.CheckedChanged += (s, e) =>
            {
                chkSslSecure.Enabled = chkUseMqtts.Checked;
                if (chkUseMqtts.Checked)
                {
                    txtPort.Text = "8883";
                }
                else
                {
                    txtPort.Text = "1883";
                }
            };

            // 创建按钮
            sendButton = CreateStyledButton("发送消息", PrimaryColor);
            sendButton.Click += async (sender, e) =>
            {
                await sendClickHandler(
                    sendButton,
                    txtBroker.Text,
                    txtPort.Text,
                    txtKeepAlive.Text,
                    txtTopic.Text,
                    txtInterval.Text,
                    chkRetain.Checked,
                    txtUsername.Text,
                    txtPassword.Text,
                    chkUseMqtts.Checked,
                    chkSslSecure.Checked);
            };

            configButton = CreateStyledButton("配置功能值", SecondaryColor);
            configButton.Click += configClickHandler;

            var instructionButton = CreateStyledButton("使用说明", TextMuted);
            instructionButton.Click += (s, e) => new InstructionForm().Show();

            // 添加控制项
            AddControlRow(panel, "Broker", txtBroker);
            AddControlRow(panel, "端口", txtPort);
            AddControlRow(panel, "保活时间", txtKeepAlive);
            AddControlRow(panel, "Topic", txtTopic);
            AddControlRow(panel, "发送间隔", txtInterval);
            AddControlRow(panel, "用户名", txtUsername);
            AddControlRow(panel, "密码", txtPassword);
            AddControlRow(panel, "消息设置", chkRetain);
            AddControlRow(panel, "MQTTS设置", chkUseMqtts);
            AddControlRow(panel, "SSL验证", chkSslSecure);

            // 添加按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 15),
                BackColor = LightGray
            };
            buttonPanel.Controls.Add(sendButton);
            buttonPanel.Controls.Add(configButton);
            buttonPanel.Controls.Add(instructionButton);
            panel.Controls.Add(buttonPanel);

            // 添加提示信息
            AddHint(panel, "💡 提示: 确保软件同一目录下存在 sim_message.txt 文件");
            AddHint(panel, "⚠️ 警告: 保存配置会修改原文件内容");
            AddHint(panel, "ℹ️ 说明: 点击最小化可将软件置入托盘");
            AddHint(panel, "📂 当前路径: " + AppDomain.CurrentDomain.BaseDirectory);

            return panel;
        }

        private static void AddControlRow(FlowLayoutPanel panel, string labelText, Control control)
        {
            var rowPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 5, 0, 5),
                BackColor = LightGray
            };

            var label = new Label
            {
                Text = labelText + ":",
                Width = 100,
                Height = 32,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextColor
            };

            rowPanel.Controls.Add(label);
            rowPanel.Controls.Add(control);
            panel.Controls.Add(rowPanel);
        }

        private static TextBox CreateStyledTextBox(string defaultText, string placeholder, bool isPassword)
        {
            return new TextBox
            {
                Text = defaultText,
                Width = 300,
                Height = 32,
                Font = new Font("Segoe UI", 9F),
                BackColor = White,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                UseSystemPasswordChar = isPassword
            };
        }

        private static CheckBox CreateStyledCheckBox(string text, bool isChecked = false, bool enabled = true)
        {
            return new CheckBox
            {
                Text = text,
                Checked = isChecked,
                Enabled = enabled,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextColor,
                Padding = new Padding(5, 2, 5, 2),
                Cursor = Cursors.Hand
            };
        }

        private static Button CreateStyledButton(string text, Color bgColor)
        {
            var button = new Button
            {
                Text = text,
                Width = 120,
                Height = 36,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = bgColor,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ChangeColorBrightness(bgColor, -20);
            button.FlatAppearance.MouseDownBackColor = ChangeColorBrightness(bgColor, -40);

            return button;
        }

        private static void AddHint(FlowLayoutPanel panel, string text)
        {
            var hint = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = TextMuted,
                Margin = new Padding(0, 3, 0, 3)
            };
            panel.Controls.Add(hint);
        }

        private static Color ChangeColorBrightness(Color color, int brightnessChange)
        {
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, color.R + brightnessChange)),
                Math.Max(0, Math.Min(255, color.G + brightnessChange)),
                Math.Max(0, Math.Min(255, color.B + brightnessChange))
            );
        }

        public static void SetConfigButtonEnabled(bool enabled)
        {
            if (configButton?.IsDisposed == false)
            {
                configButton.Invoke((MethodInvoker)(() =>
                {
                    configButton.Enabled = enabled;
                    configButton.BackColor = enabled ? SecondaryColor : Color.LightGray;
                }));
            }
        }
    }
}
