using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public partial class DbBootstrapForm : Form
    {
        private TextBox txtServer = null!, txtPort = null!, txtDb = null!, txtUser = null!, txtPass = null!;
        private Label lblStatus = null!;

        public DbBootstrapForm()
        {
            Text = "AssetPilot — Database Connection";
            Size = new Size(400, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            BuildUi();
        }

        private void BuildUi()
        {
            Controls.Add(new Label { Text = "Can't reach the database", Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 20), AutoSize = true });
            Controls.Add(new Label { Text = "Enter connection details to continue.", ForeColor = Color.Gray, Location = new Point(24, 48), AutoSize = true });

            int y = 84;
            txtServer = AddField("Database server (IP)", "192.168.18.240", ref y);
            txtPort = AddField("Port", "3306", ref y);
            txtDb = AddField("Database name", "inventory_management", ref y);
            txtUser = AddField("Username", "", ref y);
            txtPass = AddField("Password", "", ref y, isPassword: true);

            lblStatus = new Label { Location = new Point(24, y + 4), Size = new Size(330, 40), ForeColor = Color.Firebrick, Text = "" };
            Controls.Add(lblStatus);
            y += 50;

            var btnTest = new Button { Text = "Test connection", Location = new Point(24, y), Size = new Size(160, 34) };
            btnTest.Click += BtnTest_Click;
            Controls.Add(btnTest);

            var btnSave = new Button { Text = "Save and continue", Location = new Point(194, y), Size = new Size(160, 34) };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ClientSize = new Size(400, y + 70);
        }

        private TextBox AddField(string label, string defaultVal, ref int y, bool isPassword = false)
        {
            Controls.Add(new Label { Text = label, Location = new Point(24, y), AutoSize = true });
            y += 20;
            var txt = new TextBox { Location = new Point(24, y), Size = new Size(330, 24), Text = defaultVal, UseSystemPasswordChar = isPassword };
            Controls.Add(txt);
            y += 34;
            return txt;
        }

        private DbConnectionSettings BuildSettingsFromForm(out string password)
        {
            password = txtPass.Text;
            return new DbConnectionSettings
            {
                Server = txtServer.Text.Trim(),
                Port = int.TryParse(txtPort.Text, out var p) ? p : 3306,
                Database = txtDb.Text.Trim(),
                Username = txtUser.Text.Trim(),
                ServerVersion = "11.8.6-mariadb"
            };
        }

        private void BtnTest_Click(object? sender, EventArgs e)
        {
            var settings = BuildSettingsFromForm(out var pwd);
            lblStatus.ForeColor = Color.Gray; lblStatus.Text = "Testing...";
            Application.DoEvents();

            if (DbConnectionSettingsService.TestConnection(settings, pwd, out var error))
            { lblStatus.ForeColor = Color.SeaGreen; lblStatus.Text = "Connection successful."; }
            else
            { lblStatus.ForeColor = Color.Firebrick; lblStatus.Text = "Failed: " + error; }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var settings = BuildSettingsFromForm(out var pwd);
            if (!DbConnectionSettingsService.TestConnection(settings, pwd, out var error))
            { lblStatus.ForeColor = Color.Firebrick; lblStatus.Text = "Can't save — connection still failing: " + error; return; }

            DbConnectionSettingsService.Save(settings, pwd);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}