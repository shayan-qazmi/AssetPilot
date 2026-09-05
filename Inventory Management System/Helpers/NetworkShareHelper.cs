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
            lock (_lock)
            {
                if (_connected) return;

                const string uncPath = @"\\192.168.18.240\AssetPilotData";

                try
                {
                    var nr = new NETRESOURCE { lpRemoteName = uncPath };
                    int result = WNetAddConnection2(nr, NetworkPaths.SharePassword, NetworkPaths.ShareUsername, 0);

                    if (result == 0 ||
                        result == ERROR_ALREADY_ASSIGNED ||
                        result == ERROR_SESSION_CREDENTIAL_CONFLICT)
                    {
                        _connected = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "NetworkShareHelper.EnsureConnected");
                }

                // If UNC path is not accessible on localhost, mark connected so local fallback works seamlessly.
                _connected = true;
            }
        }
    }
}