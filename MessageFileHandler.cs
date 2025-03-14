﻿using System;
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
                                switch (vElement.ValueKind)
                                {
                                    case JsonValueKind.Number:
                                        // 确保数值不丢失小数位
                                        data["v"] = vElement.GetDouble();
                                        break;

                                    case JsonValueKind.True:
                                    case JsonValueKind.False:
                                        data["v"] = vElement.GetBoolean();
                                        break;

                                    case JsonValueKind.String:
                                        data["v"] = vElement.GetString();
                                        break;

                                    default:
                                        // 只有当 `v` 需要递增/随机时，才修改 `v`
                                        string vStr = vElement.GetRawText().Trim('"');
                                        data["v"] = GenerateValue(deviceId, data["m"].ToString(), vStr);
                                        break;
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
            Match standardMatch = Regex.Match(valueConfig, @"\[(\d+(\.\d+)?)-(\d+(\.\d+)?),(\d+)\]");
            if (standardMatch.Success)
            {
                decimal min = decimal.Parse(standardMatch.Groups[1].Value);
                decimal max = decimal.Parse(standardMatch.Groups[3].Value);
                int decimalPlaces = int.Parse(standardMatch.Groups[5].Value);

                decimal generatedValue = Math.Round((decimal)(random.NextDouble()) * (max - min) + min, decimalPlaces, MidpointRounding.AwayFromZero);
                return generatedValue;
            }

            // 默认情况下，不修改 v
            if (double.TryParse(valueConfig, out double fixedValue))
            {
                return fixedValue; // 保持原小数位数，不进行 Math.Round()
            }

            return valueConfig;
        }

    }
}