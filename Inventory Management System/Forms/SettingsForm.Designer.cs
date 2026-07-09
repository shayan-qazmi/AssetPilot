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
            SuspendLayout();
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 750);
            Name                = "SettingsForm";
            Text                = "Settings";
            ResumeLayout(false);
        }
        #endregion

        // All controls are created in code via BuildUI() in SettingsForm.cs
        // No controls declared here to avoid double-instantiation and overlap
        private System.Windows.Forms.TextBox  txtCompany     = null!;
        private System.Windows.Forms.TextBox  txtPrinterName = null!;
        private System.Windows.Forms.TextBox  txtLabelWidth  = null!;
        private System.Windows.Forms.TextBox  txtLabelHeight = null!;
        private System.Windows.Forms.Button   btnSave        = null!;
        private System.Windows.Forms.Button   btnReset       = null!;
        private System.Windows.Forms.Button   btnTestPrint   = null!;
    }
}