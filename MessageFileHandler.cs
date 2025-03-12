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
        private static Dictionary<string, Dictionary<string, double>> incrementValues = new Dictionary<string, Dictionary<string, double>>();

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
                    string deviceId = dev["dev"].ToString();
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
                                data["v"] = GenerateValue(deviceId, data["m"].ToString(), vStr);
                            }
                        }

                        dev["d"] = deviceData;
                    }
                }

                jsonDict["devs"] = devices;
            }

            return JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object GenerateValue(string deviceId, string functionName, string valueConfig)
        {
            Match match = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+),(\d+(\.\d+)?)\]");
            if (match.Success)
            {
                double min = double.Parse(match.Groups[1].Value);
                double max = double.Parse(match.Groups[3].Value);
                int decimalPlaces = int.Parse(match.Groups[5].Value);
                double startValue = double.Parse(match.Groups[6].Value);

                if (!incrementValues.ContainsKey(deviceId))
                {
                    incrementValues[deviceId] = new Dictionary<string, double>();
                }

                if (!incrementValues[deviceId].ContainsKey(functionName))
                {
                    incrementValues[deviceId][functionName] = startValue;
                }
                else
                {
                    double step = Math.Round(min + new Random().NextDouble() * (max - min), decimalPlaces);
                    incrementValues[deviceId][functionName] += step;
                    double currentValue = incrementValues[deviceId][functionName];

                    Trace.WriteLine($"设备: {deviceId}, 功能: {functionName}, 初始值: {startValue}, 递增步长: {step}, 当前值: {currentValue}");
                }

                return incrementValues[deviceId][functionName];
            }

            Match standardMatch = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (standardMatch.Success)
            {
                double min = double.Parse(standardMatch.Groups[1].Value);
                double max = double.Parse(standardMatch.Groups[3].Value);
                int decimalPlaces = int.Parse(standardMatch.Groups[5].Value);

                double generatedValue = Math.Round(min + new Random().NextDouble() * (max - min), decimalPlaces);
                Trace.WriteLine($"生成随机值: {generatedValue}");
                return generatedValue;
            }

            return double.TryParse(valueConfig, out double fixedValue) ? fixedValue : valueConfig;
        }
    }
}