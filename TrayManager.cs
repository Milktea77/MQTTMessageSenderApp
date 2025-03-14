using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class TrayManager
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private MainForm mainForm;
        private string topic = "未配置"; // 默认显示“未配置”
        private bool isRunning = false; // 默认状态未运行

        public TrayManager(MainForm form)
        {
            mainForm = form;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false
            };

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示", null, OnTrayMenuShowClick);
            trayMenu.Items.Add("退出", null, OnTrayMenuExitClick);
            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.DoubleClick += OnTrayIconDoubleClick;
            mainForm.Resize += OnFormResize;
        }

        private void OnTrayMenuShowClick(object sender, EventArgs e) => ShowFormFromTray();
        private void OnTrayMenuExitClick(object sender, EventArgs e) => Application.Exit();
        private void OnTrayIconDoubleClick(object sender, EventArgs e) => ShowFormFromTray();
        private void OnFormResize(object sender, EventArgs e)
        {
            if (mainForm.WindowState == FormWindowState.Minimized)
            {
                HideFormToTray();
            }
        }

        private void HideFormToTray()
        {
            mainForm.Hide();
            trayIcon.Visible = true;
            UpdateTrayTooltip(); // 更新悬停提示
        }

        private void ShowFormFromTray()
        {
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }

        public void UpdateTopic(string newTopic)
        {
            topic = string.IsNullOrWhiteSpace(newTopic) ? "未配置" : newTopic;
            UpdateTrayTooltip();
        }

        public void SetRunningStatus(bool running)
        {
            isRunning = running;
            UpdateTrayTooltip();
        }

        private void UpdateTrayTooltip()
        {
            trayIcon.Text = $"MQTT Message Sender\n" +
                            $"状态: {(isRunning ? "运行中" : "未运行")}\n" +
                            $"目标 Topic: {topic}";
        }
    }
}
