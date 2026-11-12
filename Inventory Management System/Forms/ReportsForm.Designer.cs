// Forms/ReportsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class ReportsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelTop = new System.Windows.Forms.Panel();
            pnlTabBar = new System.Windows.Forms.Panel();
            flowReportButtons = new System.Windows.Forms.FlowLayoutPanel();
            btnEquipmentReport = new System.Windows.Forms.Button();
            btnEmployeeReport = new System.Windows.Forms.Button();
            btnAllocationReport = new System.Windows.Forms.Button();
            btnReturnReport = new System.Windows.Forms.Button();
            btnCheckInOutReport = new System.Windows.Forms.Button();
            flowExportButtons = new System.Windows.Forms.FlowLayoutPanel();
            btnPrint = new System.Windows.Forms.Button();
            btnExportPDF = new System.Windows.Forms.Button();
            btnExportExcel = new System.Windows.Forms.Button();
            btnExportCSV = new System.Windows.Forms.Button();
            pnlFilterBar = new System.Windows.Forms.Panel();
            flowFilterRow1 = new System.Windows.Forms.FlowLayoutPanel();
            btnDateAllTime = new System.Windows.Forms.Button();
            btnDateToday = new System.Windows.Forms.Button();
            btnDate7Days = new System.Windows.Forms.Button();
            btnDate30Days = new System.Windows.Forms.Button();
            btnDateThisYear = new System.Windows.Forms.Button();
            btnDateCustom = new System.Windows.Forms.Button();
            lblFilterSep = new System.Windows.Forms.Label();
            cmbFilterLocation = new System.Windows.Forms.ComboBox();
            cmbFilterStatus = new System.Windows.Forms.ComboBox();
            txtFilterSearch = new System.Windows.Forms.TextBox();
            btnResetFilters = new System.Windows.Forms.Button();
            pnlSummaryCards = new System.Windows.Forms.TableLayoutPanel();
            pnlCard1 = new System.Windows.Forms.Panel();
            lblCard1Title = new System.Windows.Forms.Label();
            lblCard1Value = new System.Windows.Forms.Label();
            pnlCard2 = new System.Windows.Forms.Panel();
            lblCard2Title = new System.Windows.Forms.Label();
            lblCard2Value = new System.Windows.Forms.Label();
            pnlCard3 = new System.Windows.Forms.Panel();
            lblCard3Title = new System.Windows.Forms.Label();
            lblCard3Value = new System.Windows.Forms.Label();
            pnlCard4 = new System.Windows.Forms.Panel();
            lblCard4Title = new System.Windows.Forms.Label();
            lblCard4Value = new System.Windows.Forms.Label();
            pnlGridHeader = new System.Windows.Forms.Panel();
            lblGridReportTitle = new System.Windows.Forms.Label();
            lblGridReportSubtitle = new System.Windows.Forms.Label();
            flowGridHeaderLeft = new System.Windows.Forms.FlowLayoutPanel();
            flowGridHeaderRight = new System.Windows.Forms.FlowLayoutPanel();
            btnColumns = new System.Windows.Forms.Button();
            btnSort = new System.Windows.Forms.Button();
            dgvReport = new System.Windows.Forms.DataGridView();
            pnlPagination = new System.Windows.Forms.Panel();
            lblPaginationShowing = new System.Windows.Forms.Label();
            flowPaginationRight = new System.Windows.Forms.FlowLayoutPanel();
            cmbPageSize = new System.Windows.Forms.ComboBox();
            btnPrevPage = new System.Windows.Forms.Button();
            btnNextPage = new System.Windows.Forms.Button();

            panelTop.SuspendLayout();
            pnlTabBar.SuspendLayout();
            flowReportButtons.SuspendLayout();
            flowExportButtons.SuspendLayout();
            pnlFilterBar.SuspendLayout();
            flowFilterRow1.SuspendLayout();
            pnlSummaryCards.SuspendLayout();
            pnlCard1.SuspendLayout();
            pnlCard2.SuspendLayout();
            pnlCard3.SuspendLayout();
            pnlCard4.SuspendLayout();
            pnlGridHeader.SuspendLayout();
            flowGridHeaderLeft.SuspendLayout();
            flowGridHeaderRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReport).BeginInit();
            pnlPagination.SuspendLayout();
            flowPaginationRight.SuspendLayout();
            SuspendLayout();

            // ── panelTop ────────────────────────────────────────────────────
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.AutoSize = true;
            panelTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelTop.Name = "panelTop";
            panelTop.Padding = new System.Windows.Forms.Padding(20, 10, 20, 6);
            panelTop.Controls.Add(pnlGridHeader);
            panelTop.Controls.Add(pnlSummaryCards);
            panelTop.Controls.Add(pnlFilterBar);
            panelTop.Controls.Add(pnlTabBar);

            // ── pnlTabBar ───────────────────────────────────────────────────
            pnlTabBar.Dock = System.Windows.Forms.DockStyle.Top;
            pnlTabBar.Height = 42;
            pnlTabBar.Name = "pnlTabBar";
            pnlTabBar.Controls.Add(flowExportButtons);
            pnlTabBar.Controls.Add(flowReportButtons);

            // ── flowReportButtons (Left) ────────────────────────────────────
            flowReportButtons.Dock = System.Windows.Forms.DockStyle.Left;
            flowReportButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowReportButtons.WrapContents = false;
            flowReportButtons.AutoSize = true;
            flowReportButtons.Name = "flowReportButtons";
            flowReportButtons.Controls.Add(btnEquipmentReport);
            flowReportButtons.Controls.Add(btnEmployeeReport);
            flowReportButtons.Controls.Add(btnAllocationReport);
            flowReportButtons.Controls.Add(btnReturnReport);
            flowReportButtons.Controls.Add(btnCheckInOutReport);

            void SetupTabBtn(System.Windows.Forms.Button b, string text, System.EventHandler handler)
            {
                b.Text = text;
                b.AutoSize = true;
                b.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new System.Drawing.Size(100, 34);
                b.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
                b.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
                b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.Cursor = System.Windows.Forms.Cursors.Hand;
                b.Click += handler;
            }

            btnEquipmentReport.Name = "btnEquipmentReport";
            SetupTabBtn(btnEquipmentReport, "Equipment  ▾", btnEquipmentReport_Click);

            btnEmployeeReport.Name = "btnEmployeeReport";
            SetupTabBtn(btnEmployeeReport, "Employee", btnEmployeeReport_Click);

            btnAllocationReport.Name = "btnAllocationReport";
            SetupTabBtn(btnAllocationReport, "Allocation", btnAllocationReport_Click);

            btnReturnReport.Name = "btnReturnReport";
            SetupTabBtn(btnReturnReport, "Returns", btnReturnReport_Click);

            btnCheckInOutReport.Name = "btnCheckInOutReport";
            SetupTabBtn(btnCheckInOutReport, "Check In/Out", btnCheckInOutReport_Click);

            // ── flowExportButtons (Right) ───────────────────────────────────
            flowExportButtons.Dock = System.Windows.Forms.DockStyle.Right;
            flowExportButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            flowExportButtons.WrapContents = false;
            flowExportButtons.AutoSize = true;
            flowExportButtons.Name = "flowExportButtons";
            flowExportButtons.Controls.Add(btnExportCSV);
            flowExportButtons.Controls.Add(btnExportExcel);
            flowExportButtons.Controls.Add(btnExportPDF);
            flowExportButtons.Controls.Add(btnPrint);

            void SetupExportBtn(System.Windows.Forms.Button b, string text, System.EventHandler handler)
            {
                b.Text = text;
                b.AutoSize = true;
                b.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new System.Drawing.Size(75, 34);
                b.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
                b.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
                b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                b.Cursor = System.Windows.Forms.Cursors.Hand;
                b.Click += handler;
            }

            btnPrint.Name = "btnPrint";
            SetupExportBtn(btnPrint, "Print", btnPrint_Click);

            btnExportPDF.Name = "btnExportPDF";
            SetupExportBtn(btnExportPDF, "Export PDF", btnExportPDF_Click);

            btnExportExcel.Name = "btnExportExcel";
            SetupExportBtn(btnExportExcel, "Export Excel", btnExportExcel_Click);

            btnExportCSV.Name = "btnExportCSV";
            SetupExportBtn(btnExportCSV, "Export CSV", btnExportCSV_Click);

            // ── pnlFilterBar ────────────────────────────────────────────────
            pnlFilterBar.Dock = System.Windows.Forms.DockStyle.Top;
            pnlFilterBar.AutoSize = true;
            pnlFilterBar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            pnlFilterBar.Name = "pnlFilterBar";
            pnlFilterBar.Padding = new System.Windows.Forms.Padding(0, 8, 0, 6);
            pnlFilterBar.Controls.Add(flowFilterRow1);

            // ── flowFilterRow1 ──────────────────────────────────────────────
            flowFilterRow1.Dock = System.Windows.Forms.DockStyle.Top;
            flowFilterRow1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowFilterRow1.WrapContents = false;
            flowFilterRow1.AutoSize = true;
            flowFilterRow1.Name = "flowFilterRow1";
            flowFilterRow1.Controls.Add(btnDateAllTime);
            flowFilterRow1.Controls.Add(btnDateToday);
            flowFilterRow1.Controls.Add(btnDate7Days);
            flowFilterRow1.Controls.Add(btnDate30Days);
            flowFilterRow1.Controls.Add(btnDateThisYear);
            flowFilterRow1.Controls.Add(btnDateCustom);
            flowFilterRow1.Controls.Add(lblFilterSep);
            flowFilterRow1.Controls.Add(cmbFilterLocation);
            flowFilterRow1.Controls.Add(cmbFilterStatus);
            flowFilterRow1.Controls.Add(txtFilterSearch);
            flowFilterRow1.Controls.Add(btnResetFilters);

            void SetupPillBtn(System.Windows.Forms.Button b, string text)
            {
                b.Text = text;
                b.AutoSize = true;
                b.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new System.Drawing.Size(65, 30);
                b.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
                b.Margin = new System.Windows.Forms.Padding(0, 2, 6, 4);
                b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                b.Cursor = System.Windows.Forms.Cursors.Hand;
            }

            btnDateAllTime.Name = "btnDateAllTime";
            SetupPillBtn(btnDateAllTime, "All time");

            btnDateToday.Name = "btnDateToday";
            SetupPillBtn(btnDateToday, "Today");

            btnDate7Days.Name = "btnDate7Days";
            SetupPillBtn(btnDate7Days, "7 days");

            btnDate30Days.Name = "btnDate30Days";
            SetupPillBtn(btnDate30Days, "30 days");

            btnDateThisYear.Name = "btnDateThisYear";
            SetupPillBtn(btnDateThisYear, "This year");

            btnDateCustom.Name = "btnDateCustom";
            SetupPillBtn(btnDateCustom, "Custom...");

            lblFilterSep.Text = "|";
            lblFilterSep.AutoSize = true;
            lblFilterSep.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular);
            lblFilterSep.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            lblFilterSep.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);

            cmbFilterLocation.Name = "cmbFilterLocation";
            cmbFilterLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbFilterLocation.Size = new System.Drawing.Size(125, 30);
            cmbFilterLocation.Margin = new System.Windows.Forms.Padding(0, 2, 8, 4);

            cmbFilterStatus.Name = "cmbFilterStatus";
            cmbFilterStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbFilterStatus.Size = new System.Drawing.Size(115, 30);
            cmbFilterStatus.Margin = new System.Windows.Forms.Padding(0, 2, 8, 4);

            txtFilterSearch.Name = "txtFilterSearch";
            txtFilterSearch.Size = new System.Drawing.Size(160, 30);
            txtFilterSearch.Margin = new System.Windows.Forms.Padding(0, 2, 8, 4);

            btnResetFilters.Name = "btnResetFilters";
            btnResetFilters.Text = "Reset";
            btnResetFilters.Size = new System.Drawing.Size(65, 30);
            btnResetFilters.Margin = new System.Windows.Forms.Padding(0, 2, 8, 4);
            btnResetFilters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnResetFilters.Cursor = System.Windows.Forms.Cursors.Hand;

            // ── pnlSummaryCards ─────────────────────────────────────────────
            pnlSummaryCards.Dock = System.Windows.Forms.DockStyle.Top;
            pnlSummaryCards.Height = 70;
            pnlSummaryCards.Name = "pnlSummaryCards";
            pnlSummaryCards.ColumnCount = 4;
            pnlSummaryCards.RowCount = 1;
            pnlSummaryCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            pnlSummaryCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            pnlSummaryCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            pnlSummaryCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            pnlSummaryCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            pnlSummaryCards.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            void SetupCard(System.Windows.Forms.Panel p, System.Windows.Forms.Label lTitle, System.Windows.Forms.Label lVal, string defTitle, string defVal)
            {
                p.Dock = System.Windows.Forms.DockStyle.Fill;
                p.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);

                lTitle.Text = defTitle;
                lTitle.AutoSize = true;
                lTitle.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular);
                lTitle.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
                lTitle.Location = new System.Drawing.Point(14, 10);

                lVal.Text = defVal;
                lVal.AutoSize = true;
                lVal.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold);
                lVal.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
                lVal.Location = new System.Drawing.Point(12, 28);

                p.Controls.Add(lVal);
                p.Controls.Add(lTitle);
            }

            pnlSummaryCards.Controls.Add(pnlCard1, 0, 0);
            pnlSummaryCards.Controls.Add(pnlCard2, 1, 0);
            pnlSummaryCards.Controls.Add(pnlCard3, 2, 0);
            pnlSummaryCards.Controls.Add(pnlCard4, 3, 0);

            SetupCard(pnlCard1, lblCard1Title, lblCard1Value, "Total rows", "0");
            SetupCard(pnlCard2, lblCard2Title, lblCard2Value, "Allocated", "0");
            SetupCard(pnlCard3, lblCard3Title, lblCard3Value, "In store", "0");
            SetupCard(pnlCard4, lblCard4Title, lblCard4Value, "Needs attention", "0");

            // ── pnlGridHeader ───────────────────────────────────────────────
            pnlGridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlGridHeader.Height = 44;
            pnlGridHeader.Name = "pnlGridHeader";
            pnlGridHeader.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
            pnlGridHeader.Controls.Add(flowGridHeaderRight);
            pnlGridHeader.Controls.Add(flowGridHeaderLeft);

            flowGridHeaderLeft.Dock = System.Windows.Forms.DockStyle.Left;
            flowGridHeaderLeft.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowGridHeaderLeft.WrapContents = false;
            flowGridHeaderLeft.AutoSize = true;
            flowGridHeaderLeft.Name = "flowGridHeaderLeft";
            flowGridHeaderLeft.Controls.Add(lblGridReportTitle);
            flowGridHeaderLeft.Controls.Add(lblGridReportSubtitle);

            lblGridReportTitle.AutoSize = true;
            lblGridReportTitle.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            lblGridReportTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblGridReportTitle.Margin = new System.Windows.Forms.Padding(0, 4, 12, 0);
            lblGridReportTitle.Name = "lblGridReportTitle";
            lblGridReportTitle.Text = "Equipment Report";

            lblGridReportSubtitle.AutoSize = true;
            lblGridReportSubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            lblGridReportSubtitle.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            lblGridReportSubtitle.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            lblGridReportSubtitle.Name = "lblGridReportSubtitle";
            lblGridReportSubtitle.Text = "0 records · updated just now";

            flowGridHeaderRight.Dock = System.Windows.Forms.DockStyle.Right;
            flowGridHeaderRight.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            flowGridHeaderRight.WrapContents = false;
            flowGridHeaderRight.AutoSize = true;
            flowGridHeaderRight.Name = "flowGridHeaderRight";
            flowGridHeaderRight.Controls.Add(btnSort);
            flowGridHeaderRight.Controls.Add(btnColumns);

            void SetupHeaderBtn(System.Windows.Forms.Button b, string text, System.EventHandler handler)
            {
                b.Text = text;
                b.Size = new System.Drawing.Size(80, 30);
                b.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
                b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                b.Cursor = System.Windows.Forms.Cursors.Hand;
                b.Click += handler;
            }

            btnColumns.Name = "btnColumns";
            SetupHeaderBtn(btnColumns, "Columns", btnColumns_Click);

            btnSort.Name = "btnSort";
            SetupHeaderBtn(btnSort, "Sort  ▾", btnSort_Click);

            // ── dgvReport ───────────────────────────────────────────────────
            dgvReport.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvReport.AllowUserToAddRows = false;
            dgvReport.AllowUserToDeleteRows = false;
            dgvReport.ReadOnly = true;
            dgvReport.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvReport.Name = "dgvReport";
            dgvReport.RowHeadersVisible = false;
            dgvReport.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgvReport.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;

            // ── pnlPagination ───────────────────────────────────────────────
            pnlPagination.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlPagination.Height = 44;
            pnlPagination.Name = "pnlPagination";
            pnlPagination.Padding = new System.Windows.Forms.Padding(20, 6, 20, 6);
            pnlPagination.Controls.Add(flowPaginationRight);
            pnlPagination.Controls.Add(lblPaginationShowing);

            lblPaginationShowing.Dock = System.Windows.Forms.DockStyle.Left;
            lblPaginationShowing.AutoSize = true;
            lblPaginationShowing.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            lblPaginationShowing.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            lblPaginationShowing.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblPaginationShowing.Name = "lblPaginationShowing";
            lblPaginationShowing.Text = "Showing 0–0 of 0";

            flowPaginationRight.Dock = System.Windows.Forms.DockStyle.Right;
            flowPaginationRight.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            flowPaginationRight.WrapContents = false;
            flowPaginationRight.AutoSize = true;
            flowPaginationRight.Name = "flowPaginationRight";
            flowPaginationRight.Controls.Add(btnNextPage);
            flowPaginationRight.Controls.Add(btnPrevPage);
            flowPaginationRight.Controls.Add(cmbPageSize);

            btnNextPage.Text = "Next";
            btnNextPage.Size = new System.Drawing.Size(65, 30);
            btnNextPage.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            btnNextPage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnNextPage.Cursor = System.Windows.Forms.Cursors.Hand;
            btnNextPage.Click += (s, e) => NextPage();

            btnPrevPage.Text = "Prev";
            btnPrevPage.Size = new System.Drawing.Size(65, 30);
            btnPrevPage.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            btnPrevPage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPrevPage.Cursor = System.Windows.Forms.Cursors.Hand;
            btnPrevPage.Click += (s, e) => PrevPage();

            cmbPageSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbPageSize.Size = new System.Drawing.Size(110, 30);
            cmbPageSize.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            // ── ReportsForm ─────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 750);
            Text = "Reports";
            Name = "ReportsForm";

            Controls.Add(dgvReport);
            Controls.Add(pnlPagination);
            Controls.Add(panelTop);

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            pnlTabBar.ResumeLayout(false);
            pnlTabBar.PerformLayout();
            flowReportButtons.ResumeLayout(false);
            flowReportButtons.PerformLayout();
            flowExportButtons.ResumeLayout(false);
            flowExportButtons.PerformLayout();
            pnlFilterBar.ResumeLayout(false);
            pnlFilterBar.PerformLayout();
            flowFilterRow1.ResumeLayout(false);
            flowFilterRow1.PerformLayout();
            pnlSummaryCards.ResumeLayout(false);
            pnlCard1.ResumeLayout(false);
            pnlCard1.PerformLayout();
            pnlCard2.ResumeLayout(false);
            pnlCard2.PerformLayout();
            pnlCard3.ResumeLayout(false);
            pnlCard3.PerformLayout();
            pnlCard4.ResumeLayout(false);
            pnlCard4.PerformLayout();
            pnlGridHeader.ResumeLayout(false);
            pnlGridHeader.PerformLayout();
            flowGridHeaderRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvReport).EndInit();
            pnlPagination.ResumeLayout(false);
            pnlPagination.PerformLayout();
            flowPaginationRight.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel pnlTabBar;
        private System.Windows.Forms.FlowLayoutPanel flowReportButtons;
        private System.Windows.Forms.Button btnEquipmentReport;
        private System.Windows.Forms.Button btnEmployeeReport;
        private System.Windows.Forms.Button btnAllocationReport;
        private System.Windows.Forms.Button btnReturnReport;
        private System.Windows.Forms.Button btnCheckInOutReport;

        private System.Windows.Forms.FlowLayoutPanel flowExportButtons;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Button btnExportPDF;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnExportCSV;

        private System.Windows.Forms.Panel pnlFilterBar;
        private System.Windows.Forms.FlowLayoutPanel flowFilterRow1;
        private System.Windows.Forms.Button btnDateAllTime;
        private System.Windows.Forms.Button btnDateToday;
        private System.Windows.Forms.Button btnDate7Days;
        private System.Windows.Forms.Button btnDate30Days;
        private System.Windows.Forms.Button btnDateThisYear;
        private System.Windows.Forms.Button btnDateCustom;
        private System.Windows.Forms.Label lblFilterSep;
        private System.Windows.Forms.ComboBox cmbFilterLocation;
        private System.Windows.Forms.ComboBox cmbFilterStatus;
        private System.Windows.Forms.TextBox txtFilterSearch;
        private System.Windows.Forms.Button btnResetFilters;

        private System.Windows.Forms.TableLayoutPanel pnlSummaryCards;
        private System.Windows.Forms.Panel pnlCard1;
        private System.Windows.Forms.Label lblCard1Title;
        private System.Windows.Forms.Label lblCard1Value;
        private System.Windows.Forms.Panel pnlCard2;
        private System.Windows.Forms.Label lblCard2Title;
        private System.Windows.Forms.Label lblCard2Value;
        private System.Windows.Forms.Panel pnlCard3;
        private System.Windows.Forms.Label lblCard3Title;
        private System.Windows.Forms.Label lblCard3Value;
        private System.Windows.Forms.Panel pnlCard4;
        private System.Windows.Forms.Label lblCard4Title;
        private System.Windows.Forms.Label lblCard4Value;

        private System.Windows.Forms.Panel pnlGridHeader;
        private System.Windows.Forms.FlowLayoutPanel flowGridHeaderLeft;
        private System.Windows.Forms.Label lblGridReportTitle;
        private System.Windows.Forms.Label lblGridReportSubtitle;
        private System.Windows.Forms.FlowLayoutPanel flowGridHeaderRight;
        private System.Windows.Forms.Button btnColumns;
        private System.Windows.Forms.Button btnSort;

        private System.Windows.Forms.DataGridView dgvReport;
        private System.Windows.Forms.Panel pnlPagination;
        private System.Windows.Forms.Label lblPaginationShowing;
        private System.Windows.Forms.FlowLayoutPanel flowPaginationRight;
        private System.Windows.Forms.ComboBox cmbPageSize;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
    }
}
