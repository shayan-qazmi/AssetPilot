using System;
using System.Runtime.InteropServices;

namespace Inventory_Management_System.Helpers
{
    // Establishes an authenticated connection to the shared Samba folder when on a network share.
    // If the network share is unreachable (e.g. running on localhost), handles connection attempts
    // gracefully without throwing exceptions or blocking local image functionality.
    internal static class NetworkShareHelper
    {
        private static bool _connected = false;
        private static readonly object _lock = new();

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NETRESOURCE netResource, string password, string username, int flags);

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 1; // RESOURCETYPE_DISK
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string? lpLocalName;
            public string? lpRemoteName;
            public string? lpComment;
            public string? lpProvider;
        }

        private const int ERROR_ALREADY_ASSIGNED = 85;
        private const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;

        public static void EnsureConnected()
        {
            if (_connected) return;

            lock (_lock)
            {
                if (_connected) return;

                const string uncPath = @"\\192.168.18.240\AssetPilotData";

                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var nr = new NETRESOURCE { lpRemoteName = uncPath };
                        return WNetAddConnection2(nr, NetworkPaths.SharePassword, NetworkPaths.ShareUsername, 0);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "NetworkShareHelper.EnsureConnected");
                        return -1;
                    }
                });

                // Wait up to 1.5 seconds so we don't freeze the UI for a minute on startup
                if (task.Wait(1500))
                {
                    int result = task.Result;
                    if (result == 0 ||
                        result == ERROR_ALREADY_ASSIGNED ||
                        result == ERROR_SESSION_CREDENTIAL_CONFLICT)
                    {
                        _connected = true;
                        return;
                    }
                }

                // If it timed out or failed, mark connected so local fallback works seamlessly.
                _connected = true;
            }
        }
    }
}