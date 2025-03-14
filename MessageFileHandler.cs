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
                                if (vElement.ValueKind == JsonValueKind.True || vElement.ValueKind == JsonValueKind.False)
                                {
                                    // 原始 JSON 中 true/false 直接按布尔值保留
                                    data["v"] = vElement.GetBoolean();
                                }
                                else if (vElement.ValueKind == JsonValueKind.String)
                                {
                                    // 只要 JSON 原文中是字符串，就保持字符串格式
                                    data["v"] = vElement.GetString();
                                }
                                else
                                {
                                    // 其他情况（如数值、对象等）正常解析
                                    string vStr = vElement.GetRawText().Trim('"');
                                    data["v"] = GenerateValue(deviceId, data["m"].ToString(), vStr);
                                }
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
            Random random = new Random();

            // 解析递增模式: [a-b,c,d]（步长为小数）
            Match match = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+),(\d+(\.\d+)?)\]");
            if (match.Success)
            {
                decimal min = decimal.Parse(match.Groups[1].Value);
                decimal max = decimal.Parse(match.Groups[3].Value);
                int decimalPlaces = int.Parse(match.Groups[5].Value);
                decimal startValue = decimal.Parse(match.Groups[6].Value);

                // 计算 min 和 max 的小数位数
                int minDecimalPlaces = match.Groups[1].Value.Contains(".") ? match.Groups[1].Value.Split('.')[1].Length : 0;
                int maxDecimalPlaces = match.Groups[3].Value.Contains(".") ? match.Groups[3].Value.Split('.')[1].Length : 0;
                int requiredDecimalPlaces = Math.Max(minDecimalPlaces, maxDecimalPlaces);

                // 校验: decimalPlaces 不能小于 min/max 的小数位数
                if (decimalPlaces < requiredDecimalPlaces)
                {
                    throw new ArgumentException($"无效的格式: 指定的小数位数 {decimalPlaces} 不能小于 a({min}) 或 b({max}) 的实际小数位数 {requiredDecimalPlaces}");
                }

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
                    // 生成随机步长（支持小数）
                    decimal step = Math.Round((decimal)(random.NextDouble()) * (max - min) + min, decimalPlaces);
                    incrementValues[deviceId][functionName] = Math.Round(incrementValues[deviceId][functionName] + step, decimalPlaces, MidpointRounding.AwayFromZero);
                }

                decimal currentValue = incrementValues[deviceId][functionName];

                Trace.WriteLine($"设备: {deviceId}, 功能: {functionName}, 初始值: {startValue}, 递增步长: {min}-{max}, 当前值: {currentValue}");
                return currentValue;
            }

            // 解析随机数模式: [a-b,c]
            Match standardMatch = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (standardMatch.Success)
            {
                decimal min = decimal.Parse(standardMatch.Groups[1].Value);
                decimal max = decimal.Parse(standardMatch.Groups[3].Value);
                int decimalPlaces = int.Parse(standardMatch.Groups[5].Value);

                // 计算 min 和 max 的小数位数
                int minDecimalPlaces = standardMatch.Groups[1].Value.Contains(".") ? standardMatch.Groups[1].Value.Split('.')[1].Length : 0;
                int maxDecimalPlaces = standardMatch.Groups[3].Value.Contains(".") ? standardMatch.Groups[3].Value.Split('.')[1].Length : 0;
                int requiredDecimalPlaces = Math.Max(minDecimalPlaces, maxDecimalPlaces);

                // 校验: decimalPlaces 不能小于 min/max 的小数位数
                if (decimalPlaces < requiredDecimalPlaces)
                {
                    throw new ArgumentException($"无效的格式: 指定的小数位数 {decimalPlaces} 不能小于 a({min}) 或 b({max}) 的实际小数位数 {requiredDecimalPlaces}");
                }

                // 生成随机数
                decimal generatedValue = Math.Round((decimal)(random.NextDouble()) * (max - min) + min, decimalPlaces, MidpointRounding.AwayFromZero);
                Trace.WriteLine($"生成随机值: {generatedValue}");
                return generatedValue;
            }

            // 解析固定数值
            return decimal.TryParse(valueConfig, out decimal fixedValue) ? Math.Round(fixedValue, 2, MidpointRounding.AwayFromZero) : valueConfig;
        }
    }
}