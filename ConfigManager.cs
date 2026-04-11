using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MQTTMessageSenderApp
{
    public class ConfigManager
    {
        private static readonly string ConfigFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFileName = "mqtt_config.json";
        private static readonly string ConfigFilePath = Path.Combine(ConfigFolder, ConfigFileName);

        public static void EnsureConfigFolderExists()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }
        }

        public static void SaveConfig(string filename, MqttConfig config)
        {
            EnsureConfigFolderExists();

            string filePath = Path.Combine(ConfigFolder, filename);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(filePath, json);
        }

        public static MqttConfig LoadConfig(string filename)
        {
            string filePath = Path.Combine(ConfigFolder, filename);
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<MqttConfig>(json);
        }

        public static List<string> GetConfigFiles()
        {
            EnsureConfigFolderExists();

            var configFiles = new List<string>();
            if (Directory.Exists(ConfigFolder))
            {
                foreach (var file in Directory.GetFiles(ConfigFolder, "*.json"))
                {
                    configFiles.Add(Path.GetFileName(file));
                }
            }

            return configFiles;
        }

        public static void DeleteConfig(string filename)
        {
            string filePath = Path.Combine(ConfigFolder, filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public static string GetConfigFolder()
        {
            EnsureConfigFolderExists();
            return ConfigFolder;
        }

        // 连接配置相关方法
        public static void SaveConnectionConfig(string filename, ConnectionConfig config)
        {
            EnsureConfigFolderExists();

            string filePath = Path.Combine(ConfigFolder, filename);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(filePath, json);
        }

        public static ConnectionConfig LoadConnectionConfig(string filename)
        {
            string filePath = Path.Combine(ConfigFolder, filename);
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ConnectionConfig>(json);
        }

        public static List<string> GetConnectionConfigFiles()
        {
            EnsureConfigFolderExists();

            var configFiles = new List<string>();
            if (Directory.Exists(ConfigFolder))
            {
                foreach (var file in Directory.GetFiles(ConfigFolder, "connection_*.json"))
                {
                    configFiles.Add(Path.GetFileName(file));
                }
            }

            return configFiles;
        }

        public static void DeleteConnectionConfig(string filename)
        {
            string filePath = Path.Combine(ConfigFolder, filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    public class MqttConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("function_configs")]
        public List<FunctionConfig> FunctionConfigs { get; set; }
    }

    public class FunctionConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("selected")]
        public bool Selected { get; set; }

        [JsonPropertyName("increment")]
        public bool Increment { get; set; }
    }

    public class ConnectionConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("broker")]
        public string Broker { get; set; }

        [JsonPropertyName("port")]
        public string Port { get; set; }

        [JsonPropertyName("keepalive")]
        public string KeepAlive { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; }

        [JsonPropertyName("retain")]
        public bool Retain { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("use_mqtts")]
        public bool UseMqtts { get; set; }

        [JsonPropertyName("ssl_secure")]
        public bool SslSecure { get; set; }

        [JsonPropertyName("sim_message_path")]
        public string SimMessagePath { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
