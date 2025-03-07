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

        public TrayManager(MainForm form)
        {
            mainForm = form;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Text = "MQTT Message Sender",
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
        private void OnFormResize(object sender, EventArgs e) { if (mainForm.WindowState == FormWindowState.Minimized) HideFormToTray(); }

        private void HideFormToTray()
        {
            mainForm.Hide();
            trayIcon.Visible = true;
        }

        private void ShowFormFromTray()
        {
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }
    }
}
