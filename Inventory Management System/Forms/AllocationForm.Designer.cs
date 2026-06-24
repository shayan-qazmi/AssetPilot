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
            panelTop      = new System.Windows.Forms.Panel();
            btnAdd        = new System.Windows.Forms.Button();
            btnApprove    = new System.Windows.Forms.Button();
            btnReturn     = new System.Windows.Forms.Button();
            btnDelete     = new System.Windows.Forms.Button();
            btnRefresh    = new System.Windows.Forms.Button();
            btnExport     = new System.Windows.Forms.Button();
            btnSelectAll  = new System.Windows.Forms.Button();
            txtSearch     = new System.Windows.Forms.TextBox();
            dgvAllocation = new System.Windows.Forms.DataGridView();
            panelBottom   = new System.Windows.Forms.Panel();
            lblRowCount   = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAllocation).BeginInit();
            SuspendLayout();

            panelTop.Dock      = System.Windows.Forms.DockStyle.Top;
            panelTop.Height    = 60;
            panelTop.Name      = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(btnAdd);
            panelTop.Controls.Add(btnApprove);
            panelTop.Controls.Add(btnReturn);
            panelTop.Controls.Add(btnDelete);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(btnSelectAll);
            panelTop.Controls.Add(txtSearch);

            btnAdd.Location = new System.Drawing.Point(12, 14);
            btnAdd.Size     = new System.Drawing.Size(80, 32);
            btnAdd.Text     = "Allocate";
            btnAdd.Name     = "btnAdd";
            btnAdd.Click   += btnAdd_Click;

            btnApprove.Location = new System.Drawing.Point(100, 14);
            btnApprove.Size     = new System.Drawing.Size(80, 32);
            btnApprove.Text     = "Approve";
            btnApprove.Name     = "btnApprove";
            btnApprove.Click   += btnApprove_Click;

            btnReturn.Location = new System.Drawing.Point(188, 14);
            btnReturn.Size     = new System.Drawing.Size(80, 32);
            btnReturn.Text     = "Return";
            btnReturn.Name     = "btnReturn";
            btnReturn.Click   += btnReturn_Click;

            btnDelete.Location = new System.Drawing.Point(276, 14);
            btnDelete.Size     = new System.Drawing.Size(80, 32);
            btnDelete.Text     = "Delete";
            btnDelete.Name     = "btnDelete";
            btnDelete.Click   += btnDelete_Click;

            btnRefresh.Location = new System.Drawing.Point(364, 14);
            btnRefresh.Size     = new System.Drawing.Size(80, 32);
            btnRefresh.Text     = "Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            btnExport.Location = new System.Drawing.Point(452, 14);
            btnExport.Size     = new System.Drawing.Size(80, 32);
            btnExport.Text     = "Export";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            btnSelectAll.Location = new System.Drawing.Point(540, 14);
            btnSelectAll.Size     = new System.Drawing.Size(90, 32);
            btnSelectAll.Text     = "Select All";
            btnSelectAll.Name     = "btnSelectAll";
            btnSelectAll.Click   += btnSelectAll_Click;

            txtSearch.Location        = new System.Drawing.Point(700, 16);
            txtSearch.Size            = new System.Drawing.Size(240, 26);
            txtSearch.PlaceholderText = "Search allocations...";
            txtSearch.Name            = "txtSearch";
            txtSearch.TextChanged    += txtSearch_TextChanged;

            dgvAllocation.Dock                  = System.Windows.Forms.DockStyle.Fill;
            dgvAllocation.AutoSizeColumnsMode   = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvAllocation.AllowUserToAddRows    = false;
            dgvAllocation.ReadOnly              = true;
            dgvAllocation.SelectionMode         = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvAllocation.MultiSelect           = true;
            dgvAllocation.Name                  = "dgvAllocation";
            dgvAllocation.Columns.Add("AllocationID",  "ID");
            dgvAllocation.Columns.Add("EquipmentCode", "Equipment Code");
            dgvAllocation.Columns.Add("EquipmentName", "Equipment Name");
            dgvAllocation.Columns.Add("Employee",      "Employee");
            dgvAllocation.Columns.Add("Location",      "Location");
            dgvAllocation.Columns.Add("DateAllocated", "Date Allocated");
            dgvAllocation.Columns.Add("ExpectedReturn","Expected Return");
            dgvAllocation.Columns.Add("Status",        "Status");

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
            Controls.Add(dgvAllocation);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Allocation Management";
            Name = "AllocationForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAllocation).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Button       btnAdd;
        private System.Windows.Forms.Button       btnApprove;
        private System.Windows.Forms.Button       btnReturn;
        private System.Windows.Forms.Button       btnDelete;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.Button       btnSelectAll;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvAllocation;
    }
}