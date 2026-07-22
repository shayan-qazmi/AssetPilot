// Forms/AllocationForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class AllocationForm
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
            btnAdd = new System.Windows.Forms.Button();
            btnApprove = new System.Windows.Forms.Button();
            btnReturn = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            btnRefresh = new System.Windows.Forms.Button();
            btnExport = new System.Windows.Forms.Button();
            btnSelectAll = new System.Windows.Forms.Button();
            txtSearch = new System.Windows.Forms.TextBox();
            dgvAllocation = new System.Windows.Forms.DataGridView();
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
            ((System.ComponentModel.ISupportInitialize)dgvAllocation).BeginInit();
            SuspendLayout();

            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.AutoSize = false;
            panelTop.Height = 60;
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
            flowToolbar.Controls.Add(btnAdd);
            flowToolbar.Controls.Add(btnApprove);
            flowToolbar.Controls.Add(btnReturn);
            flowToolbar.Controls.Add(btnDelete);
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(btnSelectAll);
            flowToolbar.Controls.Add(txtSearch);

            btnAdd.Size = new System.Drawing.Size(100, 32);
            btnAdd.Text = "➕ Allocate";
            btnAdd.Name = "btnAdd";
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnAdd.Click += btnAdd_Click;

            btnApprove.Size = new System.Drawing.Size(80, 32);
            btnApprove.Text = "Approve";
            btnApprove.Name = "btnApprove";
            btnApprove.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnApprove.Click += btnApprove_Click;

            btnReturn.Size = new System.Drawing.Size(80, 32);
            btnReturn.Text = "Return";
            btnReturn.Name = "btnReturn";
            btnReturn.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnReturn.Click += btnReturn_Click;

            btnDelete.Size = new System.Drawing.Size(100, 32);
            btnDelete.Text = "🗑️ Delete";
            btnDelete.Name = "btnDelete";
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnDelete.Click += btnDelete_Click;

            btnRefresh.Size = new System.Drawing.Size(100, 32);
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size = new System.Drawing.Size(100, 32);
            btnExport.Text = "📥 Export";
            btnExport.Name = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

            btnSelectAll.Size = new System.Drawing.Size(90, 32);
            btnSelectAll.Text = "Select All";
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnSelectAll.Click += btnSelectAll_Click;

            txtSearch.Size = new System.Drawing.Size(240, 26);
            txtSearch.PlaceholderText = "Search allocations...";
            txtSearch.Name = "txtSearch";
            txtSearch.Margin = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged += txtSearch_TextChanged;

            dgvAllocation.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvAllocation.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvAllocation.AllowUserToAddRows = false;
            dgvAllocation.ReadOnly = true;
            dgvAllocation.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvAllocation.MultiSelect = true;
            dgvAllocation.Name = "dgvAllocation";
            dgvAllocation.Columns.Add("AllocationID", "ID");
            dgvAllocation.Columns.Add("QRCode", "QR Code");
            dgvAllocation.Columns.Add("EquipmentCode", "Equipment Code");
            dgvAllocation.Columns.Add("EquipmentName", "Equipment Name");
            dgvAllocation.Columns.Add("SerialNumber", "Serial Number");
            dgvAllocation.Columns.Add("Make", "Make");
            dgvAllocation.Columns.Add("Model", "Model");
            dgvAllocation.Columns.Add("Employee", "Employee");
            dgvAllocation.Columns.Add("Location", "Location");
            dgvAllocation.Columns.Add("DateAllocated", "Date Allocated");
            dgvAllocation.Columns.Add("ExpectedReturn", "Expected Return");
            dgvAllocation.Columns.Add("Status", "Status");
            dgvAllocation.Columns.Add("Source", "Source");

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
            AutoScroll = true;
            AutoScrollMinSize = new System.Drawing.Size(1000, 300);
            Controls.Add(dgvAllocation);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Allocation Management";
            Name = "AllocationForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelPagination.ResumeLayout(false);
            panelPagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAllocation).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelPagination;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnApprove;
        private System.Windows.Forms.Button btnReturn;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblPageSize;
        private System.Windows.Forms.ComboBox cmbPageSize;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageIndicator;
        private System.Windows.Forms.Label lblRowCount;
        private System.Windows.Forms.DataGridView dgvAllocation;
    }
}