// Forms/EquipmentForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class EquipmentForm
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
            btnAdd             = new System.Windows.Forms.Button();
            btnEdit            = new System.Windows.Forms.Button();
            btnDelete          = new System.Windows.Forms.Button();
            btnRefresh         = new System.Windows.Forms.Button();
            btnExport          = new System.Windows.Forms.Button();
            btnShowQR          = new System.Windows.Forms.Button();
            btnImportExcel     = new System.Windows.Forms.Button();
            btnSelectAll       = new System.Windows.Forms.Button();
            btnGenerateQRSheet = new System.Windows.Forms.Button();
            txtSearch          = new System.Windows.Forms.TextBox();
            dgvEquipment       = new System.Windows.Forms.DataGridView();
            panelBottom        = new System.Windows.Forms.Panel();
            panelPagination    = new System.Windows.Forms.Panel();
            lblRowCount        = new System.Windows.Forms.Label();
            lblPageSize        = new System.Windows.Forms.Label();
            cmbPageSize        = new System.Windows.Forms.ComboBox();
            btnPrevPage        = new System.Windows.Forms.Button();
            lblPageIndicator   = new System.Windows.Forms.Label();
            btnNextPage        = new System.Windows.Forms.Button();

            panelTop.SuspendLayout();
            panelBottom.SuspendLayout();
            panelPagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).BeginInit();
            SuspendLayout();

            // ── panelTop ──────────────────────────────────────────────────
            panelTop.Dock   = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.Name   = "panelTop";
            panelTop.Controls.Add(btnAdd);
            panelTop.Controls.Add(btnEdit);
            panelTop.Controls.Add(btnDelete);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(btnShowQR);
            panelTop.Controls.Add(btnImportExcel);
            panelTop.Controls.Add(btnSelectAll);
            panelTop.Controls.Add(btnGenerateQRSheet);
            panelTop.Controls.Add(txtSearch);

            btnAdd.Location = new System.Drawing.Point(12, 14);
            btnAdd.Size     = new System.Drawing.Size(100, 32);
            btnAdd.Text     = "➕ Add";
            btnAdd.Name     = "btnAdd";
            btnAdd.Click   += btnAdd_Click;

            btnEdit.Location = new System.Drawing.Point(120, 14);
            btnEdit.Size     = new System.Drawing.Size(100, 32);
            btnEdit.Text     = "✏️ Edit";
            btnEdit.Name     = "btnEdit";
            btnEdit.Click   += btnEdit_Click;

            btnDelete.Location = new System.Drawing.Point(228, 14);
            btnDelete.Size     = new System.Drawing.Size(100, 32);
            btnDelete.Text     = "🗑️ Delete";
            btnDelete.Name     = "btnDelete";
            btnDelete.Click   += btnDelete_Click;

            btnRefresh.Location = new System.Drawing.Point(336, 14);
            btnRefresh.Size     = new System.Drawing.Size(100, 32);
            btnRefresh.Text     = "🔄 Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            btnExport.Location = new System.Drawing.Point(444, 14);
            btnExport.Size     = new System.Drawing.Size(100, 32);
            btnExport.Text     = "📥 Export";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            btnShowQR.Location = new System.Drawing.Point(552, 14);
            btnShowQR.Size     = new System.Drawing.Size(100, 32);
            btnShowQR.Text     = "🔍 Show QR";
            btnShowQR.Name     = "btnShowQR";
            btnShowQR.Click   += btnShowQR_Click;

            btnImportExcel.Location = new System.Drawing.Point(660, 14);
            btnImportExcel.Size     = new System.Drawing.Size(120, 32);
            btnImportExcel.Text     = "📥 Import Excel";
            btnImportExcel.Name     = "btnImportExcel";
            btnImportExcel.Click   += btnImportExcel_Click;

            btnSelectAll.Location = new System.Drawing.Point(788, 14);
            btnSelectAll.Size     = new System.Drawing.Size(90, 32);
            btnSelectAll.Text     = "Select All";
            btnSelectAll.Name     = "btnSelectAll";
            btnSelectAll.Click   += btnSelectAll_Click;

            btnGenerateQRSheet.Location = new System.Drawing.Point(1006, 14);
            btnGenerateQRSheet.Size     = new System.Drawing.Size(130, 32);
            btnGenerateQRSheet.Text     = "🖨 QR Sheet";
            btnGenerateQRSheet.Name     = "btnGenerateQRSheet";
            btnGenerateQRSheet.Click   += btnGenerateQRSheet_Click;

            txtSearch.Location        = new System.Drawing.Point(1144, 17);
            txtSearch.Size            = new System.Drawing.Size(240, 26);
            txtSearch.PlaceholderText = "Search Equipment…";
            txtSearch.Name            = "txtSearch";
            txtSearch.TextChanged    += txtSearch_TextChanged;

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

            // ── dgvEquipment ──────────────────────────────────────────────
            dgvEquipment.Dock                  = System.Windows.Forms.DockStyle.Fill;
            dgvEquipment.AutoSizeColumnsMode   = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            dgvEquipment.AllowUserToAddRows    = false;
            dgvEquipment.ReadOnly              = true;
            dgvEquipment.SelectionMode         = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvEquipment.MultiSelect           = false;
            dgvEquipment.Name                  = "dgvEquipment";
            dgvEquipment.CellDoubleClick      += dgvEquipment_CellDoubleClick;
            dgvEquipment.Columns.Add("Code",            "Equipment Code");
            dgvEquipment.Columns.Add("QR",              "QR Code");
            dgvEquipment.Columns.Add("ProcurementType", "Procurement Type");
            dgvEquipment.Columns.Add("Name",            "Equipment Name");
            dgvEquipment.Columns.Add("Make",            "Make");
            dgvEquipment.Columns.Add("Model",           "Model");
            dgvEquipment.Columns.Add("Serial",          "Serial Number");
            dgvEquipment.Columns.Add("Category",        "Category");
            dgvEquipment.Columns.Add("Status",          "Status");
            dgvEquipment.Columns.Add("ConditionGrade",  "Condition");
            dgvEquipment.Columns.Add("Location",        "Location");
            dgvEquipment.Columns.Add("PurchaseDate",    "Purchase Date");

            // ── EquipmentForm ─────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
            AutoScroll          = true;
            AutoScrollMinSize   = new System.Drawing.Size(1000, 300);
            Controls.Add(dgvEquipment);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Equipment";
            Name = "EquipmentForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelPagination.ResumeLayout(false);
            panelPagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEquipment).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Panel        panelPagination;
        private System.Windows.Forms.Button       btnAdd;
        private System.Windows.Forms.Button       btnEdit;
        private System.Windows.Forms.Button       btnDelete;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.Button       btnShowQR;
        private System.Windows.Forms.Button       btnImportExcel;
        private System.Windows.Forms.Button       btnSelectAll;
        private System.Windows.Forms.Button       btnGenerateQRSheet;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblPageSize;
        private System.Windows.Forms.ComboBox     cmbPageSize;
        private System.Windows.Forms.Button       btnPrevPage;
        private System.Windows.Forms.Button       btnNextPage;
        private System.Windows.Forms.Label        lblPageIndicator;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvEquipment;
    }
}
