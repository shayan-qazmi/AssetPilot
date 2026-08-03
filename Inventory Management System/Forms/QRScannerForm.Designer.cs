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
            panelTop = new System.Windows.Forms.Panel();
            lblScanPrompt = new System.Windows.Forms.Label();
            pnlQrIcon = new System.Windows.Forms.Panel();
            txtQRCode = new System.Windows.Forms.TextBox();
            btnSearch = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            panelResult               = new System.Windows.Forms.Panel();
            lblNoResult               = new System.Windows.Forms.Label();
            panelCard                 = new System.Windows.Forms.Panel();
            panelCard = new System.Windows.Forms.Panel();
            lblCardTitle = new System.Windows.Forms.Label();
            lblEquipmentCode          = new System.Windows.Forms.Label();
            lblEquipmentCodeValue     = new System.Windows.Forms.Label();
            lblQrCode                 = new System.Windows.Forms.Label();
            lblQrCodeValue            = new System.Windows.Forms.Label();
            lblEquipmentName          = new System.Windows.Forms.Label();
            lblEquipmentNameValue     = new System.Windows.Forms.Label();
            lblMake                   = new System.Windows.Forms.Label();
            lblMakeValue              = new System.Windows.Forms.Label();
            lblModel                  = new System.Windows.Forms.Label();
            lblModelValue             = new System.Windows.Forms.Label();
            lblSerial                 = new System.Windows.Forms.Label();
            lblSerialValue            = new System.Windows.Forms.Label();
            lblCategory               = new System.Windows.Forms.Label();
            lblCategoryValue          = new System.Windows.Forms.Label();
            lblProcurementType        = new System.Windows.Forms.Label();
            lblProcurementTypeValue   = new System.Windows.Forms.Label();
            lblStatus                 = new System.Windows.Forms.Label();
            lblStatusValue            = new System.Windows.Forms.Label();
            lblCondition              = new System.Windows.Forms.Label();
            lblConditionValue         = new System.Windows.Forms.Label();
            lblLocation               = new System.Windows.Forms.Label();
            lblLocationValue          = new System.Windows.Forms.Label();
            lblEmployee               = new System.Windows.Forms.Label();
            lblEmployeeValue          = new System.Windows.Forms.Label();
            lblPurchaseDate           = new System.Windows.Forms.Label();
            lblPurchaseDateValue      = new System.Windows.Forms.Label();
            lblWarranty               = new System.Windows.Forms.Label();
            lblWarrantyValue          = new System.Windows.Forms.Label();
            lblVendor                 = new System.Windows.Forms.Label();
            lblVendorValue            = new System.Windows.Forms.Label();
            lblDescriptionTitle       = new System.Windows.Forms.Label();
            lblDescriptionValue       = new System.Windows.Forms.Label();
            lblNotesTitle             = new System.Windows.Forms.Label();
            lblNotesValue             = new System.Windows.Forms.Label();
            panelHistory              = new System.Windows.Forms.Panel();
            lblHistoryTitle           = new System.Windows.Forms.Label();
            dgvHistory                = new System.Windows.Forms.DataGridView();

            panelTop.SuspendLayout();
            panelResult.SuspendLayout();
            panelCard.SuspendLayout();
            panelHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            SuspendLayout();

            // ── panelTop ──────────────────────────────────────────────────
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 70;
            panelTop.Name = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(lblScanPrompt);
            panelTop.Controls.Add(pnlQrIcon);
            panelTop.Controls.Add(txtQRCode);
            panelTop.Controls.Add(btnSearch);
            panelTop.Controls.Add(btnClear);

            lblScanPrompt.Text = "Scan QR / Enter Code:";
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
            btnClear.Name = "btnClear";
            btnClear.Click += btnClear_Click;

            // ── panelResult (left side — equipment card) ──────────────────
            panelResult.Location  = new System.Drawing.Point(20, 90);
            panelResult.Size      = new System.Drawing.Size(460, 580);
            panelResult.Name      = "panelResult";
            panelResult.Anchor    = System.Windows.Forms.AnchorStyles.Top |
                                    System.Windows.Forms.AnchorStyles.Left |
                                    System.Windows.Forms.AnchorStyles.Bottom;
            panelResult.Controls.Add(lblNoResult);
            panelResult.Controls.Add(panelCard);

            lblNoResult.Text      = "📷  Scan a QR code or enter an equipment code to see details";
            lblNoResult.AutoSize  = false;
            lblNoResult.Size      = new System.Drawing.Size(440, 60);
            lblNoResult.Location  = new System.Drawing.Point(0, 20);
            lblNoResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblNoResult.Name      = "lblNoResult";

            // ── panelCard (compact single-column detail list) ─────────────
            panelCard.Location = new System.Drawing.Point(0, 0);
            panelCard.Size = new System.Drawing.Size(440, 560);
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

            const int lx = 12, vx = 140, vw = 288, rh = 22, sy = 52;

            void AddField(System.Windows.Forms.Label lbl, string labelText, System.Windows.Forms.Label val, int row)
            {
                lbl.Text      = labelText;
                lbl.Location  = new System.Drawing.Point(lx, sy + row * rh);
                lbl.AutoSize  = true;
                lbl.Name      = lbl.Name;
                val.Location     = new System.Drawing.Point(vx, sy + row * rh);
                val.AutoSize     = false;
                val.Size         = new System.Drawing.Size(vw, rh - 2);
                val.AutoEllipsis = true;
                val.Name         = val.Name;
                panelCard.Controls.Add(lbl);
                panelCard.Controls.Add(val);
            }

            AddField(lblEquipmentCode,   "Equipment Code:",  lblEquipmentCodeValue,   0);
            AddField(lblQrCode,          "QR Code:",         lblQrCodeValue,          1);
            AddField(lblEquipmentName,   "Name:",            lblEquipmentNameValue,   2);
            AddField(lblMake,            "Make:",            lblMakeValue,            3);
            AddField(lblModel,           "Model:",           lblModelValue,           4);
            AddField(lblSerial,          "Serial Number:",   lblSerialValue,          5);
            AddField(lblCategory,        "Category:",        lblCategoryValue,        6);
            AddField(lblProcurementType, "Procurement Type:",lblProcurementTypeValue, 7);
            AddField(lblStatus,          "Status:",          lblStatusValue,          8);
            AddField(lblCondition,       "Condition:",       lblConditionValue,       9);
            AddField(lblLocation,        "Location:",        lblLocationValue,        10);
            AddField(lblEmployee,        "Assigned To:",     lblEmployeeValue,        11);
            AddField(lblPurchaseDate,    "Purchase Date:",   lblPurchaseDateValue,    12);
            AddField(lblWarranty,        "Warranty Expiry:", lblWarrantyValue,        13);
            AddField(lblVendor,          "Vendor Name:",     lblVendorValue,          14);

            int descY = sy + 15 * rh + 8;
            lblDescriptionTitle.Text     = "Description:";
            lblDescriptionTitle.Location = new System.Drawing.Point(lx, descY);
            lblDescriptionTitle.AutoSize = true;
            lblDescriptionTitle.Name     = "lblDescriptionTitle";

            lblDescriptionValue.Location  = new System.Drawing.Point(lx, descY + 16);
            lblDescriptionValue.Size      = new System.Drawing.Size(416, 44);
            lblDescriptionValue.AutoSize  = false;
            lblDescriptionValue.Name      = "lblDescriptionValue";

            int notesY = descY + 16 + 44 + 8;
            lblNotesTitle.Text     = "Notes:";
            lblNotesTitle.Location = new System.Drawing.Point(lx, notesY);
            lblNotesTitle.AutoSize = true;
            lblNotesTitle.Name     = "lblNotesTitle";

            lblNotesValue.Location  = new System.Drawing.Point(lx, notesY + 16);
            lblNotesValue.Size      = new System.Drawing.Size(416, 44);
            lblNotesValue.AutoSize  = false;
            lblNotesValue.Name      = "lblNotesValue";

            panelCard.Controls.Add(lblDescriptionTitle);
            panelCard.Controls.Add(lblDescriptionValue);
            panelCard.Controls.Add(lblNotesTitle);
            panelCard.Controls.Add(lblNotesValue);

            // ── panelHistory (right side — audit trail) ───────────────────
            panelHistory.Location = new System.Drawing.Point(500, 90);
            panelHistory.Size     = new System.Drawing.Size(680, 580);
            panelHistory.Name     = "panelHistory";
            panelHistory.Anchor   = System.Windows.Forms.AnchorStyles.Top |
                                    System.Windows.Forms.AnchorStyles.Left |
                                    System.Windows.Forms.AnchorStyles.Right |
                                    System.Windows.Forms.AnchorStyles.Bottom;
            panelHistory.Controls.Add(lblHistoryTitle);
            panelHistory.Controls.Add(dgvHistory);

            lblHistoryTitle.Text     = "Equipment History";
            lblHistoryTitle.Location = new System.Drawing.Point(0, 0);
            lblHistoryTitle.AutoSize = true;
            lblHistoryTitle.Name     = "lblHistoryTitle";

            dgvHistory.Location             = new System.Drawing.Point(0, 28);
            dgvHistory.Size                 = new System.Drawing.Size(680, 540);
            dgvHistory.Anchor               = System.Windows.Forms.AnchorStyles.Top |
                                              System.Windows.Forms.AnchorStyles.Left |
                                              System.Windows.Forms.AnchorStyles.Right |
                                              System.Windows.Forms.AnchorStyles.Bottom;
            dgvHistory.AutoSizeColumnsMode  = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistory.AllowUserToAddRows   = false;
            dgvHistory.ReadOnly             = true;
            dgvHistory.SelectionMode        = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.Name                 = "dgvHistory";
            dgvHistory.Columns.Add("Action",    "Action");
            dgvHistory.Columns.Add("OldValue",  "Previous");
            dgvHistory.Columns.Add("NewValue",  "New Value");
            dgvHistory.Columns.Add("ChangedBy", "Changed By");
            dgvHistory.Columns.Add("Date",      "Date");

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
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

        private System.Windows.Forms.Panel        panelTop = null!;
        private System.Windows.Forms.Label        lblScanPrompt = null!;
        private System.Windows.Forms.TextBox      txtQRCode = null!;
        private System.Windows.Forms.Button       btnSearch = null!;
        private System.Windows.Forms.Button       btnClear = null!;
        private System.Windows.Forms.Panel        panelResult = null!;
        private System.Windows.Forms.Label        lblNoResult = null!;
        private System.Windows.Forms.Panel        panelCard = null!;
        private System.Windows.Forms.Label        lblEquipmentCode = null!;
        private System.Windows.Forms.Label        lblEquipmentCodeValue = null!;
        private System.Windows.Forms.Label        lblQrCode = null!;
        private System.Windows.Forms.Label        lblQrCodeValue = null!;
        private System.Windows.Forms.Label        lblEquipmentName = null!;
        private System.Windows.Forms.Label        lblEquipmentNameValue = null!;
        private System.Windows.Forms.Label        lblMake = null!;
        private System.Windows.Forms.Label        lblMakeValue = null!;
        private System.Windows.Forms.Label        lblModel = null!;
        private System.Windows.Forms.Label        lblModelValue = null!;
        private System.Windows.Forms.Label        lblSerial = null!;
        private System.Windows.Forms.Label        lblSerialValue = null!;
        private System.Windows.Forms.Label        lblCategory = null!;
        private System.Windows.Forms.Label        lblCategoryValue = null!;
        private System.Windows.Forms.Label        lblProcurementType = null!;
        private System.Windows.Forms.Label        lblProcurementTypeValue = null!;
        private System.Windows.Forms.Label        lblStatus = null!;
        private System.Windows.Forms.Label        lblStatusValue = null!;
        private System.Windows.Forms.Label        lblCondition = null!;
        private System.Windows.Forms.Label        lblConditionValue = null!;
        private System.Windows.Forms.Label        lblLocation = null!;
        private System.Windows.Forms.Label        lblLocationValue = null!;
        private System.Windows.Forms.Label        lblEmployee = null!;
        private System.Windows.Forms.Label        lblEmployeeValue = null!;
        private System.Windows.Forms.Label        lblPurchaseDate = null!;
        private System.Windows.Forms.Label        lblPurchaseDateValue = null!;
        private System.Windows.Forms.Label        lblWarranty = null!;
        private System.Windows.Forms.Label        lblWarrantyValue = null!;
        private System.Windows.Forms.Label        lblVendor = null!;
        private System.Windows.Forms.Label        lblVendorValue = null!;
        private System.Windows.Forms.Label        lblDescriptionTitle = null!;
        private System.Windows.Forms.Label        lblDescriptionValue = null!;
        private System.Windows.Forms.Label        lblNotesTitle = null!;
        private System.Windows.Forms.Label        lblNotesValue = null!;
        private System.Windows.Forms.Panel        panelHistory = null!;
        private System.Windows.Forms.Label        lblHistoryTitle = null!;
        private System.Windows.Forms.DataGridView dgvHistory = null!;
        private System.Windows.Forms.Panel pnlQrIcon = null!;
        private System.Windows.Forms.Label lblCardTitle = null!;
    }
}