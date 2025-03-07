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

        public static async Task<string> ReadMessageAsync()
        {
            if (!File.Exists(messageFile))
            {
                throw new FileNotFoundException($"消息文件 '{messageFile}' 在同目录中不存在！");
            }

            string content = await File.ReadAllTextAsync(messageFile);
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            json["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return JsonSerializer.Serialize(json);
        }
    }
}
