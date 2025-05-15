using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class UIManager
    {
        private static Button configButton;
        private static Button sendButton;

        public static void SetupControl(MainForm form, EventHandler configClickHandler, Func<Button, string, string, string, string, string, bool, string, string, Task> sendClickHandler)
        {
            form.Text = "MQTT Message Sender";
            form.ClientSize = new Size(800, 800);
            form.BackColor = Color.White;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };

            TabPage tabSingle = new TabPage("单线程") { Padding = new Padding(10) };
            tabSingle.Controls.Add(BuildSingleThreadPanel(configClickHandler, sendClickHandler));

            TabPage tabMulti = new TabPage("多线程") { Padding = new Padding(10) };
            tabMulti.Controls.Add(MultiThreadPanelBuilder.BuildCsvBased()); // 新增：基于CSV文件的界面构建

            tabControl.TabPages.Add(tabSingle);
            tabControl.TabPages.Add(tabMulti);

            form.Controls.Add(tabControl);
        }

        private static FlowLayoutPanel BuildSingleThreadPanel(EventHandler configClickHandler, Func<Button, string, string, string, string, string, bool, string, string, Task> sendClickHandler)
        {
            var font = new Font("Segoe UI", 10.5F);

            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(20),
                WrapContents = false
            };

            var txtBroker = new TextBox { Width = 360, Font = font };
            var txtPort = new TextBox { Width = 360, Font = font, Text = "1883" };
            var txtKeepAlive = new TextBox { Width = 360, Font = font, Text = "60" };
            var txtTopic = new TextBox { Width = 360, Font = font };
            var txtInterval = new TextBox { Width = 360, Font = font, Text = "60000" };
            var txtUsername = new TextBox { Width = 360, Font = font };
            var txtPassword = new TextBox { Width = 360, Font = font, UseSystemPasswordChar = true };

            var chkRetain = new CheckBox { Text = "Retain Message", AutoSize = true, Font = font };

            sendButton = new Button { Text = "发送", Width = 300, Height = 40, Font = font };
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
                    txtPassword.Text);
            };

            configButton = new Button { Text = "配置功能值", Width = 300, Height = 40, Font = font };
            configButton.Click += configClickHandler;

            var instructionButton = new Button { Text = "使用说明", Width = 300, Height = 40, Font = font };
            instructionButton.Click += (s, e) => new InstructionForm().Show();

            void AddRow(string labelText, Control input)
            {
                var rowPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    Padding = new Padding(2),
                    Margin = new Padding(1)
                };
                rowPanel.Controls.Add(new Label { Text = labelText, Width = 120, Font = font, TextAlign = ContentAlignment.MiddleLeft, Height = 28 });
                rowPanel.Controls.Add(input);
                panel.Controls.Add(rowPanel);
            }

            AddRow("Broker IP:", txtBroker);
            AddRow("Port:", txtPort);
            AddRow("KeepAlive (sec):", txtKeepAlive);
            AddRow("Topic:", txtTopic);
            AddRow("Interval (ms):", txtInterval);
            AddRow("Username:", txtUsername);
            AddRow("Password:", txtPassword);
            AddRow("Retain:", chkRetain);

            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(2), AutoSize = true };
            btnPanel.Controls.Add(sendButton);
            panel.Controls.Add(btnPanel);

            panel.Controls.Add(configButton);
            panel.Controls.Add(instructionButton);

            var labelFileHint = new Label
            {
                Text = "请确保软件同一目录下存在 sim_message.txt 文件",
                AutoSize = true,
                ForeColor = Color.DarkRed,
                Font = font
            };
            var labelConfigHint = new Label
            {
                Text = "保存修改的配置会使原 sim_message.txt 内容变更",
                AutoSize = true,
                ForeColor = Color.DarkRed,
                Font = font
            };
            var labelMinimizeHint = new Label
            {
                Text = "点击最小化可将软件置入托盘",
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = font
            };
            Label pathLabel = CreatePathLabel();
            panel.Resize += (sender, e) =>
            {
                pathLabel.MaximumSize = new Size(panel.ClientSize.Width - 20, 0);
            };

            panel.Controls.Add(labelMinimizeHint);
            panel.Controls.Add(labelConfigHint);
            panel.Controls.Add(labelFileHint);
            panel.Controls.Add(pathLabel);

            return panel;
        }

        public static void SetConfigButtonEnabled(bool enabled)
        {
            if (configButton?.IsDisposed == false)
            {
                configButton.Invoke((MethodInvoker)(() => configButton.Enabled = enabled));
            }
        }

        private static Label CreatePathLabel()
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            Label pathLabel = new Label
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10),
                MaximumSize = new Size(600, 0),
                AutoEllipsis = true,
                Text = $"Software Path: {currentPath}"
            };

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

            return pathLabel;
        }
    }
}
