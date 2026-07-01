// Forms/DashboardForm.Designer.cs
namespace Inventory_Management_System
{
    partial class DashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelMenu        = new System.Windows.Forms.Panel();
            panelTop         = new System.Windows.Forms.Panel();
            panelDesktop     = new System.Windows.Forms.Panel();
            panelStats       = new System.Windows.Forms.Panel();
            panelContent     = new System.Windows.Forms.Panel();

            lblTitle         = new System.Windows.Forms.Label();
            lblAppBrand      = new System.Windows.Forms.Label();

            btnDashboard     = new System.Windows.Forms.Button();
            btnEquipment     = new System.Windows.Forms.Button();
            btnEmployees     = new System.Windows.Forms.Button();
            btnLocations     = new System.Windows.Forms.Button();
            btnAllocation    = new System.Windows.Forms.Button();
            btnReturns       = new System.Windows.Forms.Button();
            btnTransfer      = new System.Windows.Forms.Button();
            btnDecommission  = new System.Windows.Forms.Button();
            btnLogs          = new System.Windows.Forms.Button();
            btnQRScanner     = new System.Windows.Forms.Button();
            btnReports       = new System.Windows.Forms.Button();
            btnSettings      = new System.Windows.Forms.Button();
            btnLogout        = new System.Windows.Forms.Button();

            panelMenu.SuspendLayout();
            panelTop.SuspendLayout();
            panelDesktop.SuspendLayout();
            SuspendLayout();

