using System;
using System.IO;

namespace Inventory_Management_System.Helpers
{
    // Central place for equipment, user, and employee picture storage.
    // Automatically uses shared network directory when available, or seamlessly
    // falls back to a local QInventoryData folder when running on localhost / offline.
    internal static class NetworkPaths
    {
        public const string ShareUsername = "pki";
        public const string SharePassword = "pki@123";

        private static string? _resolvedShareRoot;
        private static readonly object _rootLock = new();

        public static string ShareRoot
        {
            get
            {
                if (_resolvedShareRoot != null) return _resolvedShareRoot;
                lock (_rootLock)
                {
                    if (_resolvedShareRoot != null) return _resolvedShareRoot;

                    const string configuredUnc = @"\\192.168.18.240\AssetPilotData";
                    const string legacyUnc = @"\\192.168.18.240\QInventoryData";

                    if (!string.IsNullOrWhiteSpace(configuredUnc) && configuredUnc.StartsWith(@"\\"))
                    {
                        try
                        {
                            NetworkShareHelper.EnsureConnected();
                            if (Directory.Exists(configuredUnc))
                            {
                                _resolvedShareRoot = configuredUnc;
                                return _resolvedShareRoot;
                            }
                            if (Directory.Exists(legacyUnc))
                            {
                                _resolvedShareRoot = legacyUnc;
                                return _resolvedShareRoot;
                            }
                        }
                        catch { /* Network UNC unavailable, fall back to local folder */ }
                    }

                    string primaryLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetPilotData");
                    string legacyLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QInventoryData");
                    string localFolder = (!Directory.Exists(primaryLocal) && Directory.Exists(legacyLocal)) ? legacyLocal : primaryLocal;

                    try { Directory.CreateDirectory(localFolder); } catch { }
                    _resolvedShareRoot = localFolder;
                    return _resolvedShareRoot;
                }
            }
        }

        public static string EquipmentPicturesDir
        {
            get
            {
                var dir = Path.Combine(ShareRoot, "Equipment");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static string UserPicturesDir
        {
            get
            {
                var dir = Path.Combine(ShareRoot, "Users");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static string EmployeePicturesDir
        {
            get
            {
                var dir = Path.Combine(ShareRoot, "Employees");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static string GetEquipmentPictureFullPath(string storedValue) =>
            ResolvePictureFullPath(storedValue, EquipmentPicturesDir, "Equipment");

        public static string GetUserPictureFullPath(string storedValue) =>
            ResolvePictureFullPath(storedValue, UserPicturesDir, "Users");

        public static string GetEmployeePictureFullPath(string storedValue) =>
            ResolvePictureFullPath(storedValue, EmployeePicturesDir, "Employees");

        private static string ResolvePictureFullPath(string storedValue, string categoryDir, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return "";

            // 1. If absolute path already exists as-is
            if (Path.IsPathRooted(storedValue) && File.Exists(storedValue))
                return storedValue;

            string fileName = Path.GetFileName(storedValue);

            // 2. Check primary directory (ShareRoot/category)
            string primary = Path.Combine(categoryDir, fileName);
            if (File.Exists(primary)) return primary;

            // 3. Fallback candidates (local AppBase subfolders, legacy relative paths)
            string appBase = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new[]
            {
                Path.Combine(appBase, "AssetPilotData", categoryName, fileName),
                Path.Combine(appBase, "QInventoryData", categoryName, fileName),
                Path.Combine(appBase, "Images", categoryName, fileName),
                Path.Combine(appBase, "Images", fileName),
                Path.Combine(appBase, storedValue)
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }

            return primary;
        }
    }
}