// Forms/QRScannerForm.Designer.cs
#nullable enable
namespace Inventory_Management_System.Forms
{
    partial class QRScannerForm
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

            picQrPreview = new System.Windows.Forms.Panel();
            lblQrPreviewValue = new System.Windows.Forms.Label();
            lnkPrintLabel = new System.Windows.Forms.LinkLabel();
            chkBulkMode = new System.Windows.Forms.CheckBox();
            btnExportBulk = new System.Windows.Forms.Button();
            panelSuggestions = new System.Windows.Forms.FlowLayoutPanel();
            panelTop = new System.Windows.Forms.Panel();
            lblScanPrompt = new System.Windows.Forms.Label();
            pnlQrIcon = new System.Windows.Forms.Panel();
            txtQRCode = new System.Windows.Forms.TextBox();
            btnSearch = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            lblRecentLabel = new System.Windows.Forms.Label();
            lblSearchHint = new System.Windows.Forms.Label();
            panelRecent = new System.Windows.Forms.FlowLayoutPanel();
            panelResult = new System.Windows.Forms.Panel();
            lblNoResult = new System.Windows.Forms.Label();
            panelCard = new System.Windows.Forms.Panel();
            lblCardTitle = new System.Windows.Forms.Label();
            picHero = new System.Windows.Forms.Panel();
            lblHeroName = new System.Windows.Forms.Label();
            lblHeroCode = new System.Windows.Forms.Label();
            lblHeroStatus = new System.Windows.Forms.Label();
            btnViewPicture = new System.Windows.Forms.Button();
            btnViewLogs = new System.Windows.Forms.Button();
            btnViewQR = new System.Windows.Forms.Button();
            btnCheckInOut = new System.Windows.Forms.Button();
            toolTipHero = new System.Windows.Forms.ToolTip();
            lblEquipmentCode = new System.Windows.Forms.Label();
            lblEquipmentCodeValue = new System.Windows.Forms.Label();
            lblQrCode = new System.Windows.Forms.Label();
            lblQrCodeValue = new System.Windows.Forms.Label();
            lblEquipmentName = new System.Windows.Forms.Label();
            lblEquipmentNameValue = new System.Windows.Forms.Label();
            lblMake = new System.Windows.Forms.Label();
            lblMakeValue = new System.Windows.Forms.Label();
            lblModel = new System.Windows.Forms.Label();
            lblModelValue = new System.Windows.Forms.Label();
            lblSerial = new System.Windows.Forms.Label();
            lblSerialValue = new System.Windows.Forms.Label();
            lblCategory = new System.Windows.Forms.Label();
            lblCategoryValue = new System.Windows.Forms.Label();
            lblProcurementType = new System.Windows.Forms.Label();
            lblProcurementTypeValue = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            lblStatusValue = new System.Windows.Forms.Label();
            lblCondition = new System.Windows.Forms.Label();
            lblConditionValue = new System.Windows.Forms.Label();
            lblLocation = new System.Windows.Forms.Label();
            lblLocationValue = new System.Windows.Forms.Label();
            lblEmployee = new System.Windows.Forms.Label();
            lblEmployeeValue = new System.Windows.Forms.Label();
            lblPurchaseDate = new System.Windows.Forms.Label();
            lblPurchaseDateValue = new System.Windows.Forms.Label();
            lblWarranty = new System.Windows.Forms.Label();
            lblWarrantyValue = new System.Windows.Forms.Label();
            lblVendor = new System.Windows.Forms.Label();
            lblVendorValue = new System.Windows.Forms.Label();
            lblDescriptionTitle = new System.Windows.Forms.Label();
            lblDescriptionValue = new System.Windows.Forms.Label();
            lblNotesTitle = new System.Windows.Forms.Label();
            lblNotesValue = new System.Windows.Forms.Label();
            panelCardActions = new System.Windows.Forms.FlowLayoutPanel();
            btnExportHistory = new System.Windows.Forms.Button();
            btnPrintLabel = new System.Windows.Forms.Button();
            panelHistory = new System.Windows.Forms.Panel();
            lblHistoryTitle = new System.Windows.Forms.Label();
            dgvHistory = new System.Windows.Forms.DataGridView();

