using System;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm("MQTT Message Sender"));
        }
    }
}
