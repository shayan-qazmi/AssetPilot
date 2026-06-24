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
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReturns).BeginInit();
            SuspendLayout();

            panelTop.Dock   = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.Name   = "panelTop";
            panelTop.Controls.Add(btnAdd);
            panelTop.Controls.Add(btnEdit);
            panelTop.Controls.Add(btnDelete);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(txtSearch);

            btnAdd.Location = new System.Drawing.Point(12, 14);
            btnAdd.Size     = new System.Drawing.Size(90, 32);
            btnAdd.Text     = "Add";
            btnAdd.Name     = "btnAdd";
            btnAdd.Click   += BtnAdd_Click;

            btnEdit.Location = new System.Drawing.Point(110, 14);
            btnEdit.Size     = new System.Drawing.Size(90, 32);
            btnEdit.Text     = "Edit";
            btnEdit.Name     = "btnEdit";
            btnEdit.Click   += BtnEdit_Click;

            btnDelete.Location = new System.Drawing.Point(208, 14);
            btnDelete.Size     = new System.Drawing.Size(90, 32);
            btnDelete.Text     = "Delete";
            btnDelete.Name     = "btnDelete";
            btnDelete.Click   += BtnDelete_Click;

            btnRefresh.Location = new System.Drawing.Point(306, 14);
            btnRefresh.Size     = new System.Drawing.Size(90, 32);
            btnRefresh.Text     = "Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            btnExport.Location = new System.Drawing.Point(404, 14);
            btnExport.Size     = new System.Drawing.Size(90, 32);
            btnExport.Text     = "Export";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            txtSearch.Location        = new System.Drawing.Point(600, 16);
            txtSearch.Size            = new System.Drawing.Size(260, 26);
            txtSearch.PlaceholderText = "Search Return…";
            txtSearch.Name            = "txtSearch";
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
            Controls.Add(dgvReturns);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Returns";
            Name = "ReturnsForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvReturns).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
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