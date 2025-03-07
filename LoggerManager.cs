using NLog;

namespace MQTTMessageSenderApp
{
    public static class LoggerManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void LogInfo(string message) => Logger.Info(message);
        public static void LogError(string message) => Logger.Error(message);
    }
}
