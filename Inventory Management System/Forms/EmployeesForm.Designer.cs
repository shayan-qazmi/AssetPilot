// Forms/EmployeesForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class EmployeesForm
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
            panelBottom        = new System.Windows.Forms.Panel();
            panelPagination    = new System.Windows.Forms.Panel();
            btnAdd             = new System.Windows.Forms.Button();
            btnEdit            = new System.Windows.Forms.Button();
            btnDelete          = new System.Windows.Forms.Button();
            btnRefresh         = new System.Windows.Forms.Button();
            btnExport          = new System.Windows.Forms.Button();
            btnImport = new System.Windows.Forms.Button();
            txtSearch          = new System.Windows.Forms.TextBox();
            lblPageSize        = new System.Windows.Forms.Label();
            cmbPageSize        = new System.Windows.Forms.ComboBox();
            btnPrevPage        = new System.Windows.Forms.Button();
            btnNextPage        = new System.Windows.Forms.Button();
            lblPageIndicator   = new System.Windows.Forms.Label();
            lblRowCount        = new System.Windows.Forms.Label();
            dgvEmployees       = new System.Windows.Forms.DataGridView();


            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            panelBottom.SuspendLayout();
            panelPagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEmployees).BeginInit();
            SuspendLayout();

            // ── panelTop ─────────────────────────────────────────────────────
            panelTop.Dock         = System.Windows.Forms.DockStyle.Top;
            panelTop.Height       = 60;
            panelTop.Name         = "panelTop";
            panelTop.Controls.Add(flowToolbar);

            // ── flowToolbar ───────────────────────────────────────────────────
            flowToolbar.Dock          = System.Windows.Forms.DockStyle.Top;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents  = true;
            flowToolbar.AutoSize      = true;
            flowToolbar.AutoSizeMode  = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding       = new System.Windows.Forms.Padding(8, 8, 8, 8);
            flowToolbar.Name          = "flowToolbar";
            flowToolbar.Controls.Add(btnAdd);
            flowToolbar.Controls.Add(btnEdit);
            flowToolbar.Controls.Add(btnDelete);
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(btnImport);
            flowToolbar.Controls.Add(txtSearch);

            btnAdd.Size   = new System.Drawing.Size(90, 32);
            btnAdd.Text   = "➕ Add";
            btnAdd.Name   = "btnAdd";
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnAdd.Click += btnAdd_Click;

            btnEdit.Size   = new System.Drawing.Size(90, 32);
            btnEdit.Text   = "✏️ Edit";
            btnEdit.Name   = "btnEdit";
            btnEdit.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnEdit.Click += btnEdit_Click;

            btnDelete.Size   = new System.Drawing.Size(90, 32);
            btnDelete.Text   = "🗑️ Delete";
            btnDelete.Name   = "btnDelete";
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnDelete.Click += btnDelete_Click;

            btnRefresh.Size   = new System.Drawing.Size(90, 32);
            btnRefresh.Text   = "🔄 Refresh";
            btnRefresh.Name   = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size   = new System.Drawing.Size(90, 32);
            btnExport.Text   = "📥 Export";
            btnExport.Name   = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

     

            // Button property setups (Place next to btnExport setup)
            btnImport.Size = new System.Drawing.Size(90, 32);
            btnImport.Text = "📤 Import";
            btnImport.Name = "btnImport";
            btnImport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnImport.Click += btnImport_Click;

         

            txtSearch.Size            = new System.Drawing.Size(200, 27);
            txtSearch.Name            = "txtSearch";
            txtSearch.PlaceholderText = "Search employees…";
            txtSearch.Margin          = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged    += txtSearch_TextChanged;

            // ── panelBottom ───────────────────────────────────────────────────
            panelBottom.Dock   = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height = 40;
            panelBottom.Name   = "panelBottom";
            panelBottom.Controls.Add(lblRowCount);
            panelBottom.Controls.Add(panelPagination);

            lblRowCount.Dock      = System.Windows.Forms.DockStyle.Left;
            lblRowCount.Width     = 260;
            lblRowCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblRowCount.Name      = "lblRowCount";
            lblRowCount.Text      = "Showing 0 records";
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

            // ── DataGridView ──────────────────────────────────────────────────
            dgvEmployees.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvEmployees.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvEmployees.AllowUserToAddRows  = false;
            dgvEmployees.ReadOnly            = true;
            dgvEmployees.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvEmployees.Name                = "dgvEmployees";
            dgvEmployees.Columns.Add("ID",         "Employee Code");
            dgvEmployees.Columns.Add("Name",       "Name");
            dgvEmployees.Columns.Add("Department", "Designation");
            dgvEmployees.Columns.Add("Email",      "Email");
            dgvEmployees.Columns.Add("Phone",      "Phone");
            dgvEmployees.Columns.Add("Status",     "Status");

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
        
            Controls.Add(dgvEmployees);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Employees";
            Name = "EmployeesForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelPagination.ResumeLayout(false);
            panelPagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEmployees).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel         panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel         panelBottom;
        private System.Windows.Forms.Panel         panelPagination;
        private System.Windows.Forms.Button        btnAdd;
        private System.Windows.Forms.Button        btnEdit;
        private System.Windows.Forms.Button        btnDelete;
        private System.Windows.Forms.Button        btnRefresh;
        private System.Windows.Forms.Button        btnExport;
        private System.Windows.Forms.TextBox       txtSearch;
        private System.Windows.Forms.Label         lblPageSize;
        private System.Windows.Forms.ComboBox      cmbPageSize;
        private System.Windows.Forms.Button        btnPrevPage;
        private System.Windows.Forms.Button        btnNextPage;
        private System.Windows.Forms.Label         lblPageIndicator;
        private System.Windows.Forms.Label         lblRowCount;
        private System.Windows.Forms.DataGridView  dgvEmployees;
        private System.Windows.Forms.Button btnImport;
    }
}
