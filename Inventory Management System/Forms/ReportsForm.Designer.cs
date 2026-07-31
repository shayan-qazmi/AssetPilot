// Forms/ReportsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class ReportsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelTop = new System.Windows.Forms.Panel();
            lblReportType = new System.Windows.Forms.Label();
            flowToolbar = new System.Windows.Forms.FlowLayoutPanel();
            btnEquipmentReport = new System.Windows.Forms.Button();
            btnEmployeeReport = new System.Windows.Forms.Button();
            btnAllocationReport = new System.Windows.Forms.Button();
            btnReturnReport = new System.Windows.Forms.Button();
            btnExportPDF = new System.Windows.Forms.Button();
            btnExportExcel = new System.Windows.Forms.Button();
           

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            SuspendLayout();

            // ── panelTop ────────────────────────────────────────────────────
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.AutoSize = true;
            panelTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelTop.MinimumSize = new System.Drawing.Size(0, 60);
            panelTop.Name = "panelTop";
            panelTop.Padding = new System.Windows.Forms.Padding(20, 12, 0, 0);
        
            panelTop.Controls.Add(flowToolbar);
           

            // ── lblReportType ───────────────────────────────────────────────
            lblReportType.Dock = System.Windows.Forms.DockStyle.Top;
            lblReportType.Height = 28;
            lblReportType.Padding = new System.Windows.Forms.Padding(20, 8, 0, 0);
            lblReportType.Name = "lblReportType";
            lblReportType.Text = "Select a Report:";

            // ── flowToolbar ─────────────────────────────────────────────────
            flowToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents = true;
            flowToolbar.AutoSize = true;
            flowToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding = new System.Windows.Forms.Padding(0, 4, 8, 8); 
            flowToolbar.Name = "flowToolbar";
            flowToolbar.Controls.Add(btnEquipmentReport);
            flowToolbar.Controls.Add(btnEmployeeReport);
            flowToolbar.Controls.Add(btnAllocationReport);
            flowToolbar.Controls.Add(btnReturnReport);
            flowToolbar.Controls.Add(btnExportPDF);
            flowToolbar.Controls.Add(btnExportExcel);

            btnEquipmentReport.Name = "btnEquipmentReport";
            btnEquipmentReport.Text = "🖥 Equipment";
            btnEquipmentReport.Size = new System.Drawing.Size(115, 32);
            btnEquipmentReport.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            btnEquipmentReport.Click += new System.EventHandler(btnEquipmentReport_Click);

            btnEmployeeReport.Name = "btnEmployeeReport";
            btnEmployeeReport.Text = "👤 Employee";
            btnEmployeeReport.Size = new System.Drawing.Size(115, 32);
            btnEmployeeReport.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            btnEmployeeReport.Click += new System.EventHandler(btnEmployeeReport_Click);

            btnAllocationReport.Name = "btnAllocationReport";
            btnAllocationReport.Text = "📋 Allocation";
            btnAllocationReport.Size = new System.Drawing.Size(115, 32);
            btnAllocationReport.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            btnAllocationReport.Click += new System.EventHandler(btnAllocationReport_Click);

            btnReturnReport.Name = "btnReturnReport";
            btnReturnReport.Text = "↩ Return";
            btnReturnReport.Size = new System.Drawing.Size(115, 32);
            btnReturnReport.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            btnReturnReport.Click += new System.EventHandler(btnReturnReport_Click);

            btnExportPDF.Name = "btnExportPDF";
            btnExportPDF.Text = "📄 Export PDF";
            btnExportPDF.Size = new System.Drawing.Size(115, 32);
            btnExportPDF.Margin = new System.Windows.Forms.Padding(16, 0, 0, 4);
            btnExportPDF.Click += new System.EventHandler(btnExportPDF_Click);

            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Text = "📊 Export Excel";
            btnExportExcel.Size = new System.Drawing.Size(115, 32);
            btnExportExcel.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            btnExportExcel.Click += new System.EventHandler(btnExportExcel_Click);

          

            // ── ReportsForm ─────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 700);
            Text = "Reports";
            Name = "ReportsForm";

       
            Controls.Add(panelTop);

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Button btnEquipmentReport;
        private System.Windows.Forms.Button btnEmployeeReport;
        private System.Windows.Forms.Button btnAllocationReport;
        private System.Windows.Forms.Button btnReturnReport;
        private System.Windows.Forms.Button btnExportPDF;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Label lblReportType;
     
    }
}