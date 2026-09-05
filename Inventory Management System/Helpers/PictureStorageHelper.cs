using System;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Central place for picture storage path resolution. Automatically resolves
    /// against network share or local AssetPilotData fallback directory.
    /// </summary>
    public static class PictureStorageHelper
    {
        public static string SharedRoot => NetworkPaths.ShareRoot;

        public static string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return "";
            if (System.IO.Path.IsPathRooted(relativePath) && System.IO.File.Exists(relativePath)) return relativePath;
            return System.IO.Path.Combine(SharedRoot, relativePath);
        }

        public static void ConnectToShare()
        {
            NetworkShareHelper.EnsureConnected();
        }
    }
}