            panelTop.SuspendLayout();
            panelResult.SuspendLayout();
            panelCard.SuspendLayout();
            panelHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            SuspendLayout();

            // ── panelTop (search row + recent-scans row) ──────────────────
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 148;
            panelTop.Name = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(lblScanPrompt);
            panelTop.Controls.Add(pnlQrIcon);
            panelTop.Controls.Add(txtQRCode);
            panelTop.Controls.Add(btnSearch);
            panelTop.Controls.Add(btnClear);
            panelTop.Controls.Add(lblRecentLabel);
            panelTop.Controls.Add(panelRecent);

            lblScanPrompt.Text = "Scan QR / Enter Eqp Code:";
            lblScanPrompt.Location = new System.Drawing.Point(12, 24);
            lblScanPrompt.AutoSize = true;
            lblScanPrompt.Name = "lblScanPrompt";

            pnlQrIcon.Location = new System.Drawing.Point(190, 17);
            pnlQrIcon.Size = new System.Drawing.Size(36, 36);
            pnlQrIcon.Name = "pnlQrIcon";
            pnlQrIcon.BackColor = System.Drawing.Color.Transparent;

            txtQRCode.Location = new System.Drawing.Point(236, 20);
            txtQRCode.Size = new System.Drawing.Size(320, 30);
            txtQRCode.PlaceholderText = "Scan QR code or type equipment code...";
            txtQRCode.Name = "txtQRCode";
            txtQRCode.Font = new System.Drawing.Font("Segoe UI", 11F);
            txtQRCode.TextChanged += txtQRCode_TextChanged;
            txtQRCode.KeyDown += txtQRCode_KeyDown;

            btnSearch.Location = new System.Drawing.Point(566, 18);
            btnSearch.Size = new System.Drawing.Size(100, 34);
            btnSearch.Text = "🔍 Search";
            btnSearch.Name = "btnSearch";
            btnSearch.Click += btnSearch_Click;

            btnClear.Location = new System.Drawing.Point(674, 18);
            btnClear.Size = new System.Drawing.Size(100, 34);
            btnClear.Text = "↺ Clear";
            btnClear.Click += btnClear_Click;
            chkBulkMode.Text = "Bulk scan mode";
            chkBulkMode.Location = new System.Drawing.Point(12, 110);
            chkBulkMode.AutoSize = true;
            chkBulkMode.Name = "chkBulkMode";
            chkBulkMode.CheckedChanged += chkBulkMode_CheckedChanged;
            panelTop.Controls.Add(chkBulkMode);

            btnExportBulk.Text = "⬇ Export Scanned";
            btnExportBulk.Location = new System.Drawing.Point(160, 105);
            btnExportBulk.Size = new System.Drawing.Size(150, 32);
            btnExportBulk.Name = "btnExportBulk";
            btnExportBulk.Visible = false;
            btnExportBulk.Click += btnExportBulk_Click;
            panelTop.Controls.Add(btnExportBulk);

            lblSearchHint.Text = "Press Enter to search · matches update as you type";
            lblSearchHint.Location = new System.Drawing.Point(236, 52);
            lblSearchHint.AutoSize = true;
            lblSearchHint.Font = new System.Drawing.Font("Segoe UI", 7.5f);
            lblSearchHint.ForeColor = System.Drawing.Color.Gray;
            lblSearchHint.Name = "lblSearchHint";
            panelTop.Controls.Add(lblSearchHint);

            lblRecentLabel.Text = "Recent:";
            lblRecentLabel.Location = new System.Drawing.Point(12, 74);
            lblRecentLabel.AutoSize = true;
            lblRecentLabel.Name = "lblRecentLabel";

            panelRecent.Location = new System.Drawing.Point(70, 66);
            panelRecent.Size = new System.Drawing.Size(1100, 32);
            panelRecent.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            panelRecent.WrapContents = false;
            panelRecent.AutoScroll = true;
            panelRecent.Name = "panelRecent";
            panelRecent.BackColor = System.Drawing.Color.Transparent;

