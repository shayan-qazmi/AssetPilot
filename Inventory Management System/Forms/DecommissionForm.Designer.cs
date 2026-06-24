// Forms/DecommissionForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class DecommissionForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelTop        = new System.Windows.Forms.Panel();
            btnDecommission = new System.Windows.Forms.Button();
            btnRefresh      = new System.Windows.Forms.Button();
            btnExport       = new System.Windows.Forms.Button();
            txtSearch       = new System.Windows.Forms.TextBox();
            dgvDecommission = new System.Windows.Forms.DataGridView();
            panelBottom     = new System.Windows.Forms.Panel();
            lblRowCount     = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDecommission).BeginInit();
            SuspendLayout();

            panelTop.Dock      = System.Windows.Forms.DockStyle.Top;
            panelTop.Height    = 60;
            panelTop.Name      = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(btnDecommission);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(txtSearch);

            btnDecommission.Location = new System.Drawing.Point(12, 14);
            btnDecommission.Size     = new System.Drawing.Size(140, 32);
            btnDecommission.Text     = "Decommission Item";
            btnDecommission.Name     = "btnDecommission";
            btnDecommission.Click   += btnDecommission_Click;

            btnRefresh.Location = new System.Drawing.Point(160, 14);
            btnRefresh.Size     = new System.Drawing.Size(90, 32);
            btnRefresh.Text     = "Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            btnExport.Location = new System.Drawing.Point(258, 14);
            btnExport.Size     = new System.Drawing.Size(90, 32);
            btnExport.Text     = "Export CSV";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            txtSearch.Location        = new System.Drawing.Point(410, 16);
            txtSearch.Size            = new System.Drawing.Size(280, 26);
            txtSearch.PlaceholderText = "Search decommissioned items...";
            txtSearch.Name            = "txtSearch";
            txtSearch.TextChanged    += txtSearch_TextChanged;

            dgvDecommission.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvDecommission.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvDecommission.AllowUserToAddRows  = false;
            dgvDecommission.ReadOnly            = true;
            dgvDecommission.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvDecommission.Name                = "dgvDecommission";
            dgvDecommission.Columns.Add("EquipmentCode", "Equipment Code");
            dgvDecommission.Columns.Add("EquipmentName", "Equipment Name");
            dgvDecommission.Columns.Add("Make",          "Make");
            dgvDecommission.Columns.Add("Model",         "Model");
            dgvDecommission.Columns.Add("SerialNumber",  "Serial Number");
            dgvDecommission.Columns.Add("Reason",        "Reason");
            dgvDecommission.Columns.Add("DecommDate",    "Decommission Date");

            panelBottom.Dock      = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height    = 30;
            panelBottom.Name      = "panelBottom";
            panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelBottom.Controls.Add(lblRowCount);

            lblRowCount.AutoSize  = false;
            lblRowCount.Dock      = System.Windows.Forms.DockStyle.Fill;
            lblRowCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblRowCount.Text      = "Showing 0 records";
            lblRowCount.Name      = "lblRowCount";
            lblRowCount.Padding   = new System.Windows.Forms.Padding(8, 0, 0, 0);

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
            Controls.Add(dgvDecommission);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Decommissioned Equipment";
            Name = "DecommissionForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDecommission).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Button       btnDecommission;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvDecommission;
    }
}