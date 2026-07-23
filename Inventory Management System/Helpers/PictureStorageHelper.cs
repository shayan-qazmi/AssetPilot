// Helpers/PictureStorageHelper.cs
using System;
using System.Runtime.InteropServices;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Central place for the shared network location where equipment pictures
    /// now live. Every form that saves, loads, or deletes a picture resolves
    /// its relative path (e.g. "Images\Equipment\xxxx.png") against SharedRoot
    /// instead of the local Application.StartupPath, so all office PCs see the
    /// same files.
    /// </summary>
    public static class PictureStorageHelper
    {
        // ── Change this in ONE place if the share ever moves ────────────────
        public const string SharedRoot = @"\\192.168.18.240\QInventoryData";

        private const string ShareUser = "pki";
        private const string SharePassword = "pki@123";

        /// <summary>
        /// Resolves a relative picture path (as stored in the DB) against the
        /// shared network root. Use this everywhere the code used to do
        /// Path.Combine(Application.StartupPath, relativePath).
        /// </summary>
        public static string GetFullPath(string relativePath) =>
            System.IO.Path.Combine(SharedRoot, relativePath);

        /// <summary>
        /// Authenticates this PC's session to the shared folder using the
        /// provided credentials. Call this once, at application startup,
        /// BEFORE any form that touches pictures is shown. Safe to call even
        /// if a connection already exists (existing-connection error is
        /// ignored) — makes this safe to call every time the app launches.
        /// </summary>
        public static void ConnectToShare()
        {
            var nr = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpRemoteName = SharedRoot,
            };

            int result = WNetAddConnection2(ref nr, SharePassword, ShareUser, 0);

            // 0 = success, 1219 = ERROR_SESSION_CREDENTIAL_CONFLICT (already
            // connected under different creds — usually fine to ignore),
            // 85   = ERROR_ALREADY_ASSIGNED. Anything else, surface it once.
            if (result != 0 && result != 1219 && result != 85)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Could not connect to the shared picture folder ({SharedRoot}).\n\n" +
                    $"Windows error code: {result}\n\n" +
                    "Equipment pictures may not save or load correctly on this PC until this is resolved.",
                    "Shared Folder Connection Warning",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        // ── P/Invoke plumbing ────────────────────────────────────────────────
        private const int RESOURCETYPE_DISK = 1;

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(
            ref NETRESOURCE lpNetResource, string lpPassword, string lpUsername, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }
    }
}