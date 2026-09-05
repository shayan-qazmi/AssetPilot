// Forms/LoginForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            _animTimer?.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelLeft           = new System.Windows.Forms.Panel();
            panelAnim           = new System.Windows.Forms.Panel();
            picLogo             = new System.Windows.Forms.PictureBox();
            lblBrand            = new System.Windows.Forms.Label();
            lblTagline          = new System.Windows.Forms.Label();
            panelRight          = new System.Windows.Forms.Panel();
            panelCard           = new System.Windows.Forms.Panel();
            lblAppTitle         = new System.Windows.Forms.Label();
            lblSubTitle         = new System.Windows.Forms.Label();
            lblUsername         = new System.Windows.Forms.Label();
            panelUsername       = new Inventory_Management_System.Helpers.RoundedPanel();
            lblUserIcon         = new System.Windows.Forms.Label();
            txtUsername         = new System.Windows.Forms.TextBox();
            lblPassword         = new System.Windows.Forms.Label();
            panelPassword       = new Inventory_Management_System.Helpers.RoundedPanel();
            lblPassIcon         = new System.Windows.Forms.Label();
            txtPassword         = new System.Windows.Forms.TextBox();
            lblShowHidePassword = new System.Windows.Forms.Label();
            lblToggleLabel      = new System.Windows.Forms.Label();
            pnlToggle           = new System.Windows.Forms.Panel();
            btnLogin            = new System.Windows.Forms.Button();
            btnCancel           = new System.Windows.Forms.Button();
            lblError            = new System.Windows.Forms.Label();
            panelBottom         = new System.Windows.Forms.Panel();
            lblRowCount         = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            panelLeft.SuspendLayout();
            panelAnim.SuspendLayout();
            panelRight.SuspendLayout();
            panelCard.SuspendLayout();
            panelUsername.SuspendLayout();
            panelPassword.SuspendLayout();
            SuspendLayout();

            // ── panelLeft ─────────────────────────────────────────────────
            panelLeft.Dock      = System.Windows.Forms.DockStyle.Left;
            panelLeft.Width     = 420;
            panelLeft.BackColor = System.Drawing.Color.FromArgb(27, 94, 32);
            panelLeft.Name      = "panelLeft";
            panelLeft.Controls.Add(panelAnim);

            // ── panelAnim ─────────────────────────────────────────────────
            panelAnim.Dock      = System.Windows.Forms.DockStyle.Fill;
            panelAnim.BackColor = System.Drawing.Color.FromArgb(27, 94, 32);
            panelAnim.Name      = "panelAnim";
            panelAnim.Controls.Add(lblTagline);
            panelAnim.Controls.Add(lblBrand);
            panelAnim.Controls.Add(picLogo);

            // ── picLogo ───────────────────────────────────────────────────
            picLogo.Size      = new System.Drawing.Size(140, 140);
            picLogo.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            picLogo.BackColor = System.Drawing.Color.Transparent;
            picLogo.Location  = new System.Drawing.Point(130, 10);
            picLogo.Name      = "picLogo";

            // ── lblBrand ──────────────────────────────────────────────────
            lblBrand.Text      = "AssetPilot";
            lblBrand.AutoSize  = false;
            lblBrand.Size      = new System.Drawing.Size(400, 40);
            lblBrand.Location  = new System.Drawing.Point(10, 138);
            lblBrand.Font      = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            lblBrand.ForeColor = System.Drawing.Color.White;
            lblBrand.BackColor = System.Drawing.Color.Transparent;
            lblBrand.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblBrand.Name      = "lblBrand";

            // ── lblTagline ────────────────────────────────────────────────
            lblTagline.Text      = "Smart Inventory, Simplified";
            lblTagline.AutoSize  = false;
            lblTagline.Size      = new System.Drawing.Size(400, 24);
            lblTagline.Location  = new System.Drawing.Point(10, 182);
            lblTagline.Font      = new System.Drawing.Font("Segoe UI", 10F);
            lblTagline.ForeColor = System.Drawing.Color.FromArgb(200, 255, 255, 255);
            lblTagline.BackColor = System.Drawing.Color.Transparent;
            lblTagline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblTagline.Name      = "lblTagline";

            // ── panelRight ────────────────────────────────────────────────
            panelRight.Dock      = System.Windows.Forms.DockStyle.Fill;
            panelRight.BackColor = System.Drawing.Color.White;
            panelRight.Name      = "panelRight";
            panelRight.Controls.Add(panelCard);

            // ── panelCard ─────────────────────────────────────────────────
            // Reduced height since Cancel is removed and replaced by circle button
            panelCard.Size      = new System.Drawing.Size(360, 420);
            panelCard.BackColor = System.Drawing.Color.White;
            panelCard.Anchor    = System.Windows.Forms.AnchorStyles.None;
            panelCard.Name      = "panelCard";
            panelCard.Controls.Add(lblAppTitle);
            panelCard.Controls.Add(lblSubTitle);
            panelCard.Controls.Add(lblUsername);
            panelCard.Controls.Add(panelUsername);
            panelCard.Controls.Add(lblPassword);
            panelCard.Controls.Add(panelPassword);
            panelCard.Controls.Add(lblToggleLabel);
            panelCard.Controls.Add(pnlToggle);
            panelCard.Controls.Add(btnLogin);
            panelCard.Controls.Add(btnCancel); // kept in field but hidden
            panelCard.Controls.Add(lblError);  // kept in field but hidden

            // ── lblAppTitle ───────────────────────────────────────────────
            lblAppTitle.Text      = "Sign In";
            lblAppTitle.AutoSize  = true;
            lblAppTitle.Location  = new System.Drawing.Point(0, 0);
            lblAppTitle.Font      = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            lblAppTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblAppTitle.Name      = "lblAppTitle";

            // ── lblSubTitle ───────────────────────────────────────────────
            lblSubTitle.Text      = "Enter your credentials to continue";
            lblSubTitle.AutoSize  = true;
            lblSubTitle.Location  = new System.Drawing.Point(0, 50);
            lblSubTitle.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            lblSubTitle.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            lblSubTitle.Name      = "lblSubTitle";

            // ── lblUsername ───────────────────────────────────────────────
            lblUsername.Text      = "Username";
            lblUsername.AutoSize  = true;
            lblUsername.Location  = new System.Drawing.Point(0, 104);
            lblUsername.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            lblUsername.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblUsername.Name      = "lblUsername";

            // ── panelUsername ─────────────────────────────────────────────
            panelUsername.Location     = new System.Drawing.Point(0, 128);
            panelUsername.Size         = new System.Drawing.Size(360, 44);
            panelUsername.BackColor    = System.Drawing.Color.FromArgb(238, 241, 245);
            panelUsername.BorderColor  = System.Drawing.Color.FromArgb(209, 213, 219);
            panelUsername.BorderWidth  = 1;
            panelUsername.CornerRadius = 10;
            panelUsername.Name         = "panelUsername";
            panelUsername.Controls.Add(lblUserIcon);
            panelUsername.Controls.Add(txtUsername);
            panelUsername.TabIndex = 0;
            panelUsername.TabStop = true;

            // ── lblUserIcon ───────────────────────────────────────────────
            lblUserIcon.Text      = "👤";
            lblUserIcon.Font      = new System.Drawing.Font("Segoe UI", 11.5F);
            lblUserIcon.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            lblUserIcon.Location  = new System.Drawing.Point(12, 11);
            lblUserIcon.Size      = new System.Drawing.Size(24, 22);
            lblUserIcon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblUserIcon.BackColor = System.Drawing.Color.Transparent;
            lblUserIcon.Name      = "lblUserIcon";

            // ── txtUsername ───────────────────────────────────────────────
            txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtUsername.BackColor   = System.Drawing.Color.FromArgb(238, 241, 245);
            txtUsername.ForeColor   = System.Drawing.Color.FromArgb(17, 24, 39);
            txtUsername.Font        = new System.Drawing.Font("Segoe UI", 10.5F);
            txtUsername.Location    = new System.Drawing.Point(44, 12);
            txtUsername.Size        = new System.Drawing.Size(304, 20);
            txtUsername.Name        = "txtUsername";
            txtUsername.TabIndex    = 0;

            // ── lblPassword ───────────────────────────────────────────────
            lblPassword.Text      = "Password";
            lblPassword.AutoSize  = true;
            lblPassword.Location  = new System.Drawing.Point(0, 184);
            lblPassword.Font      = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            lblPassword.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblPassword.Name      = "lblPassword";
            txtPassword.TabIndex = 1;

            // ── panelPassword ─────────────────────────────────────────────
            panelPassword.Location     = new System.Drawing.Point(0, 208);
            panelPassword.Size         = new System.Drawing.Size(360, 44);
            panelPassword.BackColor    = System.Drawing.Color.FromArgb(238, 241, 245);
            panelPassword.BorderColor  = System.Drawing.Color.FromArgb(209, 213, 219);
            panelPassword.BorderWidth  = 1;
            panelPassword.CornerRadius = 10;
            panelPassword.Name         = "panelPassword";
            panelPassword.Controls.Add(lblPassIcon);
            panelPassword.Controls.Add(txtPassword);
            panelPassword.Controls.Add(lblShowHidePassword);
            panelPassword.TabIndex = 1;
            panelPassword.TabStop = true;

            // ── lblPassIcon ───────────────────────────────────────────────
            lblPassIcon.Text      = "🔑";
            lblPassIcon.Font      = new System.Drawing.Font("Segoe UI", 11.5F);
            lblPassIcon.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            lblPassIcon.Location  = new System.Drawing.Point(12, 11);
            lblPassIcon.Size      = new System.Drawing.Size(24, 22);
            lblPassIcon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblPassIcon.BackColor = System.Drawing.Color.Transparent;
            lblPassIcon.Name      = "lblPassIcon";

            // ── txtPassword ───────────────────────────────────────────────
            txtPassword.BorderStyle           = System.Windows.Forms.BorderStyle.None;
            txtPassword.BackColor             = System.Drawing.Color.FromArgb(238, 241, 245);
            txtPassword.ForeColor             = System.Drawing.Color.FromArgb(17, 24, 39);
            txtPassword.Font                  = new System.Drawing.Font("Segoe UI", 10.5F);
            txtPassword.Location              = new System.Drawing.Point(44, 12);
            txtPassword.Size                  = new System.Drawing.Size(278, 20);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Name                  = "txtPassword";
            txtPassword.TabIndex              = 1;

            // ── lblShowHidePassword ───────────────────────────────────────
            lblShowHidePassword.Text      = "👁";
            lblShowHidePassword.Font      = new System.Drawing.Font("Segoe UI", 11.5F);
            lblShowHidePassword.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            lblShowHidePassword.Location  = new System.Drawing.Point(328, 11);
            lblShowHidePassword.Size      = new System.Drawing.Size(24, 22);
            lblShowHidePassword.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblShowHidePassword.BackColor = System.Drawing.Color.Transparent;
            lblShowHidePassword.Cursor    = System.Windows.Forms.Cursors.Hand;
            lblShowHidePassword.Name      = "lblShowHidePassword";
            lblShowHidePassword.Click    += lblShowHidePassword_Click;

            // ── Toggle row ────────────────────────────────────────────────
            lblToggleLabel.Text      = "Remember username";
            lblToggleLabel.AutoSize = true;
            lblToggleLabel.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            lblToggleLabel.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            lblToggleLabel.BackColor = System.Drawing.Color.White;
            lblToggleLabel.Name      = "lblToggleLabel";
            lblToggleLabel.Cursor    = System.Windows.Forms.Cursors.Hand;
            lblToggleLabel.Click    += lblToggleLabel_Click;

            // Toggle switch panel (drawn in code)
            pnlToggle.Location  = new System.Drawing.Point(0, 260);
            pnlToggle.Size      = new System.Drawing.Size(64, 36);
            pnlToggle.BackColor = System.Drawing.Color.Transparent;
            pnlToggle.Name      = "pnlToggle";
            pnlToggle.Cursor    = System.Windows.Forms.Cursors.Hand;
            pnlToggle.Paint    += pnlToggle_Paint;
            pnlToggle.Click    += pnlToggle_Click;
            pnlToggle.TabIndex = 2;

            // ── btnLogin — circle button (replaces Sign In + Cancel) ──────
            // Rendered as a circle with an arrow glyph via owner-draw.
            int circleD = 64;
            btnLogin.Text      = "";
            btnLogin.Location  = new System.Drawing.Point((360 - circleD) / 2, 316);
            btnLogin.Size      = new System.Drawing.Size(circleD, circleD);
            btnLogin.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            btnLogin.BackColor = System.Drawing.Color.FromArgb(27, 94, 32);
            btnLogin.ForeColor = System.Drawing.Color.White;
            btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize         = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(56, 142, 60);
            btnLogin.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(27, 94, 32);
            btnLogin.Cursor    = System.Windows.Forms.Cursors.Hand;
            btnLogin.Name      = "btnLogin";
            btnLogin.TabIndex  = 3;
            btnLogin.Click    += btnLogin_Click;
            // Make it a pill/circle
          

            // ── btnCancel — hidden (kept for compat) ──────────────────────
            btnCancel.Text      = "Cancel";
            btnCancel.Location  = new System.Drawing.Point(-1000, -1000); // off-screen
            btnCancel.Size      = new System.Drawing.Size(1, 1);
            btnCancel.Visible   = false;
            btnCancel.Name      = "btnCancel";
            btnCancel.TabIndex  = 4;
            btnCancel.Click    += btnCancel_Click;

            // ── lblError — hidden (replaced by toast) ─────────────────────
            lblError.AutoSize  = false;
            lblError.Size      = new System.Drawing.Size(1, 1);
            lblError.Location  = new System.Drawing.Point(-1000, -1000);
            lblError.Visible   = false;
            lblError.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            lblError.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
            lblError.Name      = "lblError";
            lblError.Text      = "";

            // ── LoginForm ─────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1180, 760);
            MinimumSize = new System.Drawing.Size(980, 640);
            Controls.Add(panelRight);
            Controls.Add(panelLeft);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            MaximizeBox = true;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            Name            = "LoginForm";
            Text            = "AssetPilot — Login";
            StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            panelLeft.ResumeLayout(false);
            panelAnim.ResumeLayout(false);
            panelRight.ResumeLayout(false);
            panelCard.ResumeLayout(false);
            panelCard.PerformLayout();
            panelUsername.ResumeLayout(false);
            panelUsername.PerformLayout();
            panelPassword.ResumeLayout(false);
            panelPassword.PerformLayout();
            ResumeLayout(false);

            void CenterCard()
            {
                if (panelRight.Width <= 0 || panelRight.Height <= 0) return;
                panelCard.Location = new System.Drawing.Point(
                    Math.Max(0, (panelRight.Width - panelCard.Width) / 2),
                    Math.Max(0, (panelRight.Height - panelCard.Height) / 2));

                int labelPrefH = lblToggleLabel.PreferredHeight;
                int toggleCenterY = pnlToggle.Top + 18;
                int labelTop = toggleCenterY - (labelPrefH / 2);
                lblToggleLabel.Location = new System.Drawing.Point(pnlToggle.Right + 10, labelTop);
            }

            panelRight.Resize += (s, e) => CenterCard();
            panelCard.Layout  += (s, e) => CenterCard();
            Load              += (s, e) => CenterCard();
            Shown += (s, e) =>
            {
                CenterCard();
                try
                {
                    picLogo.Image = global::Inventory_Management_System.Properties.Resources.logo;
                }
                catch { }

                // ── Slide-up entrance animation for the right panel card ──
                StartSlideUpAnimation();
            };

            // Draw arrow on circle button
            btnLogin.Paint += BtnLogin_PaintArrow;
        }

        #endregion

        private System.Windows.Forms.Panel      panelLeft;
        private System.Windows.Forms.Panel      panelAnim;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label      lblBrand;
        private System.Windows.Forms.Label      lblTagline;
        private System.Windows.Forms.Panel      panelRight;
        private System.Windows.Forms.Panel      panelCard;
        private System.Windows.Forms.Label      lblAppTitle;
        private System.Windows.Forms.Label      lblSubTitle;
        private System.Windows.Forms.Label      lblUsername;
        private Inventory_Management_System.Helpers.RoundedPanel panelUsername;
        private System.Windows.Forms.Label      lblUserIcon;
        private System.Windows.Forms.TextBox    txtUsername;
        private System.Windows.Forms.Label      lblPassword;
        private Inventory_Management_System.Helpers.RoundedPanel panelPassword;
        private System.Windows.Forms.Label      lblPassIcon;
        private System.Windows.Forms.TextBox    txtPassword;
        private System.Windows.Forms.Label      lblShowHidePassword;
        private System.Windows.Forms.Label      lblToggleLabel;
        private System.Windows.Forms.Panel      pnlToggle;
        private System.Windows.Forms.Button     btnLogin;
        private System.Windows.Forms.Button     btnCancel;
        private System.Windows.Forms.Label      lblError;
        private System.Windows.Forms.Panel      panelBottom;
        private System.Windows.Forms.Label      lblRowCount;
    }
}
