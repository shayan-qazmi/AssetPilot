// Models/DbConnectionSettings.cs
namespace Inventory_Management_System.Models
{
    public class DbConnectionSettings
    {
        public string Server { get; set; } = "";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "inventory_management";
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public string ServerVersion { get; set; } = "11.8.6-mariadb";
    }
}