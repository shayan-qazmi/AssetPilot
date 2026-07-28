// Helpers/ConfigurationHelper.cs
using System;
using System.IO;
using System.Text.Json;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Helpers
{
    public static class ConfigurationHelper
    {
        public static void Initialize()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string? connStr = null;

                // 1. Try appsettings.Local.json first
                string localConfigPath = Path.Combine(baseDir, "appsettings.Local.json");
                if (!File.Exists(localConfigPath))
                {
                    localConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
                }

                if (File.Exists(localConfigPath))
                {
                    connStr = ReadConnectionString(localConfigPath);
                }

                // 2. Fall back to appsettings.json if appsettings.Local.json not found
                if (string.IsNullOrWhiteSpace(connStr))
                {
                    string mainConfigPath = Path.Combine(baseDir, "appsettings.json");
                    if (!File.Exists(mainConfigPath))
                    {
                        mainConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    }

                    if (File.Exists(mainConfigPath))
                    {
                        connStr = ReadConnectionString(mainConfigPath);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ConfigurationHelper.Initialize");
            }
        }

        private static string? ReadConnectionString(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connSection) &&
                    connSection.TryGetProperty("DefaultConnection", out var connProp))
                {
                    return connProp.GetString();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ReadConnectionString ({Path.GetFileName(filePath)})");
            }
            return null;
        }
    }
}
