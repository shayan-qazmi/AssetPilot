// Helpers/Logger.cs
using System;
using System.IO;

namespace Inventory_Management_System.Helpers
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "error.log");

        public static void LogError(Exception ex, string context = "")
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string ctx = string.IsNullOrWhiteSpace(context) ? "" : $" [{context}]";
                    string message = $"[{timeStamp}]{ctx} {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, message);
                }
            }
            catch
            {
                // Fallback to prevent logger itself from throwing
            }
        }
    }
}
