using System;
using System.Collections.Generic;
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


        public static async Task<string> ReadMessageAsync(Dictionary<string, string> configuredValues)
        {
            string messageFile = "sim_message.txt";
            if (!File.Exists(messageFile))
            {
                throw new FileNotFoundException($"消息文件 '{messageFile}' 不存在！");
            }

            string content = await File.ReadAllTextAsync(messageFile);
            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 🚀 更新根级 `ts`
            if (jsonDict.ContainsKey("ts"))
            {
                jsonDict["ts"] = currentTimestamp;
            }
            else
            {
                jsonDict.Add("ts", currentTimestamp);
            }

            if (jsonDict.ContainsKey("devs"))
            {
                foreach (var dev in JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonDict["devs"].ToString()))
                {
                    foreach (var data in JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dev["d"].ToString()))
                    {
                        // 🚀 更新 `ts`
                        data["ts"] = currentTimestamp;

                        // 🚀 替换 `m-v`
                        string m = data["m"].ToString();
                        if (configuredValues != null && configuredValues.ContainsKey(m))
                        {
                            data["v"] = GenerateValue(configuredValues[m]);
                        }
                    }
                }
            }

            return JsonSerializer.Serialize(jsonDict);
        }


        private static object GenerateValue(string valueConfig)
        {
            Match match = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (match.Success)
            {
                double min = double.Parse(match.Groups[1].Value);
                double max = double.Parse(match.Groups[3].Value);
                int decimalPlaces = int.Parse(match.Groups[5].Value);

                return Math.Round(min + new Random().NextDouble() * (max - min), decimalPlaces);
            }
            return valueConfig;
        }
    }
}
