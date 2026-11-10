// Forms/LogsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class LogsForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelTop           = new System.Windows.Forms.Panel();
            flowToolbar        = new System.Windows.Forms.FlowLayoutPanel();
            btnRefresh         = new System.Windows.Forms.Button();
            btnExport          = new System.Windows.Forms.Button();
            txtSearch          = new System.Windows.Forms.TextBox();
            dgvLogs            = new System.Windows.Forms.DataGridView();
            panelBottom        = new System.Windows.Forms.Panel();
            panelPagination    = new System.Windows.Forms.Panel();
            lblRowCount        = new System.Windows.Forms.Label();
            lblPageSize        = new System.Windows.Forms.Label();
            cmbPageSize        = new System.Windows.Forms.ComboBox();
            btnPrevPage        = new System.Windows.Forms.Button();
            lblPageIndicator   = new System.Windows.Forms.Label();
            btnNextPage        = new System.Windows.Forms.Button();

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            panelBottom.SuspendLayout();
            panelPagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).BeginInit();
            SuspendLayout();

            // ── panelTop ────────────────────────────────────────────────────
            panelTop.Dock         = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.Name         = "panelTop";
            panelTop.Controls.Add(flowToolbar);

            // ── flowToolbar ──────────────────────────────────────────────────
            flowToolbar.Dock          = System.Windows.Forms.DockStyle.Top;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents  = true;
            flowToolbar.AutoSize      = true;
            flowToolbar.AutoSizeMode  = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding       = new System.Windows.Forms.Padding(8, 8, 8, 8);
            flowToolbar.Name          = "flowToolbar";
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(txtSearch);

            btnRefresh.Size   = new System.Drawing.Size(100, 32);
            btnRefresh.Text   = "🔄 Refresh";
            btnRefresh.Name   = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size   = new System.Drawing.Size(100, 32);
            btnExport.Text   = "📥 Export";
            btnExport.Name   = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

            txtSearch.Size            = new System.Drawing.Size(300, 26);
            txtSearch.PlaceholderText = "Search by equipment, action, user…";
            txtSearch.Name            = "txtSearch";
            txtSearch.Margin          = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged    += txtSearch_TextChanged;

            // ── Additional toolbar controls ───────────────────────────────────────
            btnReset = new System.Windows.Forms.Button();
            lblLiveStatus = new System.Windows.Forms.Label();
            cmbDensity = new System.Windows.Forms.ComboBox();
            // Initialize btnReset
            btnReset.Size = new System.Drawing.Size(100, 32);
            btnReset.Text = "🔄 Reset";
            btnReset.Name = "btnReset";
            btnReset.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnReset.Click += btnReset_Click;
            // Initialize lblLiveStatus
            lblLiveStatus.AutoSize = true;
            lblLiveStatus.ForeColor = System.Drawing.Color.FromArgb(0, 128, 0);
            lblLiveStatus.Text = "● Live – updated just now";
            lblLiveStatus.Name = "lblLiveStatus";
            lblLiveStatus.Margin = new System.Windows.Forms.Padding(12, 8, 0, 0);
            // Initialize cmbDensity
            cmbDensity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbDensity.Items.AddRange(new object[] { "Compact", "Comfortable", "Cozy" });
            cmbDensity.SelectedIndex = 1;
            cmbDensity.Name = "cmbDensity";
            cmbDensity.Margin = new System.Windows.Forms.Padding(8, 4, 0, 0);
            cmbDensity.Size = new System.Drawing.Size(120, 27);
            // Add new controls to toolbar
            flowToolbar.Controls.Add(btnReset);
            flowToolbar.Controls.Add(lblLiveStatus);
            flowToolbar.Controls.Add(cmbDensity);

            // ── Filter row controls ───────────────────────────────────────────────
            flowFilterRow = new System.Windows.Forms.FlowLayoutPanel();
            btnFilterAll = new System.Windows.Forms.Button();
            btnFilterCreated = new System.Windows.Forms.Button();
            btnFilterUpdated = new System.Windows.Forms.Button();
            btnFilterAllocation = new System.Windows.Forms.Button();
            btnFilterCheckInOut = new System.Windows.Forms.Button();
            btnFilterTransfers = new System.Windows.Forms.Button();
            btnFilterDecommission = new System.Windows.Forms.Button();
            btnFilterDeleted = new System.Windows.Forms.Button();
            cmbFilterUser = new System.Windows.Forms.ComboBox();
            cmbFilterDate = new System.Windows.Forms.ComboBox();

            // flowFilterRow properties
            flowFilterRow.Dock = System.Windows.Forms.DockStyle.Top;
            flowFilterRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowFilterRow.WrapContents = true;
            flowFilterRow.AutoSize = true;
            flowFilterRow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowFilterRow.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            flowFilterRow.Name = "flowFilterRow";

            // Initialize filter pills
            btnFilterAll.Size = new System.Drawing.Size(80, 28);
            btnFilterAll.Text = "All";
            btnFilterAll.Name = "btnFilterAll";
            btnFilterAll.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterAll.Click += btnFilterAll_Click;

            btnFilterCreated.Size = new System.Drawing.Size(80, 28);
            btnFilterCreated.Text = "Created";
            btnFilterCreated.Name = "btnFilterCreated";
            btnFilterCreated.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterCreated.Click += btnFilterCreated_Click;

            btnFilterUpdated.Size = new System.Drawing.Size(80, 28);
            btnFilterUpdated.Text = "Updated";
            btnFilterUpdated.Name = "btnFilterUpdated";
            btnFilterUpdated.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterUpdated.Click += btnFilterUpdated_Click;

            btnFilterAllocation.Size = new System.Drawing.Size(80, 28);
            btnFilterAllocation.Text = "Allocation";
            btnFilterAllocation.Name = "btnFilterAllocation";
            btnFilterAllocation.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterAllocation.Click += btnFilterAllocation_Click;

            btnFilterCheckInOut.Size = new System.Drawing.Size(110, 28);
            btnFilterCheckInOut.Text = "Check In/Out";
            btnFilterCheckInOut.Name = "btnFilterCheckInOut";
            btnFilterCheckInOut.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterCheckInOut.Click += btnFilterCheckInOut_Click;

            btnFilterTransfers.Size = new System.Drawing.Size(80, 28);
            btnFilterTransfers.Text = "Transfers";
            btnFilterTransfers.Name = "btnFilterTransfers";
            btnFilterTransfers.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterTransfers.Click += btnFilterTransfers_Click;

            btnFilterDecommission.Size = new System.Drawing.Size(118, 28);
            btnFilterDecommission.Text = "Decommission";
            btnFilterDecommission.Name = "btnFilterDecommission";
            btnFilterDecommission.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterDecommission.Click += btnFilterDecommission_Click;

            btnFilterDeleted.Size = new System.Drawing.Size(80, 28);
            btnFilterDeleted.Text = "Deleted";
            btnFilterDeleted.Name = "btnFilterDeleted";
            btnFilterDeleted.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnFilterDeleted.Click += btnFilterDeleted_Click;

            // Add pills to flowFilterRow
            flowFilterRow.Controls.Add(btnFilterAll);
            flowFilterRow.Controls.Add(btnFilterCreated);
            flowFilterRow.Controls.Add(btnFilterUpdated);
            flowFilterRow.Controls.Add(btnFilterAllocation);
            flowFilterRow.Controls.Add(btnFilterCheckInOut);
            flowFilterRow.Controls.Add(btnFilterTransfers);
            flowFilterRow.Controls.Add(btnFilterDecommission);
            flowFilterRow.Controls.Add(btnFilterDeleted);

            // User filter combo
            cmbFilterUser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbFilterUser.Name = "cmbFilterUser";
            cmbFilterUser.Margin = new System.Windows.Forms.Padding(12, 4, 0, 0);
            cmbFilterUser.Size = new System.Drawing.Size(150, 27);
            cmbFilterUser.SelectedIndexChanged += cmbFilterUser_SelectedIndexChanged;
            flowFilterRow.Controls.Add(cmbFilterUser);

            // Date filter combo
            cmbFilterDate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbFilterDate.Name = "cmbFilterDate";
            cmbFilterDate.Margin = new System.Windows.Forms.Padding(12, 4, 0, 0);
            cmbFilterDate.Size = new System.Drawing.Size(140, 27);
            cmbFilterDate.Items.AddRange(new object[]{ "Last 7 days", "Last 30 days", "All time", "Custom..." });
            cmbFilterDate.SelectedIndex = 2; // default: All time
            cmbFilterDate.SelectedIndexChanged += cmbFilterDate_SelectedIndexChanged;
            flowFilterRow.Controls.Add(cmbFilterDate);

            // Add filter row to panelTop
            panelTop.Controls.Add(flowFilterRow);

            // ── panelBottom ───────────────────────────────────────────────

            // ── panelBottom ───────────────────────────────────────────────
            panelBottom.Dock      = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height    = 40;
            panelBottom.Name      = "panelBottom";
            panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelBottom.Controls.Add(lblRowCount);
            panelBottom.Controls.Add(panelPagination);

            lblRowCount.Dock      = System.Windows.Forms.DockStyle.Left;
            lblRowCount.Width     = 260;
            lblRowCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblRowCount.Text      = "Showing 0 records";
            lblRowCount.Name      = "lblRowCount";
            lblRowCount.Padding   = new System.Windows.Forms.Padding(8, 0, 0, 0);

            panelPagination.Dock   = System.Windows.Forms.DockStyle.Right;
            panelPagination.Width  = 420;
            panelPagination.Name   = "panelPagination";
            panelPagination.Controls.Add(lblPageSize);
            panelPagination.Controls.Add(cmbPageSize);
            panelPagination.Controls.Add(btnPrevPage);
            panelPagination.Controls.Add(lblPageIndicator);
            panelPagination.Controls.Add(btnNextPage);

            lblPageSize.AutoSize = true;
            lblPageSize.Location = new System.Drawing.Point(4, 12);
            lblPageSize.Name     = "lblPageSize";
            lblPageSize.Text     = "Page size:";

            cmbPageSize.Location              = new System.Drawing.Point(78, 8);
            cmbPageSize.Size                  = new System.Drawing.Size(60, 27);
            cmbPageSize.Name                  = "cmbPageSize";
            cmbPageSize.DropDownStyle         = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "100" });
            cmbPageSize.SelectedIndex         = 1;
            cmbPageSize.SelectedIndexChanged += cmbPageSize_SelectedIndexChanged;

            btnPrevPage.Location = new System.Drawing.Point(148, 6);
            btnPrevPage.Size     = new System.Drawing.Size(70, 26);
            btnPrevPage.Text     = "◀ Prev";
            btnPrevPage.Name     = "btnPrevPage";
            btnPrevPage.Click   += btnPrevPage_Click;

            lblPageIndicator.AutoSize  = false;
            lblPageIndicator.Location  = new System.Drawing.Point(226, 6);
            lblPageIndicator.Size      = new System.Drawing.Size(100, 26);
            lblPageIndicator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblPageIndicator.Name      = "lblPageIndicator";
            lblPageIndicator.Text      = "Page 1 of 1";

            btnNextPage.Location = new System.Drawing.Point(334, 6);
            btnNextPage.Size     = new System.Drawing.Size(70, 26);
            btnNextPage.Text     = "Next ▶";
            btnNextPage.Name     = "btnNextPage";
            btnNextPage.Click   += btnNextPage_Click;

            // ── dgvLogs ───────────────────────────────────────────────────
            dgvLogs.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvLogs.AllowUserToAddRows  = false;
            dgvLogs.ReadOnly            = true;
            dgvLogs.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvLogs.Name                = "dgvLogs";

            var colId = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "ID", HeaderText = "Log ID", FillWeight = 6 };
            var colEq = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Equipment", HeaderText = "Equipment Code", FillWeight = 12 };
            var colName = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 12 };
            var colMake = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Make", HeaderText = "Make", FillWeight = 10 };
            var colQr = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "QRCode", HeaderText = "QR Code", FillWeight = 10 };
            var colAct = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", FillWeight = 18 };
            var colUser = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "User", HeaderText = "Changed By", FillWeight = 14 };
            var colOld = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "OldValue", HeaderText = "Previous Value", FillWeight = 18 };
            var colNew = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "NewValue", HeaderText = "New Value", FillWeight = 18 };
            var colDate = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Timestamp", FillWeight = 14 };

            dgvLogs.Columns.AddRange(colId, colEq, colName, colMake, colQr, colAct, colUser, colOld, colNew, colDate);
            dgvLogs.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvLogs_ColumnHeaderMouseClick);
            dgvLogs.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvLogs_CellDoubleClick);

            // ── LogsForm ──────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
          
            Controls.Add(dgvLogs);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Audit Logs";
            Name = "LogsForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelPagination.ResumeLayout(false);
            panelPagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Panel        panelPagination;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.Button       btnReset;
        private System.Windows.Forms.Label        lblLiveStatus;
        private System.Windows.Forms.ComboBox     cmbDensity;
        private System.Windows.Forms.FlowLayoutPanel flowFilterRow;
        private System.Windows.Forms.Button       btnFilterAll;
        private System.Windows.Forms.Button       btnFilterCreated;
        private System.Windows.Forms.Button       btnFilterUpdated;
        private System.Windows.Forms.Button       btnFilterAllocation;
        private System.Windows.Forms.Button       btnFilterCheckInOut;
        private System.Windows.Forms.Button       btnFilterTransfers;
        private System.Windows.Forms.Button       btnFilterDecommission;
        private System.Windows.Forms.Button       btnFilterDeleted;
        private System.Windows.Forms.ComboBox     cmbFilterUser;
        private System.Windows.Forms.ComboBox     cmbFilterDate;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblPageSize;
        private System.Windows.Forms.ComboBox     cmbPageSize;
        private System.Windows.Forms.Button       btnPrevPage;
        private System.Windows.Forms.Button       btnNextPage;
        private System.Windows.Forms.Label        lblPageIndicator;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvLogs;
    }
}
