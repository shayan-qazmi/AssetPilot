// Program.cs
using System;
using System.Windows.Forms;
using Inventory_Management_System.Forms;
using Inventory_Management_System.Services;
using QuestPDF.Infrastructure;

namespace Inventory_Management_System
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Make sure a settings file exists (first run after this update
            // migrates the old hardcoded production connection automatically).
            DbConnectionSettingsService.SeedIfMissing(
                "192.168.18.240", 3306, "inventory_management", "ims_user", "DBPassword123!", "11.8.6-mariadb");

            // If the saved connection can't be reached (new machine, wrong
            // IP, DB server down, etc.), show the bootstrap dialog first —
            // nothing else in the app can work without a database anyway.
            if (!CanConnect())
            {
                using var bootstrap = new DbBootstrapForm();
                if (bootstrap.ShowDialog() != DialogResult.OK) return;
            }

            try
            {
                new Services.TransactionService().UpdateStatus();
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogError(ex, "Program.Main (TransactionService.UpdateStatus)");
            }

            using var login = new LoginForm();
            if (login.ShowDialog() == DialogResult.OK)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                Application.Run(new DashboardForm());
            }
        }

        static bool CanConnect()
        {
            try
            {
                var settings = DbConnectionSettingsService.Load();
                var pwd = DbConnectionSettingsService.DecryptPassword(settings);
                return DbConnectionSettingsService.TestConnection(settings, pwd, out _);
            }
            catch
            {
                return false;
            }
        }
    }
}