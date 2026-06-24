// Forms/SettingsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class SettingsForm
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
            lblSectionTitle  = new System.Windows.Forms.Label();
            lblCompany       = new System.Windows.Forms.Label();
            txtCompany       = new System.Windows.Forms.TextBox();
            lblTheme         = new System.Windows.Forms.Label();
            cmbTheme         = new System.Windows.Forms.ComboBox();
            lblServer        = new System.Windows.Forms.Label();
            txtServer        = new System.Windows.Forms.TextBox();
            lblDatabase      = new System.Windows.Forms.Label();
            txtDatabase      = new System.Windows.Forms.TextBox();
            btnSave          = new System.Windows.Forms.Button();
            btnReset         = new System.Windows.Forms.Button();
            lblBarcodeSection = new System.Windows.Forms.Label();
            lblPrinterName   = new System.Windows.Forms.Label();
            txtPrinterName   = new System.Windows.Forms.TextBox();
            lblLabelWidth    = new System.Windows.Forms.Label();
            txtLabelWidth    = new System.Windows.Forms.TextBox();
            lblLabelHeight   = new System.Windows.Forms.Label();
            txtLabelHeight   = new System.Windows.Forms.TextBox();
            btnTestPrint     = new System.Windows.Forms.Button();

            SuspendLayout();

            // Section Title
            lblSectionTitle.Text     = "Application Settings";
            lblSectionTitle.Location = new System.Drawing.Point(60, 36);
            lblSectionTitle.AutoSize = true;
            lblSectionTitle.Name     = "lblSectionTitle";

            // lblCompany
            lblCompany.Text     = "Company Name";
            lblCompany.AutoSize = true;
            lblCompany.Location = new System.Drawing.Point(60, 90);
            lblCompany.Name     = "lblCompany";

            // txtCompany
            txtCompany.Location = new System.Drawing.Point(220, 88);
            txtCompany.Size     = new System.Drawing.Size(300, 26);
            txtCompany.Name     = "txtCompany";

            // lblTheme
            lblTheme.Text     = "Theme";
            lblTheme.AutoSize = true;
            lblTheme.Location = new System.Drawing.Point(60, 140);
            lblTheme.Name     = "lblTheme";

            // cmbTheme
            cmbTheme.Location      = new System.Drawing.Point(220, 138);
            cmbTheme.Size          = new System.Drawing.Size(300, 26);
            cmbTheme.Name          = "cmbTheme";
            cmbTheme.Items.AddRange(new object[] { "Light", "Dark" });
            cmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            // lblServer
            lblServer.Text     = "Server";
            lblServer.AutoSize = true;
            lblServer.Location = new System.Drawing.Point(60, 190);
            lblServer.Name     = "lblServer";

            // txtServer
            txtServer.Location = new System.Drawing.Point(220, 188);
            txtServer.Size     = new System.Drawing.Size(300, 26);
            txtServer.Name     = "txtServer";

            // lblDatabase
            lblDatabase.Text     = "Database";
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new System.Drawing.Point(60, 240);
            lblDatabase.Name     = "lblDatabase";

            // txtDatabase
            txtDatabase.Location = new System.Drawing.Point(220, 238);
            txtDatabase.Size     = new System.Drawing.Size(300, 26);
            txtDatabase.Name     = "txtDatabase";

            // btnSave
            btnSave.Location = new System.Drawing.Point(220, 300);
            btnSave.Size     = new System.Drawing.Size(120, 38);
            btnSave.Text     = "Save";
            btnSave.Name     = "btnSave";
            btnSave.Click   += btnSave_Click;
            // Barcode Section
            lblBarcodeSection = new System.Windows.Forms.Label();
            lblPrinterName = new System.Windows.Forms.Label();
            txtPrinterName = new System.Windows.Forms.TextBox();
            lblLabelWidth = new System.Windows.Forms.Label();
            txtLabelWidth = new System.Windows.Forms.TextBox();
            lblLabelHeight = new System.Windows.Forms.Label();
            txtLabelHeight = new System.Windows.Forms.TextBox();
            btnTestPrint = new System.Windows.Forms.Button();
            // btnReset
            btnReset.Location = new System.Drawing.Point(356, 300);
            btnReset.Size     = new System.Drawing.Size(120, 38);
            btnReset.Text     = "Reset";
            btnReset.Name     = "btnReset";
            btnReset.Click   += btnReset_Click;

            // ── Barcode Label Printing Section ─────────────────────────
            lblBarcodeSection.Text      = "Barcode Label Printing";
            lblBarcodeSection.Location  = new System.Drawing.Point(60, 370);
            lblBarcodeSection.AutoSize  = true;
            lblBarcodeSection.Name      = "lblBarcodeSection";

            lblPrinterName.Text     = "Printer Name";
            lblPrinterName.AutoSize = true;
            lblPrinterName.Location = new System.Drawing.Point(60, 416);
            lblPrinterName.Name     = "lblPrinterName";

            txtPrinterName.Location    = new System.Drawing.Point(220, 414);
            txtPrinterName.Size        = new System.Drawing.Size(300, 26);
            txtPrinterName.Name        = "txtPrinterName";
            txtPrinterName.PlaceholderText = "Leave blank to use default printer";

            lblLabelWidth.Text     = "Label Width (mm)";
            lblLabelWidth.AutoSize = true;
            lblLabelWidth.Location = new System.Drawing.Point(60, 460);
            lblLabelWidth.Name     = "lblLabelWidth";

            txtLabelWidth.Location = new System.Drawing.Point(220, 458);
            txtLabelWidth.Size     = new System.Drawing.Size(100, 26);
            txtLabelWidth.Name     = "txtLabelWidth";
            txtLabelWidth.Text     = "62";

            lblLabelHeight.Text     = "Label Height (mm)";
            lblLabelHeight.AutoSize = true;
            lblLabelHeight.Location = new System.Drawing.Point(340, 460);
            lblLabelHeight.Name     = "lblLabelHeight";

            txtLabelHeight.Location = new System.Drawing.Point(480, 458);
            txtLabelHeight.Size     = new System.Drawing.Size(100, 26);
            txtLabelHeight.Name     = "txtLabelHeight";
            txtLabelHeight.Text     = "29";

            btnTestPrint.Location = new System.Drawing.Point(220, 510);
            btnTestPrint.Size     = new System.Drawing.Size(160, 38);
            btnTestPrint.Text     = "🖨 Test Print Label";
            btnTestPrint.Name     = "btnTestPrint";
            btnTestPrint.Click   += btnTestPrint_Click;

            // SettingsForm
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);

            Controls.Add(lblSectionTitle);
            Controls.Add(lblCompany);
            Controls.Add(txtCompany);
            Controls.Add(lblTheme);
            Controls.Add(cmbTheme);
            Controls.Add(lblServer);
            Controls.Add(txtServer);
            Controls.Add(lblDatabase);
            Controls.Add(txtDatabase);
            Controls.Add(btnSave);
            Controls.Add(btnReset);
            Controls.Add(lblBarcodeSection);
            Controls.Add(lblPrinterName);
            Controls.Add(txtPrinterName);
            Controls.Add(lblLabelWidth);
            Controls.Add(txtLabelWidth);
            Controls.Add(lblLabelHeight);
            Controls.Add(txtLabelHeight);
            Controls.Add(btnTestPrint);

            Text = "Settings";
            Name = "SettingsForm";

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label    lblSectionTitle;
        private System.Windows.Forms.Label    lblCompany;
        private System.Windows.Forms.TextBox  txtCompany;
        private System.Windows.Forms.Label    lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
        private System.Windows.Forms.Label    lblServer;
        private System.Windows.Forms.TextBox  txtServer;
        private System.Windows.Forms.Label    lblDatabase;
        private System.Windows.Forms.TextBox  txtDatabase;
        private System.Windows.Forms.Button   btnSave;
        private System.Windows.Forms.Button   btnReset;
        private System.Windows.Forms.Label    lblBarcodeSection;
        private System.Windows.Forms.Label    lblPrinterName;
        private System.Windows.Forms.TextBox  txtPrinterName;
        private System.Windows.Forms.Label    lblLabelWidth;
        private System.Windows.Forms.TextBox  txtLabelWidth;
        private System.Windows.Forms.Label    lblLabelHeight;
        private System.Windows.Forms.TextBox  txtLabelHeight;
        private System.Windows.Forms.Button   btnTestPrint;
    }
}