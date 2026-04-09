using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class InstructionForm : Form
    {
        // 统一配色方案
        private static readonly Color PrimaryColor = Color.FromArgb(59, 130, 246);
        private static readonly Color LightGray = Color.FromArgb(248, 250, 252);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color TextColor = Color.FromArgb(51, 65, 85);
        private static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
        private static readonly Color White = Color.FromArgb(255, 255, 255);

        public InstructionForm()
        {
            Text = "使用说明";
            Size = new Size(600, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = LightGray;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimumSize = new Size(500, 400);

            SetupUI();
        }

        private void SetupUI()
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(20),
                BackColor = LightGray
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 内容区域
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // 底部按钮

            // 内容面板
            var contentPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                BackColor = LightGray
            };

            // 添加标题
            AddSectionTitle(contentPanel, "📋 功能说明");

            // 添加说明项
            AddInstructionItem(contentPanel, "随机数公式 [a-b,c]",
                "指 a 到 b 之间保留 c 位小数的随机数");

            AddInstructionItem(contentPanel, "递增公式 [a-b,c,d]",
                "指由 d 作为起始值，每次增长步长为 a-b 之间保留 c 位小数的随机数；首次发送时 d 将作为第一个值发送");

            AddInstructionItem(contentPanel, "使用范围",
                "以上功能仅在单次发送周期内生效");

            // 添加分隔线
            contentPanel.Controls.Add(CreateSeparator());

            // 添加更多说明
            AddSectionTitle(contentPanel, "⚙️ 参数设置");

            AddInstructionItem(contentPanel, "Retain Message",
                "勾选后每次重新连接时，都会接收最后一条保留消息");

            AddInstructionItem(contentPanel, "用户名和密码",
                "非必填项，根据服务器配置选择是否填写");

            AddInstructionItem(contentPanel, "多线程CSV格式",
                "A列为Topic，B列为用户名，C列为密码，D列为设备编号(device_id)，逗号分隔");

            // 添加音乐推荐
            contentPanel.Controls.Add(CreateSeparator());

            AddSectionTitle(contentPanel, "🎵 今日推荐");
            AddInstructionItem(contentPanel, "古典音乐",
                "Piano Concerto No. 1 in G Minor, Op. 25, MWV O7 - I. Molto allegro con fuoco");

            // 添加版本信息
            contentPanel.Controls.Add(CreateSeparator());

            AddSectionTitle(contentPanel, "📱 应用信息");
            AddInstructionItem(contentPanel, "版本",
                "v0.5.1.0");
            AddInstructionItem(contentPanel, "开发团队",
                "Welcome Aboard! -- ANA3401");

            mainPanel.Controls.Add(contentPanel, 0, 0);

            // 底部按钮
            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = LightGray
            };

            var closeButton = CreateStyledButton("关闭", PrimaryColor);
            closeButton.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(closeButton);

            mainPanel.Controls.Add(bottomPanel, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private static void AddSectionTitle(FlowLayoutPanel panel, string title)
        {
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Margin = new Padding(0, 10, 0, 8),
                AutoSize = true
            };
            panel.Controls.Add(titleLabel);
        }

        private static void AddInstructionItem(FlowLayoutPanel panel, string title, string description)
        {
            var itemPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 8),
                BackColor = White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TextColor,
                AutoSize = true
            };

            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                AutoSize = true,
                MaximumSize = new Size(500, 0)
            };

            itemPanel.Controls.Add(titleLabel);
            itemPanel.Controls.Add(descLabel);
            panel.Controls.Add(itemPanel);
        }

        private static Panel CreateSeparator()
        {
            return new Panel
            {
                Height = 1,
                Width = 550,
                BackColor = BorderColor,
                Margin = new Padding(0, 15, 0, 15)
            };
        }

        private static Button CreateStyledButton(string text, Color bgColor)
        {
            var button = new Button
            {
                Text = text,
                Width = 100,
                Height = 36,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = bgColor,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ChangeColorBrightness(bgColor, -20);
            button.FlatAppearance.MouseDownBackColor = ChangeColorBrightness(bgColor, -40);

            return button;
        }

        private static Color ChangeColorBrightness(Color color, int brightnessChange)
        {
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, color.R + brightnessChange)),
                Math.Max(0, Math.Min(255, color.G + brightnessChange)),
                Math.Max(0, Math.Min(255, color.B + brightnessChange))
            );
        }
    }
}
