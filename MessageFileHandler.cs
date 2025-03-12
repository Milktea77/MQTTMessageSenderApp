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

            // 更新根级 ts
            jsonDict["ts"] = currentTimestamp;

            if (jsonDict.ContainsKey("devs") && jsonDict["devs"] is JsonElement devsElement)
            {
                var devices = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(devsElement.GetRawText());

                foreach (var dev in devices)
                {
                    dev["ts"] = currentTimestamp; // 更新每个设备的 ts

                    if (dev.ContainsKey("d") && dev["d"] is JsonElement dElement)
                    {
                        var deviceData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dElement.GetRawText());

                        foreach (var data in deviceData)
                        {
                            data["ts"] = currentTimestamp; // 更新设备数据的 ts

                            if (data.ContainsKey("v") && data["v"] is JsonElement vElement)
                            {
                                string vStr = vElement.GetRawText().Trim('"'); // 解析 v 字符串
                                data["v"] = GenerateValue(vStr);
                            }
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
