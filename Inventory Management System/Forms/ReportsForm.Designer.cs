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
            // Initialize controls
            this.btnEquipmentReport = new System.Windows.Forms.Button();
            this.btnEmployeeReport = new System.Windows.Forms.Button();
            this.btnAllocationReport = new System.Windows.Forms.Button();
            this.btnReturnReport = new System.Windows.Forms.Button();
            this.btnExportPDF = new System.Windows.Forms.Button();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.lblReportType = new System.Windows.Forms.Label();
            this.lblExport = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // btnEquipmentReport
            this.btnEquipmentReport.Name = "btnEquipmentReport";
            this.btnEquipmentReport.Text = "🖥 Equipment";
            this.btnEquipmentReport.Size = new System.Drawing.Size(115, 30);
            this.btnEquipmentReport.Location = new System.Drawing.Point(20, 20);
            this.btnEquipmentReport.Click += new System.EventHandler(this.btnEquipmentReport_Click);

            // btnEmployeeReport
            this.btnEmployeeReport.Name = "btnEmployeeReport";
            this.btnEmployeeReport.Text = "👤 Employee";
            this.btnEmployeeReport.Size = new System.Drawing.Size(115, 30);
            this.btnEmployeeReport.Location = new System.Drawing.Point(145, 20);
            this.btnEmployeeReport.Click += new System.EventHandler(this.btnEmployeeReport_Click);

            // btnAllocationReport
            this.btnAllocationReport.Name = "btnAllocationReport";
            this.btnAllocationReport.Text = "📋 Allocation";
            this.btnAllocationReport.Size = new System.Drawing.Size(115, 30);
            this.btnAllocationReport.Location = new System.Drawing.Point(270, 20);
            this.btnAllocationReport.Click += new System.EventHandler(this.btnAllocationReport_Click);

            // btnReturnReport
            this.btnReturnReport.Name = "btnReturnReport";
            this.btnReturnReport.Text = "↩ Return";
            this.btnReturnReport.Size = new System.Drawing.Size(115, 30);
            this.btnReturnReport.Location = new System.Drawing.Point(395, 20);
            this.btnReturnReport.Click += new System.EventHandler(this.btnReturnReport_Click);

            // btnExportPDF
            this.btnExportPDF.Name = "btnExportPDF";
            this.btnExportPDF.Text = "📄 Export PDF";
            this.btnExportPDF.Size = new System.Drawing.Size(115, 30);
            this.btnExportPDF.Location = new System.Drawing.Point(520, 20);
            this.btnExportPDF.Click += new System.EventHandler(this.btnExportPDF_Click);

            // btnExportExcel
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Text = "📊 Export Excel";
            this.btnExportExcel.Size = new System.Drawing.Size(115, 30);
            this.btnExportExcel.Location = new System.Drawing.Point(645, 20);
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);

            // lblReportType
            this.lblReportType.Name = "lblReportType";
            this.lblReportType.Text = "Report Type:";
            this.lblReportType.AutoSize = true;
            this.lblReportType.Text = "Select a Report:";

            // lblExport
            this.lblExport.Name = "lblExport";
            this.lblExport.Text = "Export:";
            this.lblExport.AutoSize = true;
            this.lblReportType.Location = new System.Drawing.Point(20, 0);

            // lblWatermark
            this.lblWatermark = new System.Windows.Forms.Label();
            this.lblWatermark.Name = "lblWatermark";
            this.lblWatermark.Text = "Developed by 📷@shayanqazmi";
            this.lblWatermark.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblWatermark.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
            this.lblWatermark.BackColor = System.Drawing.Color.Transparent;
            this.lblWatermark.AutoSize = true;
            this.lblWatermark.Location = new System.Drawing.Point(1020, 720);
            this.lblWatermark.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;

            // Form properties
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1160, 680);
            this.Text = "Reports";
            this.Name = "ReportsForm";

            // Add controls to form
            this.Controls.Add(this.btnEquipmentReport);
            this.Controls.Add(this.btnEmployeeReport);
            this.Controls.Add(this.btnAllocationReport);
            this.Controls.Add(this.btnReturnReport);
            this.Controls.Add(this.btnExportPDF);
            this.Controls.Add(this.btnExportExcel);
            this.Controls.Add(this.lblReportType);
            this.Controls.Add(this.lblWatermark);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Control declarations
        private System.Windows.Forms.Button btnEquipmentReport;
        private System.Windows.Forms.Button btnEmployeeReport;
        private System.Windows.Forms.Button btnAllocationReport;
        private System.Windows.Forms.Button btnReturnReport;
        private System.Windows.Forms.Button btnExportPDF;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Label lblReportType;
        private System.Windows.Forms.Label lblExport;
        private System.Windows.Forms.Label lblWatermark;
    }
}