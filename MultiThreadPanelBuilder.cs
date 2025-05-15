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
        public static Panel BuildCsvBased()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20)
            };

            Font font = new Font("Segoe UI", 10.5F);

            var txtBroker = new TextBox { Width = 360, Font = font };
            var txtPort = new TextBox { Width = 360, Font = font, Text = "1883" };
            var txtKeepAlive = new TextBox { Width = 360, Font = font, Text = "60" };
            var txtInterval = new TextBox { Width = 360, Font = font, Text = "10000" };
            var chkRetain = new CheckBox { Text = "Retain Message", Font = font, AutoSize = true };

            var txtCsvFile = new TextBox { Width = 360, Font = font, ReadOnly = true };
            var btnCsvFile = new Button { Text = "选择 CSV 文件", Width = 160, Height = 36, Font = font };

            var btnStart = new Button { Text = "开始多线程发送", Width = 240, Height = 36, Font = font };
            var btnStop = new Button { Text = "停止", Width = 240, Height = 36, Font = font };

            var txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 200,
                Width = 720,
                Font = font,
                BackColor = Color.White
            };

            MultiThreadTaskManager.BindLogBox(txtLog);

            var statusPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Width = 720,
                Padding = new Padding(5)
            };

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

                    statusPanel.Controls.Clear();

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var cols = lines[i].Split(',');
                        if (cols.Length < 1) continue;

                        var topic = cols[0].Trim();
                        var user = cols.Length > 1 ? cols[1].Trim() : "";
                        var pass = cols.Length > 2 ? cols[2].Trim() : "";

                        topics.Add(topic);
                        usernames.Add(user);
                        passwords.Add(pass);

                        var lbl = new Label
                        {
                            Text = $"[{topic}] 状态：等待启动...",
                            AutoSize = true,
                            Width = 700,
                            Font = font
                        };
                        MultiThreadTaskManager.RegisterThreadStatus(topic, lbl);
                        statusPanel.Controls.Add(lbl);
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
                        passwords
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取 CSV 文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnStop.Click += (s, e) => MultiThreadTaskManager.StopAll();

            void AddRow(string label, Control input, Control button = null)
            {
                var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(3) };
                row.Controls.Add(new Label { Text = label, Width = 120, Font = font, TextAlign = ContentAlignment.MiddleLeft });
                row.Controls.Add(input);
                if (button != null) row.Controls.Add(button);
                panel.Controls.Add(row);
            }

            AddRow("Broker:", txtBroker);
            AddRow("Port:", txtPort);
            AddRow("KeepAlive:", txtKeepAlive);
            AddRow("Interval(ms):", txtInterval);
            AddRow("Retain:", chkRetain);
            AddRow("CSV 文件:", txtCsvFile, btnCsvFile);

            var actionRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            actionRow.Controls.Add(btnStart);
            actionRow.Controls.Add(btnStop);
            panel.Controls.Add(actionRow);

            panel.Controls.Add(new Label { Text = "线程状态：", AutoSize = true, Font = font });
            panel.Controls.Add(statusPanel);
            panel.Controls.Add(txtLog);

            return panel;
        }
    }
}
