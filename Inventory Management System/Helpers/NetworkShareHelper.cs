using System;
using System.Runtime.InteropServices;

namespace Inventory_Management_System.Helpers
{
    // Establishes an authenticated connection to the shared Samba folder so
    // File.* calls against the UNC path work without every PC having to
    // manually map a drive or enter credentials via Explorer first.
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

                var nr = new NETRESOURCE { lpRemoteName = NetworkPaths.ShareRoot };

                int result = WNetAddConnection2(nr, NetworkPaths.SharePassword, NetworkPaths.ShareUsername, 0);

                if (result == 0 ||
                    result == ERROR_ALREADY_ASSIGNED ||
                    result == ERROR_SESSION_CREDENTIAL_CONFLICT)
                {
                    _connected = true;
                    return;
                }

                throw new InvalidOperationException(
                    $"Could not connect to shared folder '{NetworkPaths.ShareRoot}' (error code {result}). " +
                    "Check the server is reachable and network credentials are correct.");
            }
        }
    }
}