using System;
using System.IO;
using System.Threading.Tasks;

namespace MQTTMessageSenderApp
{
    public static class MessageFileHandler
    {
        private static readonly string messageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_message.txt");

        /// <summary>
        /// 检查 `sim_message.txt` 是否为空
        /// </summary>
        public static bool IsMessageFileEmpty()
        {
            if (!File.Exists(messageFile))
            {
                return true; // 🚀 如果文件不存在，则认为是“空”文件
            }

            string content = File.ReadAllText(messageFile);
            return string.IsNullOrWhiteSpace(content);
        }

        /// <summary>
        /// 读取 `sim_message.txt` 内容（不再进行空内容校验）
        /// </summary>
        public static async Task<string> ReadMessageAsync()
        {
            if (!File.Exists(messageFile))
            {
                throw new FileNotFoundException($"消息文件 '{messageFile}' 在同目录中不存在！");
            }

            return await File.ReadAllTextAsync(messageFile);
        }
    }
}
