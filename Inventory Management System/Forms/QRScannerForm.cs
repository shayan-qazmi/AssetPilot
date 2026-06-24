// Forms/QRScannerForm.Designer.cs
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
            panelTop              = new System.Windows.Forms.Panel();
            lblScanPrompt         = new System.Windows.Forms.Label();
            txtQRCode             = new System.Windows.Forms.TextBox();
            btnSearch             = new System.Windows.Forms.Button();
            btnClear              = new System.Windows.Forms.Button();
            panelResult           = new System.Windows.Forms.Panel();
            lblNoResult           = new System.Windows.Forms.Label();
            panelCard             = new System.Windows.Forms.Panel();
            lblEquipmentCode      = new System.Windows.Forms.Label();
            lblEquipmentCodeValue = new System.Windows.Forms.Label();
            lblEquipmentName      = new System.Windows.Forms.Label();
            lblEquipmentNameValue = new System.Windows.Forms.Label();
            lblMake               = new System.Windows.Forms.Label();
            lblMakeValue          = new System.Windows.Forms.Label();
            lblModel              = new System.Windows.Forms.Label();
            lblModelValue         = new System.Windows.Forms.Label();
            lblSerial             = new System.Windows.Forms.Label();
            lblSerialValue        = new System.Windows.Forms.Label();
            lblStatus             = new System.Windows.Forms.Label();
            lblStatusValue        = new System.Windows.Forms.Label();
            lblLocation           = new System.Windows.Forms.Label();
            lblLocationValue      = new System.Windows.Forms.Label();
            lblEmployee           = new System.Windows.Forms.Label();
            lblEmployeeValue      = new System.Windows.Forms.Label();
            lblPurchaseDate       = new System.Windows.Forms.Label();
            lblPurchaseDateValue  = new System.Windows.Forms.Label();
            panelHistory          = new System.Windows.Forms.Panel();
            lblHistoryTitle       = new System.Windows.Forms.Label();
            dgvHistory            = new System.Windows.Forms.DataGridView();

            panelTop.SuspendLayout();
            panelResult.SuspendLayout();
            panelCard.SuspendLayout();
            panelHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            SuspendLayout();

            // ── panelTop ──────────────────────────────────────────────────
            panelTop.Dock      = System.Windows.Forms.DockStyle.Top;
            panelTop.Height    = 70;
            panelTop.Name      = "panelTop";
            panelTop.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            panelTop.Controls.Add(lblScanPrompt);
            panelTop.Controls.Add(txtQRCode);
            panelTop.Controls.Add(btnSearch);
            panelTop.Controls.Add(btnClear);

            lblScanPrompt.Text      = "Scan QR / Enter Code:";
            lblScanPrompt.Location  = new System.Drawing.Point(12, 24);
            lblScanPrompt.AutoSize  = true;
            lblScanPrompt.Name      = "lblScanPrompt";

            txtQRCode.Location        = new System.Drawing.Point(170, 20);
            txtQRCode.Size            = new System.Drawing.Size(320, 30);
            txtQRCode.PlaceholderText = "Scan QR code or type equipment code...";
            txtQRCode.Name            = "txtQRCode";
            txtQRCode.Font            = new System.Drawing.Font("Segoe UI", 11F);
            txtQRCode.TextChanged    += txtQRCode_TextChanged;
            txtQRCode.KeyDown        += txtQRCode_KeyDown;

            btnSearch.Location = new System.Drawing.Point(500, 18);
            btnSearch.Size     = new System.Drawing.Size(100, 34);
            btnSearch.Text     = "🔍 Search";
            btnSearch.Name     = "btnSearch";
            btnSearch.Click   += btnSearch_Click;

            btnClear.Location = new System.Drawing.Point(608, 18);
            btnClear.Size     = new System.Drawing.Size(90, 34);
            btnClear.Text     = "Clear";
            btnClear.Name     = "btnClear";
            btnClear.Click   += btnClear_Click;

            // ── panelResult (left side — equipment card) ──────────────────
            panelResult.Location  = new System.Drawing.Point(20, 90);
            panelResult.Size      = new System.Drawing.Size(460, 560);
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

            // ── panelCard ─────────────────────────────────────────────────
            panelCard.Location  = new System.Drawing.Point(0, 0);
            panelCard.Size      = new System.Drawing.Size(440, 500);
            panelCard.Name      = "panelCard";
            panelCard.Visible   = false;
            panelCard.BackColor = System.Drawing.Color.White;

            int lx = 16, vx = 180, rh = 40, sy = 16;
            void AddField(System.Windows.Forms.Label lbl, string labelText, System.Windows.Forms.Label val, int row)
            {
                lbl.Text     = labelText;
                lbl.Location = new System.Drawing.Point(lx, sy + row * rh);
                lbl.AutoSize = true;
                lbl.Name     = lbl.Name;
                val.Location = new System.Drawing.Point(vx, sy + row * rh);
                val.AutoSize = true;
                val.Name     = val.Name;
                panelCard.Controls.Add(lbl);
                panelCard.Controls.Add(val);
            }

            AddField(lblEquipmentCode, "Equipment Code:", lblEquipmentCodeValue, 0);
            AddField(lblEquipmentName, "Name:",           lblEquipmentNameValue, 1);
            AddField(lblMake,          "Make:",           lblMakeValue,          2);
            AddField(lblModel,         "Model:",          lblModelValue,         3);
            AddField(lblSerial,        "Serial Number:",  lblSerialValue,        4);
            AddField(lblStatus,        "Status:",         lblStatusValue,        5);
            AddField(lblLocation,      "Location:",       lblLocationValue,      6);
            AddField(lblEmployee,      "Assigned To:",    lblEmployeeValue,      7);
            AddField(lblPurchaseDate,  "Purchase Date:",  lblPurchaseDateValue,  8);

            // ── panelHistory (right side — audit trail) ───────────────────
            panelHistory.Location = new System.Drawing.Point(500, 90);
            panelHistory.Size     = new System.Drawing.Size(680, 560);
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
            dgvHistory.Size                 = new System.Drawing.Size(680, 520);
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

        private System.Windows.Forms.Panel        panelTop;
        private System.Windows.Forms.Label        lblScanPrompt;
        private System.Windows.Forms.TextBox      txtQRCode;
        private System.Windows.Forms.Button       btnSearch;
        private System.Windows.Forms.Button       btnClear;
        private System.Windows.Forms.Panel        panelResult;
        private System.Windows.Forms.Label        lblNoResult;
        private System.Windows.Forms.Panel        panelCard;
        private System.Windows.Forms.Label        lblEquipmentCode;
        private System.Windows.Forms.Label        lblEquipmentCodeValue;
        private System.Windows.Forms.Label        lblEquipmentName;
        private System.Windows.Forms.Label        lblEquipmentNameValue;
        private System.Windows.Forms.Label        lblMake;
        private System.Windows.Forms.Label        lblMakeValue;
        private System.Windows.Forms.Label        lblModel;
        private System.Windows.Forms.Label        lblModelValue;
        private System.Windows.Forms.Label        lblSerial;
        private System.Windows.Forms.Label        lblSerialValue;
        private System.Windows.Forms.Label        lblStatus;
        private System.Windows.Forms.Label        lblStatusValue;
        private System.Windows.Forms.Label        lblLocation;
        private System.Windows.Forms.Label        lblLocationValue;
        private System.Windows.Forms.Label        lblEmployee;
        private System.Windows.Forms.Label        lblEmployeeValue;
        private System.Windows.Forms.Label        lblPurchaseDate;
        private System.Windows.Forms.Label        lblPurchaseDateValue;
        private System.Windows.Forms.Panel        panelHistory;
        private System.Windows.Forms.Label        lblHistoryTitle;
        private System.Windows.Forms.DataGridView dgvHistory;
    }
}