// Helpers/CategoryStore.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Single source of truth for equipment categories, backed by a JSON file
    /// at %AppData%\QInventory\categories.json. Used by SettingsForm (manage),
    /// AddEditEquipmentForm (dropdown when adding/editing equipment), and
    /// EquipmentForm (the "All Categories" filter dropdown).
    /// </summary>
    public static class CategoryStore
    {
        private static string FilePath
        {
            get
            {
                string primary = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssetPilot", "categories.json");
                string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QInventory", "categories.json");
                if (!File.Exists(primary) && File.Exists(legacy)) return legacy;
                return primary;
            }
        }

        private static readonly List<string> Defaults = new()
        {
            "IT Equipment", "Furniture", "Vehicle", "Accessory", "Other"
        };

        public static List<string> GetAll()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    return list.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
                }

                Save(Defaults);
                return Defaults.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch
            {
                return Defaults.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        public static bool Add(string name, out string? error)
        {
            error = null;
            name = (name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Category name cannot be empty.";
                return false;
            }

            var list = GetAll();
            if (list.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                error = "That category already exists.";
                return false;
            }

            list.Add(name);
            Save(list);
            return true;
        }

        public static void Delete(string name)
        {
            var list = GetAll();
            list.RemoveAll(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
            Save(list);
        }

        private static void Save(List<string> list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}