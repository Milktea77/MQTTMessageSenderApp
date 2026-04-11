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
        private static TextBox multiLogBox;

        public static Control BuildCsvBased()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = UIManager.AppBg,
                BorderStyle = BorderStyle.None
            };
            split.HandleCreated += (s, e) =>
            {
                try
                {
                    split.Panel1MinSize = 360;
                    split.Panel2MinSize = 260;
                    split.SplitterDistance = (int)(split.Width * 0.48);
                }
                catch { }
            };

            // ── Left: form card ───────────────────────────────────────────────
            split.Panel1.BackColor = UIManager.AppBg;
            split.Panel1.Padding = new Padding(10, 8, 5, 8);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UIManager.CardBg };

            var txtBroker   = UIManager.MkInput(270, placeholder: "e.g. broker.emqx.io");
            var txtPort     = UIManager.MkInput(90, text: "1883");
            var txtKeepAlive= UIManager.MkInput(90, text: "60");
            var txtInterval = UIManager.MkInput(110, text: "10000");
            var chkRetain   = UIManager.MkCheckbox("启用 Retain 消息");

            var txtCsvPath  = UIManager.MkInput(210, placeholder: "选择 CSV 文件...");
            txtCsvPath.ReadOnly = true;
            txtCsvPath.BackColor = UIManager.InputBg;

            var btnPickCsv = MkSmallBtn("浏览", UIManager.AccentGray, 52);
            btnPickCsv.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK) txtCsvPath.Text = ofd.FileName;
            };

            var btnStart = UIManager.MkActionBtn("▶  开始", UIManager.AccentGreen, 108);
            var btnStop  = UIManager.MkActionBtn("■  停止", UIManager.AccentRed,   108);

            // Thread-status label container (added dynamically)
            var statusFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = UIManager.CardBg,
                Margin = new Padding(0), Padding = new Padding(0),
                WrapContents = false
            };

            btnStart.Click += (s, e) =>
            {
                if (!int.TryParse(txtPort.Text, out int port) ||
                    !int.TryParse(txtKeepAlive.Text, out int keepalive) ||
                    !int.TryParse(txtInterval.Text, out int interval) ||
                    string.IsNullOrWhiteSpace(txtBroker.Text) ||
                    string.IsNullOrWhiteSpace(txtCsvPath.Text) ||
                    !File.Exists(txtCsvPath.Text))
                {
                    MessageBox.Show("请填写正确的连接信息，并选择有效的 CSV 文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    var lines = File.ReadAllLines(txtCsvPath.Text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    if (lines.Count < 1)
                    {
                        MessageBox.Show("CSV 文件无有效数据。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    statusFlow.Controls.Clear();

                    var topics      = new List<string>();
                    var usernames   = new List<string>();
                    var passwords   = new List<string>();
                    var deviceIdList= new List<List<string>>();
                    var topicUserMap= new Dictionary<string, string>();
                    var topicPassMap= new Dictionary<string, string>();
                    var topicDevMap = new Dictionary<string, List<string>>();

                    foreach (var line in lines)
                    {
                        var cols = line.Split(',');
                        if (cols.Length < 1) continue;
                        var topic  = cols[0].Trim();
                        var user   = cols.Length > 1 ? cols[1].Trim() : "";
                        var pass   = cols.Length > 2 ? cols[2].Trim() : "";
                        var device = cols.Length > 3 ? cols[3].Trim() : "";
                        if (!topicDevMap.ContainsKey(topic))
                        {
                            topicDevMap[topic]  = new List<string>();
                            topicUserMap[topic] = user;
                            topicPassMap[topic] = pass;
                        }
                        if (!string.IsNullOrWhiteSpace(device))
                            topicDevMap[topic].Add(device);
                    }

                    foreach (var kvp in topicDevMap)
                    {
                        topics.Add(kvp.Key);
                        usernames.Add(topicUserMap[kvp.Key]);
                        passwords.Add(topicPassMap[kvp.Key]);
                        deviceIdList.Add(kvp.Value);

                        var statusLbl = new Label
                        {
                            Text = $"[{kvp.Key}]  等待连接...",
                            AutoSize = true,
                            Font = new Font("Segoe UI", 8.5F),
                            ForeColor = UIManager.TextMuted,
                            Margin = new Padding(0, 2, 0, 2)
                        };
                        statusFlow.Controls.Add(statusLbl);
                        MultiThreadTaskManager.RegisterThreadStatus(kvp.Key, statusLbl);
                    }

                    if (topics.Count == 0)
                    {
                        MessageBox.Show("CSV 中未找到有效 Topic 行。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    MultiThreadTaskManager.StartAll(txtBroker.Text.Trim(), port, keepalive, interval,
                        chkRetain.Checked, topics, usernames, passwords, deviceIdList);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取 CSV 文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnStop.Click += (s, e) => MultiThreadTaskManager.StopAll();

            // Build form flow
            var ff = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                BackColor = UIManager.CardBg,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 16, 20, 20)
            };
            scroll.SizeChanged += (s, e) =>
            {
                if (scroll.ClientSize.Width > 60)
                    ff.Width = scroll.ClientSize.Width;
            };

            // — 连接配置 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("连接配置", first: true));
            ff.Controls.Add(FormRow("Broker 地址", txtBroker));
            ff.Controls.Add(FormRow("端口 / 保活", InlineRow(txtPort, "  保活 (s)  ", txtKeepAlive)));
            ff.Controls.Add(FormRow("发送间隔 (ms)", txtInterval));
            ff.Controls.Add(FormRow("消息选项", chkRetain));

            // — CSV 文件 ———————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("CSV 文件"));
            ff.Controls.Add(FormRow("文件路径", InlineRow(txtCsvPath, btnPickCsv)));

            var hint = new Label
            {
                Text = "格式: Topic, 用户名, 密码, 设备ID（逗号分隔，每行一个设备）",
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = UIManager.TextMuted,
                Margin = new Padding(0, 2, 0, 0)
            };
            ff.Controls.Add(hint);

            // — 操作按钮 ——————————————————————————————————————————————————————
            var btnRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = UIManager.CardBg,
                Margin = new Padding(0, 18, 0, 0)
            };
            btnRow.Controls.Add(btnStart);
            btnRow.Controls.Add(btnStop);
            ff.Controls.Add(btnRow);

            // — 线程状态 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("线程状态"));
            ff.Controls.Add(statusFlow);

            scroll.Controls.Add(ff);
            split.Panel1.Controls.Add(scroll);

            // ── Right: terminal log ───────────────────────────────────────────
            split.Panel2.BackColor = UIManager.AppBg;
            split.Panel2.Padding = new Padding(5, 8, 10, 8);

            var panel = UIManager.BuildTerminal(ref multiLogBox, "●  多线程日志",
                clearClick: (s, e) =>
                {
                    multiLogBox?.Clear();
                    MultiThreadTaskManager.ClearLog();
                });
            MultiThreadTaskManager.BindLogBox(multiLogBox);
            split.Panel2.Controls.Add(panel);

            return split;
        }

        // ── Helpers (mirror UIManager's style) ────────────────────────────────
        private static FlowLayoutPanel SecHead(string title, bool first = false)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = UIManager.CardBg,
                Margin = new Padding(0, first ? 0 : 20, 0, 6),
                Padding = new Padding(0)
            };
            p.Controls.Add(new Panel
            {
                Width = 3, Height = 16, BackColor = UIManager.AccentBlue,
                Margin = new Padding(0, 2, 8, 2)
            });
            p.Controls.Add(new Label
            {
                Text = title, AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UIManager.TextPrimary,
                Padding = new Padding(0, 1, 0, 1)
            });
            return p;
        }

        private static FlowLayoutPanel FormRow(string labelText, Control control)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = UIManager.CardBg,
                Margin = new Padding(0, 3, 0, 3)
            };
            if (!string.IsNullOrEmpty(labelText))
                row.Controls.Add(new Label
                {
                    Text = labelText,
                    Width = 96, Height = 28,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = UIManager.LabelColor,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            row.Controls.Add(control);
            return row;
        }

        private static FlowLayoutPanel InlineRow(params object[] items)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = UIManager.CardBg,
                Margin = new Padding(0), Padding = new Padding(0)
            };
            foreach (var item in items)
            {
                if (item is Control c) p.Controls.Add(c);
                else if (item is string s)
                    p.Controls.Add(new Label
                    {
                        Text = s, AutoSize = true, Height = 28,
                        Font = new Font("Segoe UI", 8.5F), ForeColor = UIManager.TextMuted,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Padding = new Padding(2, 0, 2, 0)
                    });
            }
            return p;
        }

        private static Button MkSmallBtn(string text, Color bg, int width)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 28,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = UIManager.Shift(bg, -18);
            return b;
        }
    }
}
