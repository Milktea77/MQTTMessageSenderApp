using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class InstructionForm : Form
    {
        public InstructionForm()
        {
            Text = "使用说明";
            Size = new Size(600, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Label instructionLabel = new Label
            {
                Text = "随机数公式 [a-b,c] 说明：\n" +
                       "指 a 到 b 之间保留 c 位小数的随机数\n\n" +
                       "递增公式 [a-b,c,d] 说明：\n" +
                       "指由 d 作为起始值，每次增长步长为 a-b 之间\n" +
                       "保留 c 位小数的随机数；\n" +
                       "首次发送时 d 将作为第一个值发送\n\n" +
                       "以上功能仅在单次发送周期内生效。\n\n" +
                       "勾选Retain Message以启用保留消息功能。\n" +
                       "勾选后每次重新连接时，都会接收最后一条保留消息。\n\n" +
                       "用户名和密码是非必填项。\n\n" +
                       "今日推荐：Piano Concerto No. 1 in G Minor, Op. 25, MWV O7 - I. Molto allegro con fuoco\n\n" +
                       "Version: 0.4.7.1-rc\n\n" +
                       "Welcome Aboard!  -- ANA3401",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20),
                AutoSize = false
            };

            Controls.Add(instructionLabel);
        }
    }
}
