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
        private static Dictionary<string, Dictionary<string, decimal>> incrementValues = new Dictionary<string, Dictionary<string, decimal>>();

        public static bool IsMessageFileEmpty()
        {
            string messageFile = "sim_message.txt";
            return !File.Exists(messageFile) || string.IsNullOrWhiteSpace(File.ReadAllText(messageFile));
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

            jsonDict["ts"] = currentTimestamp;

            if (jsonDict.ContainsKey("devs") && jsonDict["devs"] is JsonElement devsElement)
            {
                var devices = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(devsElement.GetRawText());

                foreach (var dev in devices)
                {
                    string deviceId = dev["dev"].ToString();
                    dev["ts"] = currentTimestamp;

                    if (dev.ContainsKey("d") && dev["d"] is JsonElement dElement)
                    {
                        var deviceData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dElement.GetRawText());

                        foreach (var data in deviceData)
                        {
                            data["ts"] = currentTimestamp;

                            if (data.ContainsKey("v") && data["v"] is JsonElement vElement)
                            {
                                string rawValue = vElement.GetRawText().Trim('"');
                                data["v"] = ProcessValueFormat(deviceId, data["m"].ToString(), rawValue);
                            }
                        }

                        dev["d"] = deviceData;
                    }
                }

                jsonDict["devs"] = devices;
            }

            return JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// 处理不同类型的值，确保：
        /// 1. 递增/随机数公式正确解析
        /// 2. 纯数字作为数值，不加引号
        /// 3. 布尔值作为布尔类型，不加引号
        /// 4. 其他内容保持字符串类型
        /// </summary>
        private static object ProcessValueFormat(string deviceId, string functionName, string value)
        {
            // 1️⃣ 先检测是否是递增/随机数公式
            if (IsIncrementOrRandomFormula(value))
            {
                return GenerateValue(deviceId, functionName, value);
            }

            // 2️⃣ 尝试解析数值
            if (double.TryParse(value, out double numericValue))
            {
                return numericValue; // 纯数字，直接返回数值
            }

            // 3️⃣ 尝试解析布尔值
            if (bool.TryParse(value, out bool boolValue))
            {
                return boolValue; // 解析为布尔值
            }

            // 4️⃣ 其他情况（含字母、非标准格式等）作为字符串处理
            return value;
        }

        /// <summary>
        /// 检测是否是随机数或递增数公式，例如：
        /// - [a-b,c] 随机数模式
        /// - [a-b,c,d] 递增模式
        /// </summary>
        private static bool IsIncrementOrRandomFormula(string value)
        {
            return Regex.IsMatch(value, @"^\[\d+(\.\d+)?-\d+(\.\d+)?,\d+(,\d+(\.\d+)?)?\]$");
        }

        private static object GenerateValue(string deviceId, string functionName, string valueConfig)
        {
            Random random = new Random();

            // 解析递增模式: [a-b,c,d]
            Match incrementMatch = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+),(\d+(\.\d+)?)\]");
            if (incrementMatch.Success)
            {
                decimal min = decimal.Parse(incrementMatch.Groups[1].Value);
                decimal max = decimal.Parse(incrementMatch.Groups[3].Value);
                int decimalPlaces = int.Parse(incrementMatch.Groups[5].Value);
                decimal startValue = decimal.Parse(incrementMatch.Groups[6].Value);

                if (!incrementValues.ContainsKey(deviceId))
                {
                    incrementValues[deviceId] = new Dictionary<string, decimal>();
                }

                if (!incrementValues[deviceId].ContainsKey(functionName))
                {
                    incrementValues[deviceId][functionName] = startValue;
                }
                else
                {
                    decimal step = Math.Round((decimal)(random.NextDouble()) * (max - min) + min, decimalPlaces);
                    incrementValues[deviceId][functionName] = Math.Round(incrementValues[deviceId][functionName] + step, decimalPlaces, MidpointRounding.AwayFromZero);
                }

                return incrementValues[deviceId][functionName];
            }

            // 解析随机数模式: [a-b,c]
            Match randomMatch = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (randomMatch.Success)
            {
                decimal min = decimal.Parse(randomMatch.Groups[1].Value);
                decimal max = decimal.Parse(randomMatch.Groups[3].Value);
                int decimalPlaces = int.Parse(randomMatch.Groups[5].Value);

                decimal generatedValue = Math.Round((decimal)(random.NextDouble()) * (max - min) + min, decimalPlaces, MidpointRounding.AwayFromZero);
                return generatedValue;
            }

            // 默认情况下，返回原始值
            return valueConfig;
        }
    }
}
