using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTMessageSenderApp
{
    public static class MultiThreadTaskManager
    {
        private static List<Task> tasks = new List<Task>();
        private static CancellationTokenSource cts;
        private static TextBox logBox;
        private static Dictionary<string, int> messageCountMap = new();
        private static Dictionary<string, Label> threadStatusMap = new();
        private const int MaxLogLines = 500;
        public static int MaxConcurrency { get; set; } = 1;

        public static void BindLogBox(TextBox outputBox)
        {
            logBox = outputBox;
        }

        public static void RegisterThreadStatus(string topic, Label statusLabel)
        {
            threadStatusMap[topic] = statusLabel;
        }

        private static void UpdateThreadStatus(string topic, string status)
        {
            if (threadStatusMap.ContainsKey(topic))
            {
                var label = threadStatusMap[topic];
                if (label.InvokeRequired)
                {
                    label.Invoke((MethodInvoker)(() => label.Text = status));
                }
                else
                {
                    label.Text = status;
                }
            }
        }

        private static void Log(string message)
        {
            if (logBox?.InvokeRequired == true)
            {
                logBox.Invoke((MethodInvoker)(() => AppendLog(message)));
            }
            else
            {
                AppendLog(message);
            }
        }

        private static void AppendLog(string message)
        {
            if (logBox == null) return;

            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");

            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();

            var lines = logBox.Lines;
            if (lines.Length > MaxLogLines)
            {
                int removeCount = lines.Length - MaxLogLines;
                string[] trimmed = new string[MaxLogLines];
                Array.Copy(lines, removeCount, trimmed, 0, MaxLogLines);
                logBox.Lines = trimmed;
            }
        }

        public static void ExportLogToFile(string filePath)
        {
            if (logBox == null) return;
            try
            {
                File.WriteAllText(filePath, logBox.Text);
                MessageBox.Show("日志已导出：" + filePath, "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ClearLog()
        {
            if (logBox == null) return;
            if (logBox.InvokeRequired)
            {
                logBox.Invoke((MethodInvoker)(() => logBox.Clear()));
            }
            else
            {
                logBox.Clear();
            }
        }

        public static void StartAll(string broker, int port, int keepalive, int interval, bool retain,
                                     List<string> topics, List<string> usernames, List<string> passwords, List<List<string>> deviceIdsList)
        {
            StopAll();

            cts = new CancellationTokenSource();
            var token = cts.Token;
            var semaphore = new SemaphoreSlim(MaxConcurrency);

            for (int i = 0; i < topics.Count; i++)
            {
                string topic = topics[i].Trim();
                string username = usernames[i].Trim();
                string password = passwords[i].Trim();
                var deviceIds = deviceIdsList[i];

                messageCountMap[topic] = 0;

                Task t = Task.Run(async () =>
                {
                    var pool = MqttClientPool.GetPool(broker, port, keepalive, username, password);

                    while (!token.IsCancellationRequested)
                    {
                        IMqttClient? mqttClient = null;
                        try
                        {
                            await semaphore.WaitAsync(token);
                            mqttClient = await pool.GetClientAsync(token);

                            string message = await MessageFileHandler.ReadMessageAsync(deviceIds);

                            var mqttMessage = new MqttApplicationMessageBuilder()
                                .WithTopic(topic)
                                .WithPayload(message)
                                .WithRetainFlag(retain)
                                .Build();

                            await mqttClient.PublishAsync(mqttMessage, token);

                            messageCountMap[topic]++;
                            Log($"[{topic}] 已发送消息，总计: {messageCountMap[topic]}");
                            UpdateThreadStatus(topic, "运行中");
                        }
                        catch (Exception ex)
                        {
                            Log($"[{topic}] 异常: {ex.Message}");
                            UpdateThreadStatus(topic, $"异常: {ex.Message}");
                            await Task.Delay(3000, token);
                        }
                        finally
                        {
                            if (mqttClient != null)
                                pool.ReturnClient(mqttClient);
                            semaphore.Release();
                        }

                        await Task.Delay(interval, token);
                    }

                    UpdateThreadStatus(topic, "已停止");
                    Log($"[{topic}] 线程已结束");
                }, token);

                tasks.Add(t);
            }
        }

        public static void StopAll()
        {
            try
            {
                cts?.Cancel();
                Log("所有线程已请求停止。");
            }
            catch { }

            tasks.Clear();
            messageCountMap.Clear();
            threadStatusMap.Clear();
            MqttClientPool.ClearAll();
        }
    }
}