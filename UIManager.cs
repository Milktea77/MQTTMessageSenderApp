using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class UIManager
    {
        public static void SetupControl(MainForm form)
        {
            form.Text = "MQTT Message Sender";
            form.ClientSize = new Size(400, 600);
            form.BackColor = Color.White;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 6,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 📌 添加标签和输入框
            var labelBroker = new Label { Text = "Broker IP:", Dock = DockStyle.Fill };
            var labelPort = new Label { Text = "Port:", Dock = DockStyle.Fill };
            var labelKeepalive = new Label { Text = "Keepalive (sec):", Dock = DockStyle.Fill };
            var labelTopic = new Label { Text = "Topic:", Dock = DockStyle.Fill };
            var labelInterval = new Label { Text = "Interval (ms):", Dock = DockStyle.Fill };

            var textBoxBroker = new TextBox { Dock = DockStyle.Fill };
            var textBoxPort = new TextBox { Dock = DockStyle.Fill, Text = "1883" };
            var textBoxKeepalive = new TextBox { Dock = DockStyle.Fill, Text = "60" }; // ✅ KeepAlive 还原
            var textBoxTopic = new TextBox { Dock = DockStyle.Fill };
            var textBoxInterval = new TextBox { Dock = DockStyle.Fill, Text = "60000" };

            var buttonSend = new Button
            {
                Name = "buttonSend",
                Text = "Send",
                Dock = DockStyle.Top,
                Height = 40
            };

            buttonSend.Click += async (sender, e) =>
                await form.ToggleSendAsync(buttonSend, textBoxBroker.Text, textBoxPort.Text, textBoxKeepalive.Text, textBoxTopic.Text, textBoxInterval.Text);

            // 📌 添加 UI 组件到布局
            layout.Controls.Add(labelBroker, 0, 0);
            layout.Controls.Add(textBoxBroker, 1, 0);
            layout.Controls.Add(labelPort, 0, 1);
            layout.Controls.Add(textBoxPort, 1, 1);
            layout.Controls.Add(labelKeepalive, 0, 2); // ✅ KeepAlive 还原
            layout.Controls.Add(textBoxKeepalive, 1, 2);
            layout.Controls.Add(labelTopic, 0, 3);
            layout.Controls.Add(textBoxTopic, 1, 3);
            layout.Controls.Add(labelInterval, 0, 4);
            layout.Controls.Add(textBoxInterval, 1, 4);

            // 📌 添加顶部的提示信息
            var labelFileHint = new Label
            {
                Text = "请确保软件同一目录下存在 sim_message.txt 文件",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Color.DarkRed,
                Padding = new Padding(10, 5, 10, 5)
            };

            var labelMinimizeHint = new Label
            {
                Text = "点击最小化可将软件置入托盘",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Color.Gray,
                Padding = new Padding(10, 0, 10, 5)
            };

            // 📌 添加路径标签
            Label pathLabel = CreatePathLabel();

            // 📌 监听窗口大小变化，确保 Path 标签自适应
            form.Resize += (sender, e) =>
            {
                pathLabel.MaximumSize = new Size(form.ClientSize.Width - 20, 0);
            };

            // 📌 创建主 Panel 以确保布局
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            mainPanel.Controls.Add(labelFileHint);
            mainPanel.Controls.Add(labelMinimizeHint);
            mainPanel.Controls.Add(layout);
            mainPanel.Controls.Add(buttonSend);
            mainPanel.Controls.Add(pathLabel);

            form.Controls.Add(mainPanel);
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
                Dock = DockStyle.Bottom,
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
