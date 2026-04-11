using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class UIManager
    {
        private static Button configButton;
        private static Button sendButton;
        private static TextBox txtBroker, txtPort, txtKeepAlive, txtTopic, txtInterval;
        private static TextBox txtUsername, txtPassword, txtSimMessagePath;
        private static CheckBox chkRetain, chkUseMqtts, chkSslSecure;
        private static TextBox logTextBox;

        private const int MaxLogLines = 1000;
        private const string ConnectionConfigFile = "single_thread.json";
        private const string AppVersion = "v0.5.1.0";

        // ── Shared Color Palette (Tailwind slate/blue) ────────────────────────
        internal static readonly Color AppBg       = Color.FromArgb(241, 245, 249); // slate-100
        internal static readonly Color CardBg      = Color.White;
        internal static readonly Color LabelColor  = Color.FromArgb(71, 85, 105);   // slate-600
        internal static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);    // slate-900
        internal static readonly Color TextMuted   = Color.FromArgb(148, 163, 184); // slate-400
        internal static readonly Color DivColor    = Color.FromArgb(226, 232, 240); // slate-200
        internal static readonly Color InputBg     = Color.FromArgb(248, 250, 252); // slate-50
        internal static readonly Color AccentBlue  = Color.FromArgb(59, 130, 246);  // blue-500
        internal static readonly Color AccentPurple= Color.FromArgb(124, 58, 237);  // violet-600
        internal static readonly Color AccentGreen = Color.FromArgb(5, 150, 105);   // emerald-600
        internal static readonly Color AccentRed   = Color.FromArgb(220, 38, 38);   // red-600
        internal static readonly Color AccentGray  = Color.FromArgb(100, 116, 139); // slate-500
        internal static readonly Color TermBg      = Color.FromArgb(15, 23, 42);    // slate-900
        internal static readonly Color TermHdr     = Color.FromArgb(30, 41, 59);    // slate-800
        internal static readonly Color TermText    = Color.FromArgb(226, 232, 240); // slate-200
        internal static readonly Color TermGreen   = Color.FromArgb(134, 239, 172); // green-300
        internal static readonly Color StatusBg    = Color.FromArgb(226, 232, 240); // slate-200
        internal static readonly Color StatusText  = Color.FromArgb(100, 116, 139); // slate-500

        // ─────────────────────────────────────────────────────────────────────
        public static void SetupControl(MainForm form, EventHandler configClickHandler,
            Func<Button, string, string, string, string, string, bool, string, string, bool, bool, Task> sendClickHandler)
        {
            form.Text = "MQTT Message Sender";
            form.ClientSize = new Size(1220, 740);
            form.BackColor = AppBg;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Font = new Font("Segoe UI", 9F);
            form.MinimumSize = new Size(900, 600);

            // Root: tab area (fill) + status bar (26 px)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = AppBg, Padding = new Padding(0), Margin = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                Padding = new Point(14, 5)
            };
            var tSingle = new TabPage("  单线程  ") { BackColor = AppBg };
            tSingle.Controls.Add(BuildSingleThreadPanel(configClickHandler, sendClickHandler));
            var tMulti = new TabPage("  多线程  ") { BackColor = AppBg };
            tMulti.Controls.Add(MultiThreadPanelBuilder.BuildCsvBased());
            tabs.TabPages.Add(tSingle);
            tabs.TabPages.Add(tMulti);
            root.Controls.Add(tabs, 0, 0);

            // Status bar
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = StatusBg, Padding = new Padding(10, 0, 10, 0) };
            bar.Controls.Add(new Label
            {
                Text = $"MQTT Message Sender  {AppVersion}",
                Dock = DockStyle.Left, AutoSize = false, Width = 280,
                Font = new Font("Segoe UI", 7.5F), ForeColor = StatusText,
                TextAlign = ContentAlignment.MiddleLeft
            });
            bar.Controls.Add(new Label
            {
                Text = "ANA3401  ©  2026",
                Dock = DockStyle.Right, AutoSize = false, Width = 130,
                Font = new Font("Segoe UI", 7.5F), ForeColor = StatusText,
                TextAlign = ContentAlignment.MiddleRight
            });
            root.Controls.Add(bar, 0, 1);

            form.Controls.Add(root);
        }

        // ── Single-thread panel ───────────────────────────────────────────────
        private static SplitContainer BuildSingleThreadPanel(EventHandler configClickHandler,
            Func<Button, string, string, string, string, string, bool, string, string, bool, bool, Task> sendClickHandler)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = AppBg,
                BorderStyle = BorderStyle.None
            };
            split.HandleCreated += (s, e) =>
            {
                try
                {
                    split.Panel1MinSize = 360;
                    split.Panel2MinSize = 260;
                    split.SplitterDistance = (int)(split.Width * 0.50);
                }
                catch { }
            };

            // ── Left: form card ───────────────────────────────────────────────
            split.Panel1.BackColor = AppBg;
            split.Panel1.Padding = new Padding(10, 8, 5, 8);

            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = CardBg };

            txtBroker         = MkInput(270, placeholder: "e.g. broker.emqx.io");
            txtPort           = MkInput(90, text: "1883");
            txtKeepAlive      = MkInput(90, text: "60");
            txtTopic          = MkInput(270, placeholder: "e.g. /device/data");
            txtInterval       = MkInput(110, text: "60000");
            txtUsername       = MkInput(200, placeholder: "可选");
            txtPassword       = MkInput(200, placeholder: "可选", pwd: true);
            txtSimMessagePath = MkInput(210);
            txtSimMessagePath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_message.txt");

            chkRetain    = MkCheckbox("启用 Retain 消息");
            chkUseMqtts  = MkCheckbox("MQTTS (SSL/TLS)");
            chkSslSecure = MkCheckbox("验证 SSL 证书", enabled: false);

            chkUseMqtts.CheckedChanged += (s, e) =>
            {
                chkSslSecure.Enabled = chkUseMqtts.Checked;
                txtPort.Text = chkUseMqtts.Checked ? "8883" : "1883";
            };

            var btnBrowse = MkSmallBtn("浏览", AccentGray, 52);
            btnBrowse.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*" };
                if (File.Exists(txtSimMessagePath.Text))
                    ofd.InitialDirectory = Path.GetDirectoryName(txtSimMessagePath.Text);
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtSimMessagePath.Text = ofd.FileName;
                    MessageFileHandler.MessageFilePath = ofd.FileName;
                }
            };

            sendButton = MkActionBtn("▶  发送", AccentBlue, 108);
            sendButton.Click += async (sender, e) =>
            {
                MessageFileHandler.MessageFilePath = txtSimMessagePath.Text;
                await sendClickHandler(sendButton,
                    txtBroker.Text, txtPort.Text, txtKeepAlive.Text,
                    txtTopic.Text, txtInterval.Text,
                    chkRetain.Checked, txtUsername.Text, txtPassword.Text,
                    chkUseMqtts.Checked, chkSslSecure.Checked);
            };

            configButton = MkActionBtn("⚙  配置", AccentPurple, 108);
            configButton.Click += configClickHandler;

            var btnSave = MkActionBtn("💾  保存", AccentGreen, 108);
            btnSave.Click += (s, e) => SaveConnectionConfig();

            var btnHelp = MkOutlineBtn("说明", 70);
            btnHelp.Click += (s, e) => new InstructionForm().Show();

            // Flow container that fills scroll panel width
            var ff = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                BackColor = CardBg,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 16, 20, 20)
            };
            scrollPanel.SizeChanged += (s, e) =>
            {
                if (scrollPanel.ClientSize.Width > 60)
                    ff.Width = scrollPanel.ClientSize.Width;
            };

            // — 连接配置 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("连接配置", first: true));
            ff.Controls.Add(FormRow("Broker 地址", txtBroker));
            ff.Controls.Add(FormRow("端口 / 保活", InlineRow(txtPort, "  保活 (s)  ", txtKeepAlive)));
            ff.Controls.Add(FormRow("Topic", txtTopic));
            ff.Controls.Add(FormRow("发送间隔 (ms)", txtInterval));

            // — 认证信息 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("认证信息"));
            ff.Controls.Add(FormRow("用户名", txtUsername));
            ff.Controls.Add(FormRow("密码", txtPassword));

            // — 连接选项 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("连接选项"));
            ff.Controls.Add(FormRow("", InlineRow(chkUseMqtts, chkSslSecure, chkRetain)));

            // — 消息文件 ——————————————————————————————————————————————————————
            ff.Controls.Add(SecHead("消息文件"));
            ff.Controls.Add(FormRow("文件路径", InlineRow(txtSimMessagePath, btnBrowse)));

            // — 操作按钮 ——————————————————————————————————————————————————————
            var btnRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = CardBg,
                Margin = new Padding(0, 18, 0, 0)
            };
            btnRow.Controls.Add(sendButton);
            btnRow.Controls.Add(configButton);
            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnHelp);
            ff.Controls.Add(btnRow);

            scrollPanel.Controls.Add(ff);
            split.Panel1.Controls.Add(scrollPanel);

            // ── Right: terminal log ───────────────────────────────────────────
            split.Panel2.BackColor = AppBg;
            split.Panel2.Padding = new Padding(5, 8, 10, 8);

            split.Panel2.Controls.Add(BuildTerminal(ref logTextBox, "●  运行日志",
                clearClick: (s, e) => logTextBox?.Clear()));

            LoadConnectionConfig();
            return split;
        }

        // ── Terminal panel builder (shared by both tabs) ──────────────────────
        internal static Panel BuildTerminal(ref TextBox logBox, string title, EventHandler clearClick)
        {
            var terminal = new Panel { Dock = DockStyle.Fill, BackColor = TermBg };

            var hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 38,
                BackColor = TermHdr, Padding = new Padding(12, 0, 8, 0)
            };
            hdr.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TermGreen,
                TextAlign = ContentAlignment.MiddleLeft
            });

            var clearBtn = new Button
            {
                Text = "清空", Dock = DockStyle.Right, Width = 52,
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.FromArgb(148, 163, 184),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            clearBtn.FlatAppearance.BorderSize = 0;
            clearBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
            clearBtn.Click += clearClick;
            hdr.Controls.Add(clearBtn);

            logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5F),
                BackColor = TermBg, ForeColor = TermText,
                BorderStyle = BorderStyle.None
            };

            terminal.Controls.Add(logBox);
            terminal.Controls.Add(hdr);
            return terminal;
        }

        // ── Section header ────────────────────────────────────────────────────
        private static FlowLayoutPanel SecHead(string title, bool first = false)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = CardBg,
                Margin = new Padding(0, first ? 0 : 20, 0, 6),
                Padding = new Padding(0)
            };
            p.Controls.Add(new Panel
            {
                Width = 3, Height = 16, BackColor = AccentBlue,
                Margin = new Padding(0, 2, 8, 2)
            });
            p.Controls.Add(new Label
            {
                Text = title, AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Padding = new Padding(0, 1, 0, 1)
            });
            return p;
        }

        // ── Single form row: [label] [control] ───────────────────────────────
        private static FlowLayoutPanel FormRow(string labelText, Control control)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = CardBg,
                Margin = new Padding(0, 3, 0, 3)
            };
            if (!string.IsNullOrEmpty(labelText))
                row.Controls.Add(new Label
                {
                    Text = labelText,
                    Width = 96, Height = 28,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = LabelColor,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            row.Controls.Add(control);
            return row;
        }

        // ── Inline horizontal grouping ────────────────────────────────────────
        private static FlowLayoutPanel InlineRow(params object[] items)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, BackColor = CardBg,
                Margin = new Padding(0), Padding = new Padding(0)
            };
            foreach (var item in items)
            {
                if (item is Control c)
                    p.Controls.Add(c);
                else if (item is string s)
                    p.Controls.Add(new Label
                    {
                        Text = s, AutoSize = true, Height = 28,
                        Font = new Font("Segoe UI", 8.5F), ForeColor = TextMuted,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Padding = new Padding(2, 0, 2, 0)
                    });
            }
            return p;
        }

        // ── Control factories ─────────────────────────────────────────────────
        internal static TextBox MkInput(int width, string text = "", string placeholder = "", bool pwd = false)
            => new TextBox
            {
                Width = width, Height = 28, Text = text,
                Font = new Font("Segoe UI", 9F),
                BackColor = InputBg, ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = pwd,
                PlaceholderText = placeholder,
                Margin = new Padding(0, 0, 6, 0)
            };

        internal static CheckBox MkCheckbox(string text, bool isChecked = false, bool enabled = true)
            => new CheckBox
            {
                Text = text, Checked = isChecked, AutoSize = true, Enabled = enabled,
                Font = new Font("Segoe UI", 9F),
                ForeColor = LabelColor, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(2, 3, 2, 3)
            };

        internal static Button MkActionBtn(string text, Color bg, int width)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 34,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Shift(bg, -18);
            b.FlatAppearance.MouseDownBackColor = Shift(bg, -36);
            return b;
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
            b.FlatAppearance.MouseOverBackColor = Shift(bg, -18);
            return b;
        }

        private static Button MkOutlineBtn(string text, int width)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 34,
                Font = new Font("Segoe UI", 9F),
                BackColor = CardBg, ForeColor = LabelColor,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            b.FlatAppearance.BorderColor = DivColor;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = AppBg;
            return b;
        }

        internal static Color Shift(Color c, int d) => Color.FromArgb(
            Math.Max(0, Math.Min(255, c.R + d)),
            Math.Max(0, Math.Min(255, c.G + d)),
            Math.Max(0, Math.Min(255, c.B + d)));

        // ── Log ───────────────────────────────────────────────────────────────
        public static void AppendLog(string message)
        {
            if (logTextBox == null) return;
            if (logTextBox.InvokeRequired)
                logTextBox.Invoke((MethodInvoker)(() => DoAppendLog(message)));
            else
                DoAppendLog(message);
        }

        private static void DoAppendLog(string message)
        {
            if (logTextBox == null) return;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
            var lines = logTextBox.Lines;
            if (lines.Length > MaxLogLines)
            {
                int rm = lines.Length - MaxLogLines;
                var t = new string[MaxLogLines];
                Array.Copy(lines, rm, t, 0, MaxLogLines);
                logTextBox.Lines = t;
            }
        }

        // ── Config ────────────────────────────────────────────────────────────
        private static void SaveConnectionConfig()
        {
            try
            {
                ConfigManager.SaveConnectionConfig(ConnectionConfigFile, new ConnectionConfig
                {
                    Name = "single_thread",
                    Broker        = txtBroker?.Text        ?? "",
                    Port          = txtPort?.Text          ?? "1883",
                    KeepAlive     = txtKeepAlive?.Text     ?? "60",
                    Topic         = txtTopic?.Text         ?? "",
                    Interval      = txtInterval?.Text      ?? "60000",
                    Retain        = chkRetain?.Checked     ?? false,
                    Username      = txtUsername?.Text      ?? "",
                    Password      = txtPassword?.Text      ?? "",
                    UseMqtts      = chkUseMqtts?.Checked   ?? false,
                    SslSecure     = chkSslSecure?.Checked  ?? false,
                    SimMessagePath= txtSimMessagePath?.Text ?? "",
                    CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
                });
                AppendLog("配置已保存 → config/single_thread.json");
                MessageBox.Show("配置已保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LoadConnectionConfig()
        {
            try
            {
                var c = ConfigManager.LoadConnectionConfig(ConnectionConfigFile);
                if (c == null) return;
                if (txtBroker    != null) txtBroker.Text    = c.Broker    ?? "";
                if (txtKeepAlive != null) txtKeepAlive.Text = c.KeepAlive ?? "60";
                if (txtTopic     != null) txtTopic.Text     = c.Topic     ?? "";
                if (txtInterval  != null) txtInterval.Text  = c.Interval  ?? "60000";
                if (txtUsername  != null) txtUsername.Text  = c.Username  ?? "";
                if (txtPassword  != null) txtPassword.Text  = c.Password  ?? "";
                if (chkRetain    != null) chkRetain.Checked = c.Retain;
                if (chkUseMqtts  != null) chkUseMqtts.Checked = c.UseMqtts;
                if (txtPort      != null) txtPort.Text = c.Port ?? "1883";
                if (chkSslSecure != null) chkSslSecure.Checked = c.SslSecure;
                if (txtSimMessagePath != null && !string.IsNullOrWhiteSpace(c.SimMessagePath))
                {
                    txtSimMessagePath.Text = c.SimMessagePath;
                    MessageFileHandler.MessageFilePath = c.SimMessagePath;
                }
            }
            catch { }
        }

        public static void SetConfigButtonEnabled(bool enabled)
        {
            if (configButton?.IsDisposed == false)
                configButton.Invoke((MethodInvoker)(() =>
                {
                    configButton.Enabled = enabled;
                    configButton.BackColor = enabled ? AccentPurple : Shift(AccentGray, 40);
                }));
        }
    }
}
