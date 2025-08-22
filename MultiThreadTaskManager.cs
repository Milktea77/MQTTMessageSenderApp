using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private static readonly ConcurrentQueue<string> logQueue = new();
        private static Timer logTimer;
        private static SemaphoreSlim semaphore;

        public static int MaxConcurrency { get; set; } = 500;
        public static int LogFlushInterval { get; set; } = 500;

        private const int MaxLogLines = 500;
        private const int MaxQueueLength = 10000;

        public static void BindLogBox(TextBox outputBox)
        {
            logBox = outputBox;
            StartLogTimer();
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
            if (logQueue.Count < MaxQueueLength)
            {
                logQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }

        private static void FlushLogQueue(object state)
        {
            if (logBox == null) return;

            List<string> logs = new List<string>();
            while (logQueue.TryDequeue(out var item))
            {
                logs.Add(item);
            }

            if (logs.Count == 0) return;

            string text = string.Join(Environment.NewLine, logs) + Environment.NewLine;

            if (logBox.InvokeRequired)
            {
                logBox.Invoke((MethodInvoker)(() => AppendLog(text)));
            }
            else
            {
                AppendLog(text);
            }
        }

        private static void AppendLog(string text)
        {
            if (logBox == null) return;

            logBox.AppendText(text);

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

        private static void StartLogTimer()
        {
            logTimer?.Dispose();
            logTimer = new Timer(FlushLogQueue, null, 0, LogFlushInterval);
        }

        public static void SetLogFlushInterval(int interval)
        {
            LogFlushInterval = interval;
            logTimer?.Change(0, LogFlushInterval);
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
                                     List<string> topics, List<string> usernames, List<string> passwords, List<List<string>> deviceIdsList,
                                     int? maxConcurrency = null, int? logFlushInterval = null)
        {
            StopAll();

            if (maxConcurrency.HasValue) MaxConcurrency = maxConcurrency.Value;
            if (logFlushInterval.HasValue) SetLogFlushInterval(logFlushInterval.Value);

            cts = new CancellationTokenSource();
            var token = cts.Token;
            semaphore = new SemaphoreSlim(MaxConcurrency);

            for (int i = 0; i < topics.Count; i++)
            {
                string topic = topics[i].Trim();
                string username = usernames[i].Trim();
                string password = passwords[i].Trim();
                var deviceIds = deviceIdsList[i];

                messageCountMap[topic] = 0;

                Task t = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(token);
                    var pool = MqttClientPoolManager.GetPool(broker, port, keepalive, username, password, MaxConcurrency);
                    IMqttClient mqttClient = null;
                    try
                    {
                        int retryCount = 0;
                        mqttClient = await pool.GetClientAsync(token);
                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                if (!mqttClient.IsConnected)
                                {
                                    await Task.Delay(1000, token);
                                    continue;
                                }

                                Log($"[{topic}] 已连接");
                                UpdateThreadStatus(topic, "已连接");
                                retryCount = 0;

                                while (!token.IsCancellationRequested && mqttClient.IsConnected)
                                {
                                    string message = await MessageFileHandler.ReadMessageAsync(deviceIds);

                                    var mqttMessage = new MqttApplicationMessageBuilder()
                                        .WithTopic(topic)
                                        .WithPayload(message)
                                        .WithRetainFlag(retain)
                                        .Build();

                                    await mqttClient.PublishAsync(mqttMessage, token);

                                    messageCountMap[topic]++;
                                    Log($"[{topic}] 已发送消息，总计: {messageCountMap[topic]}");
                                    await Task.Delay(interval, token);
                                }

                                UpdateThreadStatus(topic, "已断开");
                                Log($"[{topic}] 已断开连接");
                            }
                            catch (Exception ex)
                            {
                                retryCount++;
                                Log($"[{topic}] 异常({retryCount}): {ex.Message}");
                                UpdateThreadStatus(topic, $"重试中({retryCount})");
                                await Task.Delay(3000, token);
                            }
                        }
                    }
                    finally
                    {
                        if (mqttClient != null)
                            pool.ReturnClient(mqttClient);
                        semaphore.Release();
                    }
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

            semaphore?.Dispose();
            logTimer?.Dispose();
            MqttClientPoolManager.DisposeAllAsync().GetAwaiter().GetResult();
        }
    }
}