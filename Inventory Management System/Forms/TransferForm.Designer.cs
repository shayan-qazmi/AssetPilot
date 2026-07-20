// Forms/TransferForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class TransferForm
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
            panelTop    = new System.Windows.Forms.Panel();
            flowToolbar = new System.Windows.Forms.FlowLayoutPanel();
            btnTransfer = new System.Windows.Forms.Button();
            btnRefresh  = new System.Windows.Forms.Button();
            btnExport   = new System.Windows.Forms.Button();
            txtSearch   = new System.Windows.Forms.TextBox();
            dgvTransfer = new System.Windows.Forms.DataGridView();
            panelBottom = new System.Windows.Forms.Panel();
            lblRowCount = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTransfer).BeginInit();
            SuspendLayout();

            panelTop.Dock         = System.Windows.Forms.DockStyle.Top;
            panelTop.AutoSize     = true;
            panelTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelTop.MinimumSize  = new System.Drawing.Size(0, 60);
            panelTop.Name         = "panelTop";
            panelTop.BackColor    = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(flowToolbar);

            flowToolbar.Dock          = System.Windows.Forms.DockStyle.Fill;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents  = true;
            flowToolbar.AutoSize      = true;
            flowToolbar.AutoSizeMode  = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding       = new System.Windows.Forms.Padding(8, 8, 8, 8);
            flowToolbar.Name          = "flowToolbar";
            flowToolbar.Controls.Add(btnTransfer);
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(txtSearch);

            btnTransfer.Size   = new System.Drawing.Size(135, 32);
            btnTransfer.Text   = "➕ New Transfer";
            btnTransfer.Name   = "btnTransfer";
            btnTransfer.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnTransfer.Click += btnTransfer_Click;

            btnRefresh.Size   = new System.Drawing.Size(90, 32);
            btnRefresh.Text   = "🔄 Refresh";
            btnRefresh.Name   = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size   = new System.Drawing.Size(90, 32);
            btnExport.Text   = "📥 Export CSV";
            btnExport.Name   = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

            txtSearch.Size            = new System.Drawing.Size(280, 26);
            txtSearch.PlaceholderText = "Search transfers...";
            txtSearch.Name            = "txtSearch";
            txtSearch.Margin          = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged    += txtSearch_TextChanged;

            dgvTransfer.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvTransfer.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransfer.AllowUserToAddRows  = false;
            dgvTransfer.ReadOnly            = true;
            dgvTransfer.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvTransfer.Name                = "dgvTransfer";
            dgvTransfer.Columns.Add("TransferID",    "Transfer ID");
            dgvTransfer.Columns.Add("EquipmentCode", "Equipment Code");
            dgvTransfer.Columns.Add("EquipmentName", "Equipment Name");
            dgvTransfer.Columns.Add("FromEmployee",  "From Employee");
            dgvTransfer.Columns.Add("ToEmployee",    "To Employee");
            dgvTransfer.Columns.Add("FromLocation",  "From Location");
            dgvTransfer.Columns.Add("ToLocation",    "To Location");
            dgvTransfer.Columns.Add("TransferDate",  "Transfer Date");

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
            AutoScroll          = true;
            AutoScrollMinSize   = new System.Drawing.Size(1000, 300);
            Controls.Add(dgvTransfer);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Transfer History";
            Name = "TransferForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTransfer).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Button       btnTransfer;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvTransfer;
    }
}