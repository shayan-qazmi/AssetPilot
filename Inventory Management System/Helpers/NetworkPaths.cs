using System.IO;

namespace Inventory_Management_System.Helpers
{
    // Central place for the shared network folder used to store equipment
    // (and now user, and employee) pictures, so every PC running Q-Inventory
    // reads/writes the same files regardless of which machine originally
    // uploaded them.
    internal static class NetworkPaths
    {
        // Samba share on the Debian DB server (192.168.18.240).
        public const string ShareRoot = @"\\192.168.18.240\QInventoryData";
        public const string ShareUsername = "pki";
        public const string SharePassword = "pki@123";

        public static string EquipmentPicturesDir => Path.Combine(ShareRoot, "Equipment");
        public static string UserPicturesDir => Path.Combine(ShareRoot, "Users");
        public static string EmployeePicturesDir => Path.Combine(ShareRoot, "Employees");

        // Resolves a stored picture_path DB value — which may be an old-style
        // relative path like "Images/Equipment/x.png" from before the shared
        // folder existed, or a new bare filename — to a full UNC path.
        public static string GetEquipmentPictureFullPath(string storedValue)
        {
            string fileName = Path.GetFileName(storedValue);
            return Path.Combine(EquipmentPicturesDir, fileName);
        }

        public static string GetUserPictureFullPath(string storedValue)
        {
            string fileName = Path.GetFileName(storedValue);
            return Path.Combine(UserPicturesDir, fileName);
        }

        public static string GetEmployeePictureFullPath(string storedValue)
        {
            string fileName = Path.GetFileName(storedValue);
            return Path.Combine(EmployeePicturesDir, fileName);
        }
    }
}