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
            form.ClientSize = new Size(400, 600);
            form.BackColor = Color.White;

            // 📌 使用 FlowLayoutPanel 作为主布局容器
            FlowLayoutPanel mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown, // 控件按顺序垂直排列
                AutoSize = true,
                WrapContents = false, // 禁止换行，确保垂直排列
                Padding = new Padding(20)
            };

            // 📌 创建输入框布局
            TableLayoutPanel layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 8,
                AutoSize = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 📌 添加标签和输入框
            var labelBroker = new Label { Text = "Broker IP:", AutoSize = true };
            var labelPort = new Label { Text = "Port:", AutoSize = true };
            var labelKeepalive = new Label { Text = "Keepalive (sec):", AutoSize = true };
            var labelTopic = new Label { Text = "Topic:", AutoSize = true };
            var labelInterval = new Label { Text = "Interval (ms):", AutoSize = true };

            var textBoxBroker = new TextBox { Width = 200 };
            var textBoxPort = new TextBox { Width = 200, Text = "1883" };
            var textBoxKeepalive = new TextBox { Width = 200, Text = "60" };
            var textBoxTopic = new TextBox { Width = 200 };
            var textBoxInterval = new TextBox { Width = 200, Text = "60000" };
            var labelUsername = new Label { Text = "Username:", AutoSize = true };
            var labelPassword = new Label { Text = "Password:", AutoSize = true };

            var textBoxUsername = new TextBox { Width = 200 };
            var textBoxPassword = new TextBox { Width = 200, UseSystemPasswordChar = true };


            // 📌 添加输入框和标签到 layout
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
            layout.Controls.Add(labelUsername, 0, 5);
            layout.Controls.Add(textBoxUsername, 1, 5);
            layout.Controls.Add(labelPassword, 0, 6);
            layout.Controls.Add(textBoxPassword, 1, 6);


            var retainCheckBox = new CheckBox
            {
                Text = "Retain Message",
                AutoSize = true,
                Checked = false
            };

            // 📌 创建 "发送" 按钮
            sendButton = new Button
            {
                Name = "buttonSend",
                Text = "Send",
                Width = 360,
                Height = 40
            };

            sendButton.Click += async (sender, e) =>
                await sendClickHandler(
                    sendButton,
                    textBoxBroker.Text,
                    textBoxPort.Text,
                    textBoxKeepalive.Text,
                    textBoxTopic.Text,
                    textBoxInterval.Text,
                    retainCheckBox.Checked,
                    textBoxUsername.Text,
                    textBoxPassword.Text
                );


            // 📌 创建 "配置功能值" 按钮
            configButton = new Button
            {
                Text = "配置功能值",
                Width = 360,
                Height = 40
            };
            configButton.Click += configClickHandler;

            // 📌 创建 "使用说明" 按钮（放在 "配置功能值" 下面）
            Button instructionButton = new Button
            {
                Text = "使用说明",
                Width = 360,
                Height = 40
            };
            instructionButton.Click += (sender, e) =>
            {
                InstructionForm instructionForm = new InstructionForm();
                instructionForm.Show();
            };

            // 📌 添加顶部的提示信息
            var labelFileHint = new Label
            {
                Text = "请确保软件同一目录下存在 sim_message.txt 文件",
                AutoSize = true,
                ForeColor = Color.DarkRed
            };

            var labelConfigHint = new Label
            {
                Text = "保存修改的配置会使原 sim_message.txt 内容变更",
                AutoSize = true,
                ForeColor = Color.DarkRed
            };

            var labelMinimizeHint = new Label
            {
                Text = "点击最小化可将软件置入托盘",
                AutoSize = true,
                ForeColor = Color.Gray
            };

            // 📌 添加路径标签
            Label pathLabel = CreatePathLabel();

            // 📌 监听窗口大小变化，确保 Path 标签自适应
            form.Resize += (sender, e) =>
            {
                pathLabel.MaximumSize = new Size(form.ClientSize.Width - 20, 0);
            };

            // 📌 依次添加控件，确保顺序正确
            mainPanel.Controls.Add(sendButton);
            mainPanel.Controls.Add(layout);
            mainPanel.Controls.Add(retainCheckBox);
            mainPanel.Controls.Add(configButton);
            mainPanel.Controls.Add(instructionButton); // 确保 "使用说明" 按钮 在 "配置功能值" 按钮下方
            mainPanel.Controls.Add(labelMinimizeHint);
            mainPanel.Controls.Add(labelConfigHint);
            mainPanel.Controls.Add(labelFileHint);
            mainPanel.Controls.Add(pathLabel);

            // 📌 添加到窗体
            form.Controls.Add(mainPanel);
        }

        /// <summary>
        /// 设置 "配置功能项" 按钮的可用性
        /// </summary>
        public static void SetConfigButtonEnabled(bool enabled)
        {
            if (configButton?.IsDisposed == false)
            {
                configButton.Invoke((MethodInvoker)(() => configButton.Enabled = enabled));
            }
        }

        /// <summary>
        /// 创建路径标签（带点击复制功能）
        /// </summary>
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
                MaximumSize = new Size(400, 0), // 限制最大宽度
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
