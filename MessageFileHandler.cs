using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MQTTMessageSenderApp
{
    public static class MessageFileHandler
    {
        public static bool IsMessageFileEmpty()
        {
            string messageFile = "sim_message.txt";
            if (!File.Exists(messageFile))
            {
                return true; // 文件不存在视为空
            }

            string content = File.ReadAllText(messageFile);
            return string.IsNullOrWhiteSpace(content); // 内容为空或仅有空格
        }


        public static async Task<string> ReadMessageAsync()
        {
            string messageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_message.txt");
            if (!File.Exists(messageFile))
            {
                throw new FileNotFoundException($"消息文件 '{messageFile}' 不存在！");
            }

            string content = await File.ReadAllTextAsync(messageFile);
            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            jsonDict["ts"] = currentTimestamp; // 更新根级 ts

            if (jsonDict.ContainsKey("devs"))
            {
                List<Dictionary<string, object>> devices = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonDict["devs"].ToString());

                foreach (var dev in devices)
                {
                    if (dev.ContainsKey("d"))
                    {
                        List<Dictionary<string, object>> deviceData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dev["d"].ToString());

                        foreach (var data in deviceData)
                        {
                            data["ts"] = currentTimestamp; // 更新设备 ts

                            string vStr = data["v"].ToString();
                            data["v"] = GenerateValue(vStr); // 转换 `[a-b,c]` 为随机数
                        }

                        dev["d"] = deviceData;
                    }
                }

                jsonDict["devs"] = devices;
            }

            return JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object GenerateValue(string valueConfig)
        {
            Trace.WriteLine($"解析 GenerateValue: {valueConfig}");

            Match match = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (match.Success)
            {
                double min = double.Parse(match.Groups[1].Value);
                double max = double.Parse(match.Groups[3].Value);
                int decimalPlaces = int.Parse(match.Groups[5].Value);

                double generatedValue = Math.Round(min + new Random().NextDouble() * (max - min), decimalPlaces);
                Trace.WriteLine($"生成随机值: {generatedValue}");
                return generatedValue;
            }

            return double.TryParse(valueConfig, out double fixedValue) ? fixedValue : valueConfig;
        }

    }
}
