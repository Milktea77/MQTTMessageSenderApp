using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public static class MultiThreadPanelBuilder
    {
        // 统一配色方案
        private static readonly Color PrimaryColor = Color.FromArgb(59, 130, 246);
        private static readonly Color SecondaryColor = Color.FromArgb(107, 33, 168);
        private static readonly Color SuccessColor = Color.FromArgb(16, 185, 129);
        private static readonly Color DangerColor = Color.FromArgb(239, 68, 68);
        private static readonly Color LightGray = Color.FromArgb(248, 250, 252);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color TextColor = Color.FromArgb(51, 65, 85);
        private static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
        private static readonly Color White = Color.FromArgb(255, 255, 255);

        public static Panel BuildCsvBased()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 20, 20, 20),
                BackColor = LightGray
            };

            var txtBroker = CreateStyledTextBox("", "输入Broker地址");
            var txtPort = CreateStyledTextBox("1883", "输入端口号");
            var txtKeepAlive = CreateStyledTextBox("60", "输入保活时间");
            var txtInterval = CreateStyledTextBox("10000", "输入发送间隔");
            var chkRetain = CreateStyledCheckBox("启用Retain消息");

            var txtCsvFile = CreateStyledTextBox("", "选择CSV文件");
            txtCsvFile.ReadOnly = true;
            txtCsvFile.BackColor = LightGray;

            var btnCsvFile = CreateStyledButton("选择文件", SecondaryColor);
            btnCsvFile.Click += (s, e) =>
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtCsvFile.Text = ofd.FileName;
                }
            };

            var txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 150,
                Width = 700,
                Font = new Font("Consolas", 8F),
                BackColor = LightGray,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            MultiThreadTaskManager.BindLogBox(txtLog);

            // 创建按钮
            var btnStart = CreateStyledButton("开始多线程发送", SuccessColor);
            var btnStop = CreateStyledButton("停止发送", DangerColor);

            btnStart.Click += (s, e) =>
            {
                if (!int.TryParse(txtPort.Text, out int port) ||
                    !int.TryParse(txtKeepAlive.Text, out int keepalive) ||
                    !int.TryParse(txtInterval.Text, out int interval) ||
                    string.IsNullOrWhiteSpace(txtBroker.Text) ||
                    string.IsNullOrWhiteSpace(txtCsvFile.Text) ||
                    !File.Exists(txtCsvFile.Text))
                {
                    MessageBox.Show("请填写正确的连接信息，并选择有效的 CSV 文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    var lines = File.ReadAllLines(txtCsvFile.Text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    if (lines.Count < 1)
                    {
                        MessageBox.Show("CSV 文件无有效数据。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var topics = new List<string>();
                    var usernames = new List<string>();
                    var passwords = new List<string>();
                    var deviceIdList = new List<List<string>>();

                    // 清除状态显示
                    ClearStatusPanel();

                    var topicUserMap = new Dictionary<string, string>();
                    var topicPassMap = new Dictionary<string, string>();
                    var topicDeviceMap = new Dictionary<string, List<string>>();

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var cols = lines[i].Split(',');
                        if (cols.Length < 1) continue;

                        var topic = cols[0].Trim();
                        var user = cols.Length > 1 ? cols[1].Trim() : "";
                        var pass = cols.Length > 2 ? cols[2].Trim() : "";
                        var device = cols.Length > 3 ? cols[3].Trim() : "";

                        if (!topicDeviceMap.ContainsKey(topic))
                        {
                            topicDeviceMap[topic] = new List<string>();
                            topicUserMap[topic] = user;
                            topicPassMap[topic] = pass;
                        }

                        if (!string.IsNullOrWhiteSpace(device))
                            topicDeviceMap[topic].Add(device);
                    }

                    foreach (var kvp in topicDeviceMap)
                    {
                        topics.Add(kvp.Key);
                        usernames.Add(topicUserMap[kvp.Key]);
                        passwords.Add(topicPassMap[kvp.Key]);
                        deviceIdList.Add(kvp.Value);

                        // 添加状态标签
                        var statusLabel = CreateStatusLabel($"[{kvp.Key}] 状态：等待启动...");
                        panel.Controls.Add(statusLabel);
                        MultiThreadTaskManager.RegisterThreadStatus(kvp.Key, statusLabel);
                    }

                    if (topics.Count == 0)
                    {
                        MessageBox.Show("CSV 中未找到有效 Topic 行。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    MultiThreadTaskManager.StartAll(
                        txtBroker.Text.Trim(),
                        port,
                        keepalive,
                        interval,
                        chkRetain.Checked,
                        topics,
                        usernames,
                        passwords,
                        deviceIdList
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取 CSV 文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnStop.Click += (s, e) => MultiThreadTaskManager.StopAll();

            // 添加控制项
            AddControlRow(panel, "Broker", txtBroker);
            AddControlRow(panel, "端口", txtPort);
            AddControlRow(panel, "保活时间", txtKeepAlive);
            AddControlRow(panel, "发送间隔", txtInterval);
            AddControlRow(panel, "消息设置", chkRetain);

            // CSV文件选择
            var csvRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 5, 0, 5),
                BackColor = LightGray
            };

            var csvLabel = new Label
            {
                Text = "CSV文件:",
                Width = 100,
                Height = 32,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextColor
            };

            csvRow.Controls.Add(csvLabel);
            csvRow.Controls.Add(txtCsvFile);
            csvRow.Controls.Add(btnCsvFile);
            panel.Controls.Add(csvRow);

            // 添加按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 15),
                BackColor = LightGray
            };
            buttonPanel.Controls.Add(btnStart);
            buttonPanel.Controls.Add(btnStop);
            panel.Controls.Add(buttonPanel);

            // 分隔线
            var separator = CreateSeparator();
            panel.Controls.Add(separator);

            // 状态标题
            var statusTitle = new Label
            {
                Text = "线程状态：",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TextColor,
                Margin = new Padding(0, 10, 0, 5)
            };
            panel.Controls.Add(statusTitle);

            // 日志区域
            var logLabel = new Label
            {
                Text = "运行日志：",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TextColor,
                Margin = new Padding(0, 10, 0, 5)
            };
            panel.Controls.Add(logLabel);
            panel.Controls.Add(txtLog);

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

        private static TextBox CreateStyledTextBox(string defaultText, string placeholder)
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
                PlaceholderText = placeholder
            };
        }

        private static CheckBox CreateStyledCheckBox(string text, bool isChecked = false)
        {
            return new CheckBox
            {
                Text = text,
                Checked = isChecked,
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
                Width = 140,
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

        private static Label CreateStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Width = 700,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                Margin = new Padding(0, 3, 0, 3)
            };
        }

        private static Panel CreateSeparator()
        {
            return new Panel
            {
                Height = 1,
                Width = 700,
                BackColor = BorderColor,
                Margin = new Padding(0, 15, 0, 15)
            };
        }

        private static Color ChangeColorBrightness(Color color, int brightnessChange)
        {
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, color.R + brightnessChange)),
                Math.Max(0, Math.Min(255, color.G + brightnessChange)),
                Math.Max(0, Math.Min(255, color.B + brightnessChange))
            );
        }

        private static void ClearStatusPanel()
        {
            // 这个方法保持空实现，由调用者管理状态标签
        }
    }
}