            // ── panelResult (left side — equipment card) ──────────────────
            panelResult.Location = new System.Drawing.Point(20, 168);
            panelResult.Size = new System.Drawing.Size(460, 720);
            panelResult.Name = "panelResult";
            panelResult.Anchor = System.Windows.Forms.AnchorStyles.Top |
                                    System.Windows.Forms.AnchorStyles.Left |
                                    System.Windows.Forms.AnchorStyles.Bottom;
            panelResult.Controls.Add(lblNoResult);
            panelSuggestions.Location = new System.Drawing.Point(0, 90);
            panelSuggestions.Size = new System.Drawing.Size(440, 140);
            panelSuggestions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            panelSuggestions.WrapContents = false;
            panelSuggestions.Name = "panelSuggestions";
            panelSuggestions.BackColor = System.Drawing.Color.Transparent;
            panelResult.Controls.Add(panelSuggestions);
            panelResult.Controls.Add(panelCard);

            lblNoResult.Text = "📷  Scan a QR code or enter an equipment code to see details";
            lblNoResult.AutoSize = false;
            lblNoResult.Size = new System.Drawing.Size(440, 60);
            lblNoResult.Location = new System.Drawing.Point(0, 20);
            lblNoResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblNoResult.Name = "lblNoResult";

            // ── panelCard ──────────────────────────────────────────────────
            panelCard.Location = new System.Drawing.Point(0, 0);
            panelCard.Size = new System.Drawing.Size(440, 700);
            panelCard.Name = "panelCard";
            panelCard.Visible = false;
            panelCard.BackColor = System.Drawing.Color.White;
            panelCard.AutoScroll = true;

            lblCardTitle.Text = "📋 Equipment Details";
            lblCardTitle.Location = new System.Drawing.Point(12, 12);
            lblCardTitle.AutoSize = true;
            lblCardTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblCardTitle.Name = "lblCardTitle";
            panelCard.Controls.Add(lblCardTitle);

            // ── Hero: photo + name/code/status + View Picture + View Logs ──
            picHero.Location = new System.Drawing.Point(12, 40);
            picHero.Size = new System.Drawing.Size(90, 90);
            picHero.Name = "picHero";
            picHero.BackColor = System.Drawing.Color.Transparent;
            picHero.Paint += picHero_Paint;
            panelCard.Controls.Add(picHero);

            lblHeroName.Location = new System.Drawing.Point(114, 40);
            lblHeroName.Size = new System.Drawing.Size(300, 20);
            lblHeroName.AutoSize = false;
            lblHeroName.AutoEllipsis = true;
            lblHeroName.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            lblHeroName.Name = "lblHeroName";
            panelCard.Controls.Add(lblHeroName);

            lblHeroCode.Location = new System.Drawing.Point(114, 62);
            lblHeroCode.Size = new System.Drawing.Size(300, 16);
            lblHeroCode.AutoSize = false;
            lblHeroCode.Font = new System.Drawing.Font("Segoe UI", 8f);
            lblHeroCode.Name = "lblHeroCode";
            panelCard.Controls.Add(lblHeroCode);