            // ── panelMenu (sidebar) ─────────────────────────────────────────
            panelMenu.Dock      = System.Windows.Forms.DockStyle.Left;
            panelMenu.Width     = 200;
            panelMenu.Name      = "panelMenu";
            panelMenu.AutoScroll = true;
            panelMenu.Controls.Add(btnLogout);
            panelMenu.Controls.Add(btnSettings);
            panelMenu.Controls.Add(btnReports);
            panelMenu.Controls.Add(btnQRScanner);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnDecommission);
            panelMenu.Controls.Add(btnTransfer);
            panelMenu.Controls.Add(btnReturns);
            panelMenu.Controls.Add(btnAllocation);
            panelMenu.Controls.Add(btnLocations);
            panelMenu.Controls.Add(btnEmployees);
            panelMenu.Controls.Add(btnEquipment);
            panelMenu.Controls.Add(btnDashboard);
            panelMenu.Controls.Add(lblAppBrand);

            lblAppBrand.Text      = "IMS";
            lblAppBrand.Dock      = System.Windows.Forms.DockStyle.Top;
            lblAppBrand.Height    = 120;
            lblAppBrand.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblAppBrand.Name      = "lblAppBrand";

            btnDashboard.Dock    = System.Windows.Forms.DockStyle.Top;
            btnDashboard.Height  = 46; btnDashboard.Text = "⊞  Dashboard";
            btnDashboard.Name    = "btnDashboard";
            btnDashboard.Click  += btnDashboard_Click;

            btnEquipment.Dock    = System.Windows.Forms.DockStyle.Top;
            btnEquipment.Height  = 46; btnEquipment.Text = "🖥  Equipment";
            btnEquipment.Name    = "btnEquipment";
            btnEquipment.Click  += btnEquipment_Click;

            btnEmployees.Dock    = System.Windows.Forms.DockStyle.Top;
            btnEmployees.Height  = 46; btnEmployees.Text = "👤  Employees";
            btnEmployees.Name    = "btnEmployees";
            btnEmployees.Click  += btnEmployees_Click;

            btnLocations.Dock    = System.Windows.Forms.DockStyle.Top;
            btnLocations.Height  = 46; btnLocations.Text = "📍  Locations";
            btnLocations.Name    = "btnLocations";
            btnLocations.Click  += btnLocations_Click;

            btnAllocation.Dock   = System.Windows.Forms.DockStyle.Top;
            btnAllocation.Height = 46; btnAllocation.Text = "📦  Allocation";
            btnAllocation.Name   = "btnAllocation";
            btnAllocation.Click += btnAllocation_Click;

            btnReturns.Dock      = System.Windows.Forms.DockStyle.Top;
            btnReturns.Height    = 46; btnReturns.Text = "↩  Returns";
            btnReturns.Name      = "btnReturns";
            btnReturns.Click    += btnReturns_Click;

            btnTransfer.Dock     = System.Windows.Forms.DockStyle.Top;
            btnTransfer.Height   = 46; btnTransfer.Text = "🔀  Transfer";
            btnTransfer.Name     = "btnTransfer";
            btnTransfer.Click   += btnTransfer_Click;

            btnDecommission.Dock   = System.Windows.Forms.DockStyle.Top;
            btnDecommission.Height = 46; btnDecommission.Text = "🗑  Decommission";
            btnDecommission.Name   = "btnDecommission";
            btnDecommission.Click += btnDecommission_Click;

            btnLogs.Dock         = System.Windows.Forms.DockStyle.Top;
            btnLogs.Height       = 46; btnLogs.Text = "📋  Logs";
            btnLogs.Name         = "btnLogs";
            btnLogs.Click       += btnLogs_Click;

            btnQRScanner.Dock    = System.Windows.Forms.DockStyle.Top;
            btnQRScanner.Height  = 46; btnQRScanner.Text = "📷  QR Scanner";
            btnQRScanner.Name    = "btnQRScanner";
            btnQRScanner.Click  += btnQRScanner_Click;

            btnReports.Dock      = System.Windows.Forms.DockStyle.Top;
            btnReports.Height    = 46; btnReports.Text = "📊  Reports";
            btnReports.Name      = "btnReports";
            btnReports.Click    += btnReports_Click;

            btnSettings.Dock     = System.Windows.Forms.DockStyle.Top;
            btnSettings.Height   = 46; btnSettings.Text = "⚙  Settings";
            btnSettings.Name     = "btnSettings";
            btnSettings.Click   += btnSettings_Click;

            btnLogout.Dock       = System.Windows.Forms.DockStyle.Bottom;
            btnLogout.Height     = 46; btnLogout.Text = "🚪  Logout";
            btnLogout.Name       = "btnLogout";
            btnLogout.UseVisualStyleBackColor              = false;
            btnLogout.FlatStyle                           = System.Windows.Forms.FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize           = 0;
            btnLogout.BackColor                           = System.Drawing.Color.Firebrick;
            btnLogout.ForeColor                           = System.Drawing.Color.White;
            btnLogout.FlatAppearance.MouseOverBackColor   = System.Drawing.Color.DarkRed;
            btnLogout.FlatAppearance.MouseDownBackColor   = System.Drawing.Color.Maroon;
            btnLogout.Click += btnLogout_Click;

            // ── panelTop ────────────────────────────────────────────────────
            panelTop.Dock   = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 56;
            panelTop.Name   = "panelTop";
            panelTop.Controls.Add(lblTitle);

            lblTitle.Text      = "Q-Inventory";
            lblTitle.AutoSize  = false;
            lblTitle.Dock      = System.Windows.Forms.DockStyle.Fill;
            lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblTitle.Font      = new System.Drawing.Font(lblTitle.Font.FontFamily, 20F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(255, 255, 224);
            lblTitle.Name      = "lblTitle";

            // ── panelDesktop ────────────────────────────────────────────────
            panelDesktop.Dock = System.Windows.Forms.DockStyle.Fill;
            panelDesktop.Name = "panelDesktop";
            panelDesktop.Controls.Add(panelContent);
            panelDesktop.Controls.Add(panelStats);

            // ── panelStats — compact single row ─────────────────────────────
            panelStats.Dock    = System.Windows.Forms.DockStyle.Top;
            panelStats.Height  = 110;
            panelStats.Name    = "panelStats";
            panelStats.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);

            // ── panelContent ────────────────────────────────────────────────
            panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            panelContent.Name = "panelContent";

            // ── Form ────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1400, 860);
            Controls.Add(panelDesktop);
            Controls.Add(panelTop);
            Controls.Add(panelMenu);
            Name        = "DashboardForm";
            Text        = "Q-Inventory";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;

            panelMenu.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelDesktop.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel  panelMenu;
        private System.Windows.Forms.Panel  panelTop;
        private System.Windows.Forms.Panel  panelDesktop;
        private System.Windows.Forms.Panel  panelStats;
        private System.Windows.Forms.Panel  panelContent;
        private System.Windows.Forms.Label  lblTitle;
        private System.Windows.Forms.Label  lblAppBrand;
        private System.Windows.Forms.Button btnDashboard;
        private System.Windows.Forms.Button btnEquipment;
        private System.Windows.Forms.Button btnEmployees;
        private System.Windows.Forms.Button btnLocations;
        private System.Windows.Forms.Button btnAllocation;
        private System.Windows.Forms.Button btnReturns;
        private System.Windows.Forms.Button btnTransfer;
        private System.Windows.Forms.Button btnDecommission;
        private System.Windows.Forms.Button btnLogs;
        private System.Windows.Forms.Button btnQRScanner;
        private System.Windows.Forms.Button btnReports;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnLogout;
    }
}