// Program.cs
using System;
using System.Windows.Forms;
using Inventory_Management_System.Forms;
using QuestPDF.Infrastructure;

namespace Inventory_Management_System
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

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
    }
}