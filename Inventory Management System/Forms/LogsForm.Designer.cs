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
            panelTop    = new System.Windows.Forms.Panel();
            btnRefresh  = new System.Windows.Forms.Button();
            btnExport   = new System.Windows.Forms.Button();
            txtSearch   = new System.Windows.Forms.TextBox();
            dgvLogs     = new System.Windows.Forms.DataGridView();
            panelBottom = new System.Windows.Forms.Panel();
            lblRowCount = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLogs).BeginInit();
            SuspendLayout();

            // ── panelTop ──────────────────────────────────────────────────
            panelTop.Dock   = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.Name   = "panelTop";
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(txtSearch);

            // btnRefresh
            btnRefresh.Location = new System.Drawing.Point(12, 14);
            btnRefresh.Size     = new System.Drawing.Size(100, 32);
            btnRefresh.Text     = "🔄 Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            // btnExport
            btnExport.Location = new System.Drawing.Point(120, 14);
            btnExport.Size     = new System.Drawing.Size(100, 32);
            btnExport.Text     = "📥 Export";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            // txtSearch
            txtSearch.Location        = new System.Drawing.Point(260, 16);
            txtSearch.Size            = new System.Drawing.Size(300, 26);
            txtSearch.PlaceholderText = "Search by equipment, action, user…";
            txtSearch.Name            = "txtSearch";
            txtSearch.TextChanged    += txtSearch_TextChanged;

            // ── panelBottom ───────────────────────────────────────────────
            panelBottom.Dock      = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height    = 30;
            panelBottom.Name      = "panelBottom";
            panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelBottom.Controls.Add(lblRowCount);

            // lblRowCount
            lblRowCount.AutoSize  = false;
            lblRowCount.Dock      = System.Windows.Forms.DockStyle.Fill;
            lblRowCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblRowCount.Text      = "Showing 0 records";
            lblRowCount.Name      = "lblRowCount";
            lblRowCount.Padding   = new System.Windows.Forms.Padding(8, 0, 0, 0);

            // ── dgvLogs ───────────────────────────────────────────────────
            dgvLogs.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvLogs.AllowUserToAddRows  = false;
            dgvLogs.ReadOnly            = true;
            dgvLogs.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvLogs.Name                = "dgvLogs";

            // Columns
            var colId   = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "ID",        HeaderText = "Log ID",         FillWeight = 6 };
            var colEq   = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Equipment", HeaderText = "Equipment Code", FillWeight = 12 };
            var colAct  = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Action",    HeaderText = "Action",         FillWeight = 18 };
            var colUser = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "User",      HeaderText = "Changed By",     FillWeight = 14 };
            var colOld  = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "OldValue",  HeaderText = "Previous Value", FillWeight = 20 };
            var colNew  = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "NewValue",  HeaderText = "New Value",      FillWeight = 20 };
            var colDate = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Date",      HeaderText = "Timestamp",      FillWeight = 14 };

            dgvLogs.Columns.AddRange(colId, colEq, colAct, colUser, colOld, colNew, colDate);

            // ── LogsForm ──────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
            AutoScroll          = true;
            AutoScrollMinSize   = new System.Drawing.Size(1000, 300);
            Controls.Add(dgvLogs);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Text = "Audit Logs";
            Name = "LogsForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvLogs).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Panel        panelBottom;
        private System.Windows.Forms.Button       btnRefresh;
        private System.Windows.Forms.Button       btnExport;
        private System.Windows.Forms.TextBox      txtSearch;
        private System.Windows.Forms.Label        lblRowCount;
        private System.Windows.Forms.DataGridView dgvLogs;
    }
}