            lblHeroStatus.Location = new System.Drawing.Point(114, 84);
            lblHeroStatus.AutoSize = true;
            lblHeroStatus.Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold);
            lblHeroStatus.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            lblHeroStatus.Name = "lblHeroStatus";
            panelCard.Controls.Add(lblHeroStatus);

            const int lx = 12, rh = 22, sy = 176, colGap = 16, valOffset = 78;
            const int cardContentW = 416;
            int colW = (cardContentW - colGap) / 2;
            int col1X = lx;
            int col2X = lx + colW + colGap;

            const int btnRowY = 136, btnH = 32, btnGap = 4, btnRowW = cardContentW;
            int btnW = (btnRowW - btnGap * 3) / 4;

            btnViewPicture.Location = new System.Drawing.Point(lx, btnRowY);
            btnViewPicture.Size = new System.Drawing.Size(btnW, btnH);
            btnViewPicture.Font = new System.Drawing.Font("Segoe UI", 7.75f, System.Drawing.FontStyle.Bold);
            btnViewPicture.Text = "🖼 Picture";
            btnViewPicture.Name = "btnViewPicture";
            btnViewPicture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnViewPicture.Click += btnViewPicture_Click;
            panelCard.Controls.Add(btnViewPicture);

            btnViewLogs.Location = new System.Drawing.Point(lx + (btnW + btnGap) * 1, btnRowY);
            btnViewLogs.Size = new System.Drawing.Size(btnW, btnH);
            btnViewLogs.Font = new System.Drawing.Font("Segoe UI", 7.75f, System.Drawing.FontStyle.Bold);
            btnViewLogs.Text = "📜 Logs";
            btnViewLogs.Name = "btnViewLogs";
            btnViewLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnViewLogs.Click += btnViewLogs_Click;
            panelCard.Controls.Add(btnViewLogs);

            btnViewQR.Location = new System.Drawing.Point(lx + (btnW + btnGap) * 2, btnRowY);
            btnViewQR.Size = new System.Drawing.Size(btnW, btnH);
            btnViewQR.Font = new System.Drawing.Font("Segoe UI", 7.75f, System.Drawing.FontStyle.Bold);
            btnViewQR.Text = "QR Code";
            btnViewQR.Name = "btnViewQR";
            btnViewQR.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnViewQR.Click += btnViewQR_Click;
            panelCard.Controls.Add(btnViewQR);

            btnCheckInOut.Location = new System.Drawing.Point(lx + (btnW + btnGap) * 3, btnRowY);
            btnCheckInOut.Size = new System.Drawing.Size(btnW, btnH);
            btnCheckInOut.Font = new System.Drawing.Font("Segoe UI", 7.75f, System.Drawing.FontStyle.Bold);
            btnCheckInOut.Text = "✅ Check";
            btnCheckInOut.Name = "btnCheckInOut";
            btnCheckInOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCheckInOut.Click += btnCheckInOut_Click;
            panelCard.Controls.Add(btnCheckInOut);

            toolTipHero.SetToolTip(btnViewPicture, "View picture");
            toolTipHero.SetToolTip(btnViewLogs, "View audit logs");
            toolTipHero.SetToolTip(btnViewQR, "View QR code");
            toolTipHero.SetToolTip(btnCheckInOut, "Check In/Out history");

            // ── Field grid — compact two-column layout ─────────────────────
            void AddFieldAt(System.Windows.Forms.Label lbl, string labelText, System.Windows.Forms.Label val, int colX, int row, int width)
            {
                int y = sy + row * rh;
                lbl.Text = labelText;
                lbl.Location = new System.Drawing.Point(colX, y);
                lbl.AutoSize = true;
                val.Location = new System.Drawing.Point(colX + valOffset, y);
                val.AutoSize = false;
                val.Size = new System.Drawing.Size(width - valOffset, rh - 2);
                val.AutoEllipsis = true;
                panelCard.Controls.Add(lbl);
                panelCard.Controls.Add(val);
            }

            AddFieldAt(lblEquipmentCode, "Eqp Code:", lblEquipmentCodeValue, col1X, 0, colW);
            AddFieldAt(lblQrCode, "QR Code:", lblQrCodeValue, col2X, 0, colW);
            AddFieldAt(lblMake, "Make:", lblMakeValue, col1X, 1, colW);
            AddFieldAt(lblModel, "Model:", lblModelValue, col2X, 1, colW);
            AddFieldAt(lblSerial, "Serial #:", lblSerialValue, col1X, 2, colW);
            AddFieldAt(lblCategory, "Category:", lblCategoryValue, col2X, 2, colW);
            AddFieldAt(lblProcurementType, "Proc. Type:", lblProcurementTypeValue, col1X, 3, colW);
            AddFieldAt(lblCondition, "Condition:", lblConditionValue, col2X, 3, colW);
            AddFieldAt(lblStatus, "Status:", lblStatusValue, col1X, 4, colW);
            AddFieldAt(lblLocation, "Location:", lblLocationValue, col2X, 4, colW);
            AddFieldAt(lblEmployee, "Assigned:", lblEmployeeValue, col1X, 5, colW);
            AddFieldAt(lblPurchaseDate, "Purchased:", lblPurchaseDateValue, col2X, 5, colW);
            AddFieldAt(lblWarranty, "Warranty:", lblWarrantyValue, col1X, 6, cardContentW);
            AddFieldAt(lblVendor, "Vendor:", lblVendorValue, col1X, 7, cardContentW);

            lblStatusValue.AutoSize = true;
            lblStatusValue.Size = new System.Drawing.Size(0, 0);
            lblStatusValue.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);

            int descY = sy + 8 * rh + 10;
            lblDescriptionTitle.Text = "Description:";
            lblDescriptionTitle.Location = new System.Drawing.Point(lx, descY);
            lblDescriptionTitle.AutoSize = true;
            lblDescriptionTitle.Name = "lblDescriptionTitle";

            lblDescriptionValue.Location = new System.Drawing.Point(lx, descY + 16);
            lblDescriptionValue.Size = new System.Drawing.Size(416, 44);
            lblDescriptionValue.AutoSize = false;
            lblDescriptionValue.Name = "lblDescriptionValue";

            int notesY = descY + 16 + 44 + 8;
            lblNotesTitle.Text = "Notes:";
            lblNotesTitle.Location = new System.Drawing.Point(lx, notesY);
            lblNotesTitle.AutoSize = true;
            lblNotesTitle.Name = "lblNotesTitle";

            lblNotesValue.Location = new System.Drawing.Point(lx, notesY + 16);
            lblNotesValue.Size = new System.Drawing.Size(416, 44);
            lblNotesValue.AutoSize = false;
            lblNotesValue.Name = "lblNotesValue";

            panelCard.Controls.Add(lblDescriptionTitle);
            panelCard.Controls.Add(lblDescriptionValue);
            panelCard.Controls.Add(lblNotesTitle);
            panelCard.Controls.Add(lblNotesValue);

            // ── Bottom action row: Export + Print label ────────────────────
            int actionsY = notesY + 16 + 44 + 16;
            panelCardActions.Location = new System.Drawing.Point(lx, actionsY);
            panelCardActions.Size = new System.Drawing.Size(416, 40);
            panelCardActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            panelCardActions.WrapContents = false;
            panelCardActions.Name = "panelCardActions";
            panelCardActions.BackColor = System.Drawing.Color.Transparent;

            btnExportHistory.Text = "⬇ Export";
            btnExportHistory.Size = new System.Drawing.Size(120, 32);
            btnExportHistory.Name = "btnExportHistory";
            btnExportHistory.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnExportHistory.Click += btnExportHistory_Click;

            btnPrintLabel.Text = "🖶 Print Label";
            btnPrintLabel.Size = new System.Drawing.Size(120, 32);
            btnPrintLabel.Name = "btnPrintLabel";
            btnPrintLabel.Click += btnPrintLabel_Click;

            panelCardActions.Controls.Add(btnExportHistory);
            panelCardActions.Controls.Add(btnPrintLabel);
            int qrBoxY = actionsY;
            var qrWrapper = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(lx, qrBoxY),
                Size = new System.Drawing.Size(416, 66),
                BackColor = System.Drawing.Color.FromArgb(249, 250, 251)
            };
            picQrPreview.Location = new System.Drawing.Point(10, 5);
            picQrPreview.Size = new System.Drawing.Size(56, 56);
            picQrPreview.BackColor = System.Drawing.Color.White;
            picQrPreview.Paint += picQrPreview_Paint;
            qrWrapper.Controls.Add(picQrPreview);

            lblQrPreviewValue.Location = new System.Drawing.Point(76, 12);
            lblQrPreviewValue.AutoSize = true;
            lblQrPreviewValue.Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold);
            lblQrPreviewValue.Name = "lblQrPreviewValue";
            qrWrapper.Controls.Add(lblQrPreviewValue);

            lnkPrintLabel.Text = "Print label →";
            lnkPrintLabel.Location = new System.Drawing.Point(76, 34);
            lnkPrintLabel.AutoSize = true;
            lnkPrintLabel.Font = new System.Drawing.Font("Segoe UI", 8f);
            lnkPrintLabel.LinkClicked += (s, e) => btnPrintLabel_Click(s, EventArgs.Empty);
            qrWrapper.Controls.Add(lnkPrintLabel);

            panelCard.Controls.Add(qrWrapper);
            actionsY = qrBoxY + 66 + 10;
            panelCardActions.Location = new System.Drawing.Point(lx, actionsY);
            panelCard.Controls.Add(panelCardActions);

            // ── panelHistory (right side — audit trail) ───────────────────
            panelHistory.Location = new System.Drawing.Point(500, 168);
            panelHistory.Size = new System.Drawing.Size(680, 720);
            panelHistory.Name = "panelHistory";
            panelHistory.Anchor = System.Windows.Forms.AnchorStyles.Top |
                                    System.Windows.Forms.AnchorStyles.Left |
                                    System.Windows.Forms.AnchorStyles.Right |
                                    System.Windows.Forms.AnchorStyles.Bottom;
            panelHistory.Controls.Add(lblHistoryTitle);
            panelHistory.Controls.Add(dgvHistory);

            lblHistoryTitle.Text = "📜 Equipment History";
            lblHistoryTitle.Location = new System.Drawing.Point(0, 0);
            lblHistoryTitle.AutoSize = true;
            lblHistoryTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblHistoryTitle.Name = "lblHistoryTitle";

            dgvHistory.Location = new System.Drawing.Point(0, 36);
            dgvHistory.Size = new System.Drawing.Size(680, 676);
            dgvHistory.Anchor = System.Windows.Forms.AnchorStyles.Top |
                                              System.Windows.Forms.AnchorStyles.Left |
                                              System.Windows.Forms.AnchorStyles.Right |
                                              System.Windows.Forms.AnchorStyles.Bottom;
            dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.ReadOnly = true;
            dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.ShowCellToolTips = true;
            dgvHistory.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            dgvHistory.Name = "dgvHistory";
            dgvHistory.Columns.Add("Action", "Action");
            dgvHistory.Columns.Add("OldValue", "Previous");
            dgvHistory.Columns.Add("NewValue", "New Value");
            dgvHistory.Columns.Add("ChangedBy", "Changed By");
            dgvHistory.Columns.Add("Date", "Date");
            dgvHistory.Columns["Action"]!.FillWeight = 20;
            dgvHistory.Columns["OldValue"]!.FillWeight = 18;
            dgvHistory.Columns["NewValue"]!.FillWeight = 22;
            dgvHistory.Columns["ChangedBy"]!.FillWeight = 20;
            dgvHistory.Columns["Date"]!.FillWeight = 20;
            dgvHistory.CellDoubleClick += dgvHistory_CellDoubleClick;

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 900);
            Controls.Add(panelResult);
            Controls.Add(panelHistory);
            Controls.Add(panelTop);
            Text = "QR Scanner";
            Name = "QRScannerForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelResult.ResumeLayout(false);
            panelCard.ResumeLayout(false);
            panelCard.PerformLayout();
            panelHistory.ResumeLayout(false);
            panelHistory.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            ResumeLayout(false);
        }



        private System.Windows.Forms.CheckBox chkBulkMode = null!;
        private System.Windows.Forms.Button btnExportBulk = null!;
        private System.Windows.Forms.Panel panelTop = null!;
        private System.Windows.Forms.Label lblScanPrompt = null!;
        private System.Windows.Forms.Label lblSearchHint = null!;
        private System.Windows.Forms.TextBox txtQRCode = null!;
        private System.Windows.Forms.Button btnSearch = null!;
        private System.Windows.Forms.Button btnClear = null!;
        private System.Windows.Forms.Label lblRecentLabel = null!;
        private System.Windows.Forms.FlowLayoutPanel panelRecent = null!;
        private System.Windows.Forms.Panel panelResult = null!;
        private System.Windows.Forms.Label lblNoResult = null!;
        private System.Windows.Forms.Panel panelCard = null!;
        private System.Windows.Forms.Panel picHero = null!;
        private System.Windows.Forms.Label lblHeroName = null!;
        private System.Windows.Forms.Label lblHeroCode = null!;
        private System.Windows.Forms.Label lblHeroStatus = null!;
        private System.Windows.Forms.Button btnViewPicture = null!;
        private System.Windows.Forms.Button btnViewLogs = null!;
        private System.Windows.Forms.Button btnViewQR = null!;
        private System.Windows.Forms.Button btnCheckInOut = null!;
        private System.Windows.Forms.ToolTip toolTipHero = null!;
        private System.Windows.Forms.Label lblEquipmentCode = null!;
        private System.Windows.Forms.Label lblEquipmentCodeValue = null!;
        private System.Windows.Forms.Label lblQrCode = null!;
        private System.Windows.Forms.Label lblQrCodeValue = null!;
        private System.Windows.Forms.Label lblEquipmentName = null!;
        private System.Windows.Forms.Label lblEquipmentNameValue = null!;
        private System.Windows.Forms.Label lblMake = null!;
        private System.Windows.Forms.Label lblMakeValue = null!;
        private System.Windows.Forms.Label lblModel = null!;
        private System.Windows.Forms.Label lblModelValue = null!;
        private System.Windows.Forms.Label lblSerial = null!;
        private System.Windows.Forms.Label lblSerialValue = null!;
        private System.Windows.Forms.Label lblCategory = null!;
        private System.Windows.Forms.Label lblCategoryValue = null!;
        private System.Windows.Forms.Label lblProcurementType = null!;
        private System.Windows.Forms.Label lblProcurementTypeValue = null!;
        private System.Windows.Forms.Label lblStatus = null!;
        private System.Windows.Forms.Label lblStatusValue = null!;
        private System.Windows.Forms.Label lblCondition = null!;
        private System.Windows.Forms.Label lblConditionValue = null!;
        private System.Windows.Forms.Label lblLocation = null!;
        private System.Windows.Forms.Label lblLocationValue = null!;
        private System.Windows.Forms.Label lblEmployee = null!;
        private System.Windows.Forms.Label lblEmployeeValue = null!;
        private System.Windows.Forms.Label lblPurchaseDate = null!;
        private System.Windows.Forms.Label lblPurchaseDateValue = null!;
        private System.Windows.Forms.Label lblWarranty = null!;
        private System.Windows.Forms.Label lblWarrantyValue = null!;
        private System.Windows.Forms.Label lblVendor = null!;
        private System.Windows.Forms.Label lblVendorValue = null!;
        private System.Windows.Forms.Label lblDescriptionTitle = null!;
        private System.Windows.Forms.Label lblDescriptionValue = null!;
        private System.Windows.Forms.Label lblNotesTitle = null!;
        private System.Windows.Forms.Label lblNotesValue = null!;
        private System.Windows.Forms.FlowLayoutPanel panelCardActions = null!;
        private System.Windows.Forms.Button btnExportHistory = null!;
        private System.Windows.Forms.Button btnPrintLabel = null!;
        private System.Windows.Forms.Panel panelHistory = null!;
        private System.Windows.Forms.Label lblHistoryTitle = null!;
        private System.Windows.Forms.DataGridView dgvHistory = null!;
        private System.Windows.Forms.Panel pnlQrIcon = null!;
        private System.Windows.Forms.Label lblCardTitle = null!;
        private System.Windows.Forms.Panel picQrPreview = null!;
        private System.Windows.Forms.Label lblQrPreviewValue = null!;
        private System.Windows.Forms.LinkLabel lnkPrintLabel = null!;
        private System.Windows.Forms.FlowLayoutPanel panelSuggestions = null!;
    }
}