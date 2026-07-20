// Forms/ReturnsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class ReturnsForm
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
            panelTop    = new System.Windows.Forms.Panel();
            flowToolbar = new System.Windows.Forms.FlowLayoutPanel();
            btnAdd      = new System.Windows.Forms.Button();
            btnEdit     = new System.Windows.Forms.Button();
            btnDelete   = new System.Windows.Forms.Button();
            btnRefresh  = new System.Windows.Forms.Button();
            btnExport   = new System.Windows.Forms.Button();
            txtSearch   = new System.Windows.Forms.TextBox();
            dgvReturns  = new System.Windows.Forms.DataGridView();
            panelBottom = new System.Windows.Forms.Panel();
            lblRowCount = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReturns).BeginInit();
            SuspendLayout();

            panelTop.Dock         = System.Windows.Forms.DockStyle.Top;
            panelTop.AutoSize     = true;
            panelTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelTop.MinimumSize  = new System.Drawing.Size(0, 60);
            panelTop.Name         = "panelTop";
            panelTop.Controls.Add(flowToolbar);

            flowToolbar.Dock          = System.Windows.Forms.DockStyle.Fill;
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
            flowToolbar.Controls.Add(txtSearch);

            btnAdd.Size   = new System.Drawing.Size(90, 32);
            btnAdd.Text   = "➕ Process Return";
            btnAdd.Name   = "btnAdd";
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnAdd.Click += BtnAdd_Click;

            btnEdit.Size   = new System.Drawing.Size(90, 32);
            btnEdit.Text   = "✏️ Edit";
            btnEdit.Name   = "btnEdit";
            btnEdit.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnEdit.Click += BtnEdit_Click;

            btnDelete.Size   = new System.Drawing.Size(90, 32);
            btnDelete.Text   = "🗑️ Remove Return";
            btnDelete.Name   = "btnDelete";
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnDelete.Click += BtnDelete_Click;

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

            txtSearch.Size            = new System.Drawing.Size(260, 26);
            txtSearch.PlaceholderText = "Search Return…";
            txtSearch.Name            = "txtSearch";
            txtSearch.Margin          = new System.Windows.Forms.Padding(16, 7, 4, 0);
            txtSearch.TextChanged    += txtSearch_TextChanged;

            dgvReturns.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvReturns.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvReturns.AllowUserToAddRows  = false;
            dgvReturns.ReadOnly            = true;
            dgvReturns.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvReturns.Name                = "dgvReturns";
            dgvReturns.Columns.Add("ReturnID",      "Return ID");
            dgvReturns.Columns.Add("EquipmentCode", "Equipment Code");
            dgvReturns.Columns.Add("EquipmentName", "Equipment Name");
            dgvReturns.Columns.Add("Employee",      "Employee");
            dgvReturns.Columns.Add("ReturnDate",    "Return Date");
            dgvReturns.Columns.Add("Condition",     "Condition");
            dgvReturns.Columns.Add("Source", "Source");

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
            Controls.Add(dgvReturns);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Returns";
            Name = "ReturnsForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvReturns).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Button       btnAdd;
        private System.Windows.Forms.Button       btnEdit;
        private System.Windows.Forms.Button       btnDelete;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvReturns;
    }
}