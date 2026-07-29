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
            panelTop = new System.Windows.Forms.Panel();
            flowToolbar = new System.Windows.Forms.FlowLayoutPanel();
            btnTransfer = new System.Windows.Forms.Button();
            btnRefresh = new System.Windows.Forms.Button();
            btnExport = new System.Windows.Forms.Button();
            txtSearch = new System.Windows.Forms.TextBox();
            dgvTransfer = new System.Windows.Forms.DataGridView();
            panelBottom = new System.Windows.Forms.Panel();
            panelPagination = new System.Windows.Forms.Panel();
            lblRowCount = new System.Windows.Forms.Label();
            lblPageSize = new System.Windows.Forms.Label();
            cmbPageSize = new System.Windows.Forms.ComboBox();
            btnPrevPage = new System.Windows.Forms.Button();
            lblPageIndicator = new System.Windows.Forms.Label();
            btnNextPage = new System.Windows.Forms.Button();

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            panelBottom.SuspendLayout();
            panelPagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTransfer).BeginInit();
            SuspendLayout();

            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.MinimumSize = new System.Drawing.Size(0, 60);
            panelTop.Name = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(flowToolbar);

            flowToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents = true;
            flowToolbar.AutoSize = true;
            flowToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            flowToolbar.Name = "flowToolbar";
            flowToolbar.Controls.Add(btnTransfer);
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(txtSearch);

            btnTransfer.Size = new System.Drawing.Size(135, 32);
            btnTransfer.Text = "➕ New Transfer";
            btnTransfer.Name = "btnTransfer";
            btnTransfer.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnTransfer.Click += btnTransfer_Click;

            btnRefresh.Size = new System.Drawing.Size(90, 32);
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size = new System.Drawing.Size(90, 32);
            btnExport.Text = "📥 Export CSV";
            btnExport.Name = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

            txtSearch.Size = new System.Drawing.Size(280, 26);
            txtSearch.PlaceholderText = "Search transfers...";
            txtSearch.Name = "txtSearch";
            txtSearch.Margin = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged += txtSearch_TextChanged;

            dgvTransfer.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvTransfer.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransfer.AllowUserToAddRows = false;
            dgvTransfer.ReadOnly = true;
            dgvTransfer.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvTransfer.Name = "dgvTransfer";
            dgvTransfer.CellDoubleClick += dgvTransfer_CellDoubleClick;
            dgvTransfer.Columns.Add("AllocationId", "AllocationId"); // hidden, used to identify the row
            dgvTransfer.Columns.Add("QRCode", "QR Code");
            dgvTransfer.Columns.Add("EquipmentCode", "Equipment Code");
            dgvTransfer.Columns.Add("EquipmentName", "Equipment Name");
            dgvTransfer.Columns.Add("CurrentEmployee", "Currently With");
            dgvTransfer.Columns.Add("CurrentLocation", "Current Location");
            dgvTransfer.Columns.Add("AllocatedSince", "Allocated Since");
            dgvTransfer.Columns.Add("Status", "Status");
            dgvTransfer.Columns["AllocationId"].Visible = false;

            panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height = 40;
            panelBottom.Name = "panelBottom";
            panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelBottom.Controls.Add(lblRowCount);
            panelBottom.Controls.Add(panelPagination);

            lblRowCount.Dock = System.Windows.Forms.DockStyle.Left;
            lblRowCount.Width = 260;
            lblRowCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblRowCount.Text = "Showing 0 records";
            lblRowCount.Name = "lblRowCount";
            lblRowCount.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);

            panelPagination.Dock = System.Windows.Forms.DockStyle.Right;
            panelPagination.Width = 420;
            panelPagination.Name = "panelPagination";
            panelPagination.Controls.Add(lblPageSize);
            panelPagination.Controls.Add(cmbPageSize);
            panelPagination.Controls.Add(btnPrevPage);
            panelPagination.Controls.Add(lblPageIndicator);
            panelPagination.Controls.Add(btnNextPage);

            lblPageSize.AutoSize = true;
            lblPageSize.Location = new System.Drawing.Point(4, 12);
            lblPageSize.Name = "lblPageSize";
            lblPageSize.Text = "Page size:";

            cmbPageSize.Location = new System.Drawing.Point(78, 8);
            cmbPageSize.Size = new System.Drawing.Size(60, 27);
            cmbPageSize.Name = "cmbPageSize";
            cmbPageSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "100" });
            cmbPageSize.SelectedIndex = 1;
            cmbPageSize.SelectedIndexChanged += cmbPageSize_SelectedIndexChanged;

            btnPrevPage.Location = new System.Drawing.Point(148, 6);
            btnPrevPage.Size = new System.Drawing.Size(70, 26);
            btnPrevPage.Text = "◀ Prev";
            btnPrevPage.Name = "btnPrevPage";
            btnPrevPage.Click += btnPrevPage_Click;

            lblPageIndicator.AutoSize = false;
            lblPageIndicator.Location = new System.Drawing.Point(226, 6);
            lblPageIndicator.Size = new System.Drawing.Size(100, 26);
            lblPageIndicator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblPageIndicator.Name = "lblPageIndicator";
            lblPageIndicator.Text = "Page 1 of 1";

            btnNextPage.Location = new System.Drawing.Point(334, 6);
            btnNextPage.Size = new System.Drawing.Size(70, 26);
            btnNextPage.Text = "Next ▶";
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Click += btnNextPage_Click;

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 700);
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
            panelPagination.ResumeLayout(false);
            panelPagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTransfer).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelPagination;
        private System.Windows.Forms.Button btnTransfer;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblPageSize;
        private System.Windows.Forms.ComboBox cmbPageSize;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageIndicator;
        private System.Windows.Forms.Label lblRowCount;
        private System.Windows.Forms.DataGridView dgvTransfer;
    }
}