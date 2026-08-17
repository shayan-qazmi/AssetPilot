namespace Inventory_Management_System.Forms
{
    partial class CheckInOutForm
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
            lblScanPrompt = new System.Windows.Forms.Label();
            pnlQrIcon = new System.Windows.Forms.Panel();
            txtQRCode = new System.Windows.Forms.TextBox();
            btnSearch = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            lblNoResult = new System.Windows.Forms.Label();
            panelMainContent = new System.Windows.Forms.Panel();
            panelLeft = new System.Windows.Forms.Panel();
            lblCardTitle = new System.Windows.Forms.Label();
            lblCodeLabel = new System.Windows.Forms.Label();
            lblCodeValue = new System.Windows.Forms.Label();
            lblQrCodeLabel = new System.Windows.Forms.Label();
            lblQrCodeValue = new System.Windows.Forms.Label();
            lblNameLabel = new System.Windows.Forms.Label();
            lblNameValue = new System.Windows.Forms.Label();
            lblCategoryLabel = new System.Windows.Forms.Label();
            lblCategoryValue = new System.Windows.Forms.Label();
            lblMakeLabel = new System.Windows.Forms.Label();
            lblMakeValue = new System.Windows.Forms.Label();
            lblModelLabel = new System.Windows.Forms.Label();
            lblModelValue = new System.Windows.Forms.Label();
            lblSerialLabel = new System.Windows.Forms.Label();
            lblSerialValue = new System.Windows.Forms.Label();
            lblProcurementTypeLabel = new System.Windows.Forms.Label();
            lblProcurementTypeValue = new System.Windows.Forms.Label();
            lblStatusLabel = new System.Windows.Forms.Label();
            lblStatusValue = new System.Windows.Forms.Label();
            lblConditionLabel = new System.Windows.Forms.Label();
            lblConditionValue = new System.Windows.Forms.Label();
            lblLocationLabel = new System.Windows.Forms.Label();
            lblLocationValue = new System.Windows.Forms.Label();
            lblVendorLabel = new System.Windows.Forms.Label();
            lblVendorValue = new System.Windows.Forms.Label();
            lblPurchaseDateLabel = new System.Windows.Forms.Label();
            lblPurchaseDateValue = new System.Windows.Forms.Label();
            lblWarrantyLabel = new System.Windows.Forms.Label();
            lblWarrantyValue = new System.Windows.Forms.Label();
            lblDescriptionTitle = new System.Windows.Forms.Label();
            lblDescriptionValue = new System.Windows.Forms.Label();
            lblNotesTitle = new System.Windows.Forms.Label();
            lblNotesValue = new System.Windows.Forms.Label();
            panelCenter = new System.Windows.Forms.Panel();
            panelTabHeader = new System.Windows.Forms.Panel();
            btnTabCheckOut = new System.Windows.Forms.Button();
            btnTabCheckIn = new System.Windows.Forms.Button();
            panelTabContent = new System.Windows.Forms.Panel();
            panelCheckOut = new System.Windows.Forms.Panel();
            lblIssuedTo = new System.Windows.Forms.Label();
            txtIssuedTo = new System.Windows.Forms.TextBox();
            lblDepartment = new System.Windows.Forms.Label();
            txtDepartment = new System.Windows.Forms.TextBox();
            lblLocationOut = new System.Windows.Forms.Label();
            cmbLocationOut = new System.Windows.Forms.ComboBox();
            lblLocationHint = new System.Windows.Forms.Label();
            lblRoomOut = new System.Windows.Forms.Label();
            cmbRoomOut = new System.Windows.Forms.ComboBox();
            lblRoomHint = new System.Windows.Forms.Label();
            lblOperatorOut = new System.Windows.Forms.Label();
            txtOperatorOut = new System.Windows.Forms.TextBox();
            lblExpectedReturn = new System.Windows.Forms.Label();
            dtpExpectedReturn = new System.Windows.Forms.DateTimePicker();
            lblConditionBefore = new System.Windows.Forms.Label();
            cmbConditionBefore = new System.Windows.Forms.ComboBox();
            lblRemarksOut = new System.Windows.Forms.Label();
            txtRemarksOut = new System.Windows.Forms.TextBox();
            btnConfirmCheckOut = new System.Windows.Forms.Button();
            panelCheckIn = new System.Windows.Forms.Panel();
            lblCurrentlyIssuedTo = new System.Windows.Forms.Label();
            txtCurrentlyIssuedTo = new System.Windows.Forms.TextBox();
            lblOperatorIn = new System.Windows.Forms.Label();
            txtOperatorIn = new System.Windows.Forms.TextBox();
            lblConditionAfter = new System.Windows.Forms.Label();
            cmbConditionAfter = new System.Windows.Forms.ComboBox();
            lblRemarksIn = new System.Windows.Forms.Label();
            txtRemarksIn = new System.Windows.Forms.TextBox();
            btnConfirmCheckIn = new System.Windows.Forms.Button();
            panelBottom = new System.Windows.Forms.Panel();
            lblGridTitle = new System.Windows.Forms.Label();
            dgvRecentTransactions = new System.Windows.Forms.DataGridView();
            pnlEmptyState = new System.Windows.Forms.Panel();
            lblEmptyIcon = new System.Windows.Forms.Label();
            lblEmptyTitle = new System.Windows.Forms.Label();
            lblEmptySubtitle = new System.Windows.Forms.Label();

            panelTop.SuspendLayout();
            panelMainContent.SuspendLayout();
            panelLeft.SuspendLayout();
            panelCenter.SuspendLayout();
            panelTabHeader.SuspendLayout();
            panelTabContent.SuspendLayout();
            panelCheckOut.SuspendLayout();
            panelCheckIn.SuspendLayout();
            panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRecentTransactions).BeginInit();
            SuspendLayout();

            // ── panelTop (dock top) ───────────────────────────────────────
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 70;
            panelTop.Name = "panelTop";
            panelTop.Controls.Add(lblScanPrompt);
            panelTop.Controls.Add(pnlQrIcon);
            panelTop.Controls.Add(txtQRCode);
            panelTop.Controls.Add(btnSearch);
            panelTop.Controls.Add(btnClear);

            lblScanPrompt.Text = "Scan QR Code / Enter Equipment Code:";
            lblScanPrompt.Location = new System.Drawing.Point(12, 24);
            lblScanPrompt.AutoSize = true;
            lblScanPrompt.Name = "lblScanPrompt";

            pnlQrIcon.Location = new System.Drawing.Point(280, 17);
            pnlQrIcon.Size = new System.Drawing.Size(36, 36);
            pnlQrIcon.Name = "pnlQrIcon";
            pnlQrIcon.BackColor = System.Drawing.Color.Transparent;

            txtQRCode.Location = new System.Drawing.Point(326, 20);
            txtQRCode.Size = new System.Drawing.Size(280, 30);
            txtQRCode.PlaceholderText = "Scan QR code or type equipment code...";
            txtQRCode.Name = "txtQRCode";
            txtQRCode.Font = new System.Drawing.Font("Segoe UI", 11F);
            txtQRCode.TextChanged += txtQRCode_TextChanged;
            txtQRCode.KeyDown += txtQRCode_KeyDown;

            btnSearch.Location = new System.Drawing.Point(616, 18);
            btnSearch.Size = new System.Drawing.Size(100, 34);
            btnSearch.Text = "🔍 Search";
            btnSearch.Name = "btnSearch";
            btnSearch.Click += btnSearch_Click;

            btnClear.Location = new System.Drawing.Point(724, 18);
            btnClear.Size = new System.Drawing.Size(100, 34);
            btnClear.Text = "↺ Clear";
            btnClear.Name = "btnClear";
            btnClear.Click += btnClear_Click;

            // ── lblNoResult ───────────────────────────────────────────────
            lblNoResult.Text = "📋 Scan a QR code or enter an equipment code to start check-in or check-out";
            lblNoResult.Dock = System.Windows.Forms.DockStyle.Fill;
            lblNoResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblNoResult.Name = "lblNoResult";

            // ── panelMainContent (dock fill) ──────────────────────────────
            panelMainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            panelMainContent.Name = "panelMainContent";
            panelMainContent.Visible = false;
            panelMainContent.Controls.Add(panelCenter);
            panelMainContent.Controls.Add(panelLeft);
            panelMainContent.Controls.Add(panelBottom);

            // ── panelLeft (dock left) ──────────────────────────────────────
            panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            panelLeft.Width = 360;
            panelLeft.Name = "panelLeft";
            panelLeft.Padding = new System.Windows.Forms.Padding(16);
            panelLeft.AutoScroll = true;
            panelLeft.Controls.Add(lblCardTitle);

            lblCardTitle.Text = "📋 Equipment Details";
            lblCardTitle.Location = new System.Drawing.Point(16, 16);
            lblCardTitle.AutoSize = true;
            lblCardTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblCardTitle.Name = "lblCardTitle";

            const int lx = 16, vx = 140, vw = 188, rh = 22, sy = 56;
            void AddField(System.Windows.Forms.Label lbl, string labelText, System.Windows.Forms.Label val, int row)
            {
                lbl.Text = labelText;
                lbl.Location = new System.Drawing.Point(lx, sy + row * rh);
                lbl.AutoSize = true;
                val.Location = new System.Drawing.Point(vx, sy + row * rh);
                val.AutoSize = false;
                val.Size = new System.Drawing.Size(vw, rh - 2);
                val.AutoEllipsis = true;
                panelLeft.Controls.Add(lbl);
                panelLeft.Controls.Add(val);
            }

            AddField(lblCodeLabel, "Equipment Code:", lblCodeValue, 0);
            AddField(lblQrCodeLabel, "QR Code:", lblQrCodeValue, 1);
            AddField(lblNameLabel, "Name:", lblNameValue, 2);
            AddField(lblCategoryLabel, "Category:", lblCategoryValue, 3);
            AddField(lblMakeLabel, "Make:", lblMakeValue, 4);
            AddField(lblModelLabel, "Model:", lblModelValue, 5);
            AddField(lblSerialLabel, "Serial Number:", lblSerialValue, 6);
            AddField(lblProcurementTypeLabel, "Procurement Type:", lblProcurementTypeValue, 7);
            AddField(lblStatusLabel, "Status:", lblStatusValue, 8);
            AddField(lblConditionLabel, "Condition Grade:", lblConditionValue, 9);
            AddField(lblLocationLabel, "Location:", lblLocationValue, 10);
            AddField(lblVendorLabel, "Vendor Name:", lblVendorValue, 11);
            AddField(lblPurchaseDateLabel, "Purchase Date:", lblPurchaseDateValue, 12);
            AddField(lblWarrantyLabel, "Warranty Expiry:", lblWarrantyValue, 13);

            int descY = sy + 14 * rh + 8;
            lblDescriptionTitle.Text = "Description:";
            lblDescriptionTitle.Location = new System.Drawing.Point(lx, descY);
            lblDescriptionTitle.AutoSize = true;
            lblDescriptionTitle.Name = "lblDescriptionTitle";

            lblDescriptionValue.Location = new System.Drawing.Point(lx, descY + 16);
            lblDescriptionValue.Size = new System.Drawing.Size(328, 40);
            lblDescriptionValue.AutoSize = false;
            lblDescriptionValue.Name = "lblDescriptionValue";

            int notesY = descY + 16 + 40 + 8;
            lblNotesTitle.Text = "Notes:";
            lblNotesTitle.Location = new System.Drawing.Point(lx, notesY);
            lblNotesTitle.AutoSize = true;
            lblNotesTitle.Name = "lblNotesTitle";

            lblNotesValue.Location = new System.Drawing.Point(lx, notesY + 16);
            lblNotesValue.Size = new System.Drawing.Size(328, 40);
            lblNotesValue.AutoSize = false;
            lblNotesValue.Name = "lblNotesValue";

            panelLeft.Controls.Add(lblDescriptionTitle);
            panelLeft.Controls.Add(lblDescriptionValue);
            panelLeft.Controls.Add(lblNotesTitle);
            panelLeft.Controls.Add(lblNotesValue);

            // ── panelCenter (dock fill) ───────────────────────────────────
            panelCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            panelCenter.Name = "panelCenter";
            panelCenter.Padding = new System.Windows.Forms.Padding(16, 0, 16, 16);
            panelCenter.Controls.Add(panelTabContent);
            panelCenter.Controls.Add(panelTabHeader);

            // ── panelTabHeader ────────────────────────────────────────────
            panelTabHeader.Dock = System.Windows.Forms.DockStyle.Top;
            panelTabHeader.Height = 45;
            panelTabHeader.Name = "panelTabHeader";
            panelTabHeader.Controls.Add(btnTabCheckOut);
            panelTabHeader.Controls.Add(btnTabCheckIn);

            btnTabCheckOut.Location = new System.Drawing.Point(0, 0);
            btnTabCheckOut.Size = new System.Drawing.Size(150, 45);
            btnTabCheckOut.Text = "📤 Check-Out";
            btnTabCheckOut.Name = "btnTabCheckOut";
            btnTabCheckOut.Click += btnTabCheckOut_Click;

            btnTabCheckIn.Location = new System.Drawing.Point(154, 0);
            btnTabCheckIn.Size = new System.Drawing.Size(150, 45);
            btnTabCheckIn.Text = "📥 Check-In";
            btnTabCheckIn.Name = "btnTabCheckIn";
            btnTabCheckIn.Click += btnTabCheckIn_Click;

            // ── panelTabContent ───────────────────────────────────────────
            panelTabContent.Dock = System.Windows.Forms.DockStyle.Fill;
            panelTabContent.Name = "panelTabContent";
            panelTabContent.Controls.Add(panelCheckOut);
            panelTabContent.Controls.Add(panelCheckIn);

            // ── panelCheckOut (rounded floating card — sized/positioned at runtime) ──
            panelCheckOut.Location = new System.Drawing.Point(10, 10);
            panelCheckOut.Size = new System.Drawing.Size(1160, 550);
            panelCheckOut.AutoScroll = true;
            panelCheckOut.Name = "panelCheckOut";
            panelCheckOut.Padding = new System.Windows.Forms.Padding(16);
            panelCheckOut.Visible = true;

            int outY = 16, outH = 26, outLGap = 6, labelWidth = 140, controlWidth = 320;

            void AddCheckoutField(System.Windows.Forms.Label lbl, string txt, System.Windows.Forms.Control ctrl, bool personIcon = false)
            {
                lbl.Text = txt;
                lbl.Location = new System.Drawing.Point(16, outY + outLGap);
                lbl.Size = new System.Drawing.Size(labelWidth, outH);
                panelCheckOut.Controls.Add(lbl);
                panelCheckOut.Controls.Add(CreateInputHost(ctrl, new System.Drawing.Point(160, outY), new System.Drawing.Size(controlWidth, outH), personIcon));
                outY += 38;
                panelCheckOut.AutoScroll = true;
            }

            AddCheckoutField(lblIssuedTo, "Issued To:", txtIssuedTo, personIcon: true);

            void AddCheckoutDropdownField(System.Windows.Forms.Label lbl, string txt, System.Windows.Forms.ComboBox cmb, System.Windows.Forms.Label hint, string hintText)
            {
                lbl.Text = txt;
                lbl.Location = new System.Drawing.Point(16, outY + outLGap);
                lbl.Size = new System.Drawing.Size(labelWidth, outH);
                panelCheckOut.Controls.Add(lbl);
                cmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                cmb.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
                cmb.DrawItem += (s, e) =>
                {
                    if (e.Index < 0) return;
                    bool selected = (e.State & System.Windows.Forms.DrawItemState.Selected) == System.Windows.Forms.DrawItemState.Selected;
                    using var bg = new System.Drawing.SolidBrush(selected ? System.Drawing.Color.FromArgb(224, 231, 255) : System.Drawing.Color.White);
                    e.Graphics.FillRectangle(bg, e.Bounds);
                    string text = cmb.Items.Count > e.Index ? cmb.GetItemText(cmb.Items[e.Index]) : "";
                    using var fg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(17, 24, 39));
                    var sf = new System.Drawing.StringFormat { LineAlignment = System.Drawing.StringAlignment.Center };
                    e.Graphics.DrawString(text, cmb.Font, fg, e.Bounds, sf);
                    e.DrawFocusRectangle();
                };
                panelCheckOut.Controls.Add(CreateInputHost(cmb, new System.Drawing.Point(160, outY), new System.Drawing.Size(controlWidth, outH))); 
                outY += 30;
                hint.Text = hintText;
                hint.Font = new System.Drawing.Font("Segoe UI", 7.8f, System.Drawing.FontStyle.Italic);
                hint.AutoSize = true;
                hint.Location = new System.Drawing.Point(160, outY);
                panelCheckOut.Controls.Add(hint);
                outY += 22;
            }

            AddCheckoutDropdownField(lblLocationOut, "Location:  *", cmbLocationOut, lblLocationHint, "Required — the site or building");
            AddCheckoutDropdownField(lblRoomOut, "Room:", cmbRoomOut, lblRoomHint, "Optional — narrows to a specific room");    

            AddCheckoutField(lblOperatorOut, "Operator Name:", txtOperatorOut, personIcon: true);
            txtOperatorOut.ReadOnly = true;

            lblExpectedReturn.Text = "Expected Return:";
            lblExpectedReturn.Location = new System.Drawing.Point(16, outY + outLGap);
            lblExpectedReturn.Size = new System.Drawing.Size(labelWidth, outH);
            dtpExpectedReturn.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            panelCheckOut.Controls.Add(lblExpectedReturn);
            panelCheckOut.Controls.Add(CreateInputHost(dtpExpectedReturn, new System.Drawing.Point(160, outY), new System.Drawing.Size(controlWidth, outH)));
            outY += 38;

            lblConditionBefore.Text = "Condition Before:";
            lblConditionBefore.Location = new System.Drawing.Point(16, outY + outLGap);
            lblConditionBefore.Size = new System.Drawing.Size(labelWidth, outH);
            cmbConditionBefore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbConditionBefore.Items.AddRange(new object[] { "New", "Good", "Fair", "Poor", "Unserviceable" });
            panelCheckOut.Controls.Add(lblConditionBefore);
            panelCheckOut.Controls.Add(CreateInputHost(cmbConditionBefore, new System.Drawing.Point(160, outY), new System.Drawing.Size(controlWidth, outH)));
            outY += 38;

            lblRemarksOut.Text = "Remarks:";
            lblRemarksOut.Location = new System.Drawing.Point(16, outY + outLGap);
            lblRemarksOut.Size = new System.Drawing.Size(labelWidth, outH);
            txtRemarksOut.Multiline = true;
            txtRemarksOut.Size = new System.Drawing.Size(controlWidth, 75);
            txtRemarksOut.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            panelCheckOut.Controls.Add(lblRemarksOut);
            panelCheckOut.Controls.Add(CreateInputHost(txtRemarksOut, new System.Drawing.Point(160, outY), new System.Drawing.Size(controlWidth, 75)));
            outY += 85;

            btnConfirmCheckOut.Location = new System.Drawing.Point(160, outY);
            btnConfirmCheckOut.Size = new System.Drawing.Size(controlWidth, 40);
            btnConfirmCheckOut.Text = "✅ Confirm Check-Out";
            btnConfirmCheckOut.Name = "btnConfirmCheckOut";
            btnConfirmCheckOut.Click += btnConfirmCheckOut_Click;
            panelCheckOut.Controls.Add(btnConfirmCheckOut);

            // ── panelCheckIn (rounded floating card) ────────────────────────
            panelCheckIn.Location = new System.Drawing.Point(10, 10);
            panelCheckIn.Size = new System.Drawing.Size(1160, 550);
            panelCheckIn.Name = "panelCheckIn";
            panelCheckIn.Padding = new System.Windows.Forms.Padding(16);
            panelCheckIn.Visible = false;

            int inY = 16;
            void AddCheckinField(System.Windows.Forms.Label lbl, string txt, System.Windows.Forms.Control ctrl, bool personIcon = false)
            {
                lbl.Text = txt;
                lbl.Location = new System.Drawing.Point(16, inY + outLGap);
                lbl.Size = new System.Drawing.Size(labelWidth, outH);
                panelCheckIn.Controls.Add(lbl);
                panelCheckIn.Controls.Add(CreateInputHost(ctrl, new System.Drawing.Point(160, inY), new System.Drawing.Size(controlWidth, outH), personIcon));
                inY += 38;
            }

            AddCheckinField(lblCurrentlyIssuedTo, "Currently Issued To:", txtCurrentlyIssuedTo, personIcon: true);
            txtCurrentlyIssuedTo.ReadOnly = true;

            AddCheckinField(lblOperatorIn, "Operator Name:", txtOperatorIn, personIcon: true);
            txtOperatorIn.ReadOnly = true;

            lblConditionAfter.Text = "Condition After:";
            lblConditionAfter.Location = new System.Drawing.Point(16, inY + outLGap);
            lblConditionAfter.Size = new System.Drawing.Size(labelWidth, outH);
            cmbConditionAfter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbConditionAfter.Items.AddRange(new object[] { "New", "Good", "Fair", "Poor", "Unserviceable" });
            panelCheckIn.Controls.Add(lblConditionAfter);
            panelCheckIn.Controls.Add(CreateInputHost(cmbConditionAfter, new System.Drawing.Point(160, inY), new System.Drawing.Size(controlWidth, outH)));
            inY += 38;

            lblRemarksIn.Text = "Remarks:";
            lblRemarksIn.Location = new System.Drawing.Point(16, inY + outLGap);
            lblRemarksIn.Size = new System.Drawing.Size(labelWidth, outH);
            txtRemarksIn.Multiline = true;
            txtRemarksIn.Size = new System.Drawing.Size(controlWidth, 75);
            txtRemarksIn.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            panelCheckIn.Controls.Add(lblRemarksIn);
            panelCheckIn.Controls.Add(CreateInputHost(txtRemarksIn, new System.Drawing.Point(160, inY), new System.Drawing.Size(controlWidth, 75)));
            inY += 85;

            btnConfirmCheckIn.Location = new System.Drawing.Point(160, inY);
            btnConfirmCheckIn.Size = new System.Drawing.Size(controlWidth, 40);
            btnConfirmCheckIn.Text = "✅ Confirm Check-In";
            btnConfirmCheckIn.Name = "btnConfirmCheckIn";
            btnConfirmCheckIn.Click += btnConfirmCheckIn_Click;
            panelCheckIn.Controls.Add(btnConfirmCheckIn);

            // ── panelBottom (dock bottom) ─────────────────────────────────
            panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Height = 220;
            panelBottom.Name = "panelBottom";
            panelBottom.Padding = new System.Windows.Forms.Padding(16, 8, 16, 16);
            panelBottom.Controls.Add(lblGridTitle);
            panelBottom.Controls.Add(dgvRecentTransactions);

            lblGridTitle.Text = "🕐 Recent Transactions (Last 10)";
            lblGridTitle.Location = new System.Drawing.Point(16, 8);
            lblGridTitle.AutoSize = true;
            lblGridTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblGridTitle.Name = "lblGridTitle";

            dgvRecentTransactions.Location = new System.Drawing.Point(16, 32);
            dgvRecentTransactions.Size = new System.Drawing.Size(1168, 172);
            dgvRecentTransactions.Anchor = System.Windows.Forms.AnchorStyles.Top |
                                            System.Windows.Forms.AnchorStyles.Left |
                                            System.Windows.Forms.AnchorStyles.Right |
                                            System.Windows.Forms.AnchorStyles.Bottom;
            dgvRecentTransactions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvRecentTransactions.AllowUserToAddRows = false;
            dgvRecentTransactions.ReadOnly = true;
            dgvRecentTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvRecentTransactions.Name = "dgvRecentTransactions";
            dgvRecentTransactions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvRecentTransactions.ColumnHeadersHeight = 32;
            dgvRecentTransactions.Columns.Add("Date", "Date");
            dgvRecentTransactions.Columns.Add("Type", "Type");
            dgvRecentTransactions.Columns.Add("IssuedTo", "Issued To");
            dgvRecentTransactions.Columns.Add("Department", "Department");
            dgvRecentTransactions.Columns.Add("ExpectedReturn", "Expected Return");
            dgvRecentTransactions.Columns.Add("Status", "Status");
            dgvRecentTransactions.Columns.Add("ConditionBefore", "Condition Before");
            dgvRecentTransactions.Columns.Add("ConditionAfter", "Condition After");
            dgvRecentTransactions.Columns.Add("Remarks", "Remarks");

            pnlEmptyState.Location = new System.Drawing.Point(
                dgvRecentTransactions.Left,
                dgvRecentTransactions.Top + dgvRecentTransactions.ColumnHeadersHeight);
            pnlEmptyState.Size = new System.Drawing.Size(
                dgvRecentTransactions.Width,
                dgvRecentTransactions.Height - dgvRecentTransactions.ColumnHeadersHeight);
            pnlEmptyState.Anchor = dgvRecentTransactions.Anchor;
            pnlEmptyState.BackColor = System.Drawing.Color.White;
            pnlEmptyState.Name = "pnlEmptyState";
            pnlEmptyState.Visible = false;
            panelBottom.Controls.Add(pnlEmptyState);

            lblEmptyIcon.Text = "📥";
            lblEmptyIcon.Font = new System.Drawing.Font("Segoe UI Emoji", 26F);
            lblEmptyIcon.Name = "lblEmptyIcon";

            lblEmptyTitle.Text = "No recent transactions found.";
            lblEmptyTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblEmptyTitle.Name = "lblEmptyTitle";

            lblEmptySubtitle.Text = "Transactions will appear here.";
            lblEmptySubtitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lblEmptySubtitle.Name = "lblEmptySubtitle";

            // ── Form configuration ────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1200, 700);
            Controls.Add(panelMainContent);
            Controls.Add(lblNoResult);
            Controls.Add(panelTop);
            Text = "Reception Check In/Out";
            Name = "CheckInOutForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelMainContent.ResumeLayout(false);
            panelLeft.ResumeLayout(false);
            panelLeft.PerformLayout();
            panelCenter.ResumeLayout(false);
            panelTabHeader.ResumeLayout(false);
            panelTabContent.ResumeLayout(false);
            panelCheckOut.ResumeLayout(false);
            panelCheckOut.PerformLayout();
            panelCheckIn.ResumeLayout(false);
            panelCheckIn.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRecentTransactions).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblScanPrompt;
        private System.Windows.Forms.Panel pnlQrIcon;
        private System.Windows.Forms.TextBox txtQRCode;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblNoResult;
        private System.Windows.Forms.Panel panelMainContent;

        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Label lblCardTitle;
        private System.Windows.Forms.Label lblCodeLabel;
        private System.Windows.Forms.Label lblCodeValue;
        private System.Windows.Forms.Label lblQrCodeLabel;
        private System.Windows.Forms.Label lblQrCodeValue;
        private System.Windows.Forms.Label lblNameLabel;
        private System.Windows.Forms.Label lblNameValue;
        private System.Windows.Forms.Label lblCategoryLabel;
        private System.Windows.Forms.Label lblCategoryValue;
        private System.Windows.Forms.Label lblMakeLabel;
        private System.Windows.Forms.Label lblMakeValue;
        private System.Windows.Forms.Label lblModelLabel;
        private System.Windows.Forms.Label lblModelValue;
        private System.Windows.Forms.Label lblSerialLabel;
        private System.Windows.Forms.Label lblSerialValue;
        private System.Windows.Forms.Label lblProcurementTypeLabel;
        private System.Windows.Forms.Label lblProcurementTypeValue;
        private System.Windows.Forms.Label lblStatusLabel;
        private System.Windows.Forms.Label lblStatusValue;
        private System.Windows.Forms.Label lblConditionLabel;
        private System.Windows.Forms.Label lblConditionValue;
        private System.Windows.Forms.Label lblLocationLabel;
        private System.Windows.Forms.Label lblLocationValue;
        private System.Windows.Forms.Label lblVendorLabel;
        private System.Windows.Forms.Label lblVendorValue;
        private System.Windows.Forms.Label lblPurchaseDateLabel;
        private System.Windows.Forms.Label lblPurchaseDateValue;
        private System.Windows.Forms.Label lblWarrantyLabel;
        private System.Windows.Forms.Label lblWarrantyValue;
        private System.Windows.Forms.Label lblDescriptionTitle;
        private System.Windows.Forms.Label lblDescriptionValue;
        private System.Windows.Forms.Label lblNotesTitle;
        private System.Windows.Forms.Label lblNotesValue;

        private System.Windows.Forms.Panel panelCenter;
        private System.Windows.Forms.Panel panelTabHeader;
        private System.Windows.Forms.Button btnTabCheckOut;
        private System.Windows.Forms.Button btnTabCheckIn;
        private System.Windows.Forms.Panel panelTabContent;

        private System.Windows.Forms.Panel panelCheckOut;
        private System.Windows.Forms.Label lblIssuedTo;
        private System.Windows.Forms.TextBox txtIssuedTo;
        private System.Windows.Forms.Label lblDepartment;
        private System.Windows.Forms.TextBox txtDepartment;
        private System.Windows.Forms.Label lblLocationOut;
        private System.Windows.Forms.ComboBox cmbLocationOut;
        private System.Windows.Forms.Label lblLocationHint;
        private System.Windows.Forms.Label lblRoomOut;
        private System.Windows.Forms.ComboBox cmbRoomOut;
        private System.Windows.Forms.Label lblRoomHint;
        private System.Windows.Forms.Label lblOperatorOut;
        private System.Windows.Forms.TextBox txtOperatorOut;
        private System.Windows.Forms.Label lblExpectedReturn;
        private System.Windows.Forms.DateTimePicker dtpExpectedReturn;
        private System.Windows.Forms.Label lblConditionBefore;
        private System.Windows.Forms.ComboBox cmbConditionBefore;
        private System.Windows.Forms.Label lblRemarksOut;
        private System.Windows.Forms.TextBox txtRemarksOut;
        private System.Windows.Forms.Button btnConfirmCheckOut;

        private System.Windows.Forms.Panel panelCheckIn;
        private System.Windows.Forms.Label lblCurrentlyIssuedTo;
        private System.Windows.Forms.TextBox txtCurrentlyIssuedTo;
        private System.Windows.Forms.Label lblOperatorIn;
        private System.Windows.Forms.TextBox txtOperatorIn;
        private System.Windows.Forms.Label lblConditionAfter;
        private System.Windows.Forms.ComboBox cmbConditionAfter;
        private System.Windows.Forms.Label lblRemarksIn;
        private System.Windows.Forms.TextBox txtRemarksIn;
        private System.Windows.Forms.Button btnConfirmCheckIn;

        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblGridTitle;
        private System.Windows.Forms.DataGridView dgvRecentTransactions;
        private System.Windows.Forms.Panel pnlEmptyState;
        private System.Windows.Forms.Label lblEmptyIcon;
        private System.Windows.Forms.Label lblEmptyTitle;
        private System.Windows.Forms.Label lblEmptySubtitle;
    }
}