using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        /// 读取 `sim_message.txt` 并自动替换 `ts` 字段为当前时间戳
        /// </summary>
        public static async Task<string> ReadMessageAsync()
        {
            if (!File.Exists(messageFile))
            {
                throw new FileNotFoundException($"消息文件 '{messageFile}' 在同目录中不存在！");
            }

            string content = await File.ReadAllTextAsync(messageFile);

            if (string.IsNullOrWhiteSpace(content))
            {
                return content; // 🚀 如果内容为空，直接返回，不修改
            }

            try
            {
                // 解析 JSON 数据
                var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                // 🚀 自动替换 `ts` 字段为当前时间戳
                jsonDict["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 重新序列化 JSON 并返回
                return JsonSerializer.Serialize(jsonDict);
            }
            catch (JsonException)
            {
                // 🚀 不是 JSON 格式，直接返回原始内容，不修改
                return content;
            }
        }
    }
}
