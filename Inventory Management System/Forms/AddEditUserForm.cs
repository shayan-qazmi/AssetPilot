// Forms/AddEditUserForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Forms
{
    public class AddEditUserForm : Form
    {
        private readonly User _user;
        private readonly bool _isEditMode;
        private List<Role> _roles = new();

        private TextBox txtUsername = null!;
        private TextBox txtFullName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPassword = null!;
        private TextBox txtConfirm = null!;
        private ComboBox cmbRole = null!;
        private CheckBox chkActive = null!;
        private Label lblStatus = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        // ── Profile picture (optional) ──────────────────────────────────
        private Panel pnlAvatar = null!;
        private Button btnUploadPicture = null!;
        private Button btnRemovePicture = null!;
        private Image? _avatarImage;
        private string? _pendingPictureFile; // local file chosen via dialog, not yet copied to the share
        private bool _removePicture;      // true if user cleared an existing picture

        public AddEditUserForm(User? user = null)
        {
            _user = user ?? new User { IsActive = true };
            _isEditMode = user != null;
            InitializeUI();
            LoadRoles();
            if (_isEditMode) PopulateFields();

            FormClosed += (s, e) => _avatarImage?.Dispose();
        }

        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        private void InitializeUI()
        {
            Text = _isEditMode ? "Edit User" : "Add User";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScroll = false;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 460;
            const int margin = 28;
            int contentWidth = formWidth - margin * 2;

            // ── Header Banner ──
            Color modeAccent = _isEditMode ? Color.FromArgb(245, 158, 11) : ThemeHelper.Header;
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = modeAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit User" : "Add User",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = _isEditMode ? "Update user account credentials and role permissions" : "Create a new user account and assign system permissions";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(235, 245, 233),
                AutoSize = false,
                Size = new Size(contentWidth, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 6)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 20;

            // ── Body Panel ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10), AutoScroll = false };
            Controls.Add(body);

            // ── Footer Panel ──
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(120, 40) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "Save", Size = new Size(130, 40) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnSave.Location = new Point(formWidth - btnSave.Width - margin, 16);
            btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 12, 16);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            var lblRequiredNote = new Label
            {
                Text = "* Required field",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = MutedText,
                AutoSize = true
            };
            lblRequiredNote.Location = new Point(margin, (footerHeight - lblRequiredNote.PreferredHeight) / 2);
            pnlFooter.Controls.Add(lblRequiredNote);

            int y = 20;

            void SectionHeader(string titleText)
            {
                body.Controls.Add(new Panel { BackColor = modeAccent, Size = new Size(4, 22), Location = new Point(margin, y) });
                body.Controls.Add(new Label
                {
                    Text = titleText,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(margin + 12, y - 1)
                });
                y += 34;
            }

            void FieldLabel(string label, bool required = false)
            {
                body.Controls.Add(new Label
                {
                    Text = required ? $"{label}  *" : label,
                    Location = new Point(margin, y),
                    AutoSize = true,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary
                });
                y += 20;
            }

            void PlaceControl(Control ctrl, int height = 30)
            {
                ctrl.Location = new Point(margin, y);
                ctrl.Width = contentWidth;
                if (ctrl.Height < height) ctrl.Height = height;
                body.Controls.Add(ctrl);
                y += ctrl.Height + 4;
            }

            void Hint(string text)
            {
                var font = new Font("Segoe UI", 7.8f, FontStyle.Italic);
                var sz = TextRenderer.MeasureText(text, font, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
                body.Controls.Add(new Label
                {
                    Text = text,
                    Font = font,
                    ForeColor = MutedText,
                    AutoSize = false,
                    Size = new Size(contentWidth, sz.Height + 4),
                    Location = new Point(margin, y)
                });
                y += sz.Height + 4 + 8;
            }

            SectionHeader("User Account Details");

            txtUsername = new TextBox();
            ThemeHelper.StyleTextBox(txtUsername);
            FieldLabel("Username", required: true);
            PlaceControl(txtUsername);
            if (_isEditMode) txtUsername.Enabled = false;
            Hint("System login username — must be unique");

            txtFullName = new TextBox();
            ThemeHelper.StyleTextBox(txtFullName);
            FieldLabel("Full Name", required: true);
            PlaceControl(txtFullName);
            Hint("Full display name of the user");

            txtEmail = new TextBox();
            ThemeHelper.StyleTextBox(txtEmail);
            FieldLabel("Email");
            PlaceControl(txtEmail);
            Hint("Contact email for system notifications and password recovery");

            txtPassword = new TextBox { UseSystemPasswordChar = true };
            ThemeHelper.StyleTextBox(txtPassword);
            FieldLabel(_isEditMode ? "New Password" : "Password", required: !_isEditMode);
            PlaceControl(txtPassword);
            if (_isEditMode)
                Hint("Leave blank to keep existing password");

            if (!_isEditMode)
            {
                txtConfirm = new TextBox { UseSystemPasswordChar = true };
                ThemeHelper.StyleTextBox(txtConfirm);
                FieldLabel("Confirm Password", required: true);
                PlaceControl(txtConfirm);
                Hint("Re-enter the password to confirm");
            }

            cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            ThemeHelper.StyleComboBox(cmbRole);
            FieldLabel("Role", required: true);
            PlaceControl(cmbRole);
            Hint("Assigned role determines accessible screens and system capabilities");

            // ── Profile Picture (optional) ──
            const int avatarSize = 72;
            FieldLabel("Profile Picture");

            pnlAvatar = new Panel
            {
                Location = new Point(margin, y),
                Size = new Size(avatarSize, avatarSize),
                BackColor = Color.Transparent
            };
            pnlAvatar.Paint += (s, e) =>
                AvatarHelper.DrawAvatar(e.Graphics, new Rectangle(0, 0, avatarSize - 1, avatarSize - 1), _avatarImage);
            body.Controls.Add(pnlAvatar);

            btnUploadPicture = new Button
            {
                Text = "Upload...",
                Size = new Size(120, 28),
                Location = new Point(margin + avatarSize + 14, y + 6)
            };
            ThemeHelper.StyleSecondaryButton(btnUploadPicture);
            btnUploadPicture.Click += BtnUploadPicture_Click;
            body.Controls.Add(btnUploadPicture);

            btnRemovePicture = new Button
            {
                Text = "Remove",
                Size = new Size(120, 28),
                Location = new Point(margin + avatarSize + 14, y + 40)
            };
            ThemeHelper.StyleSecondaryButton(btnRemovePicture);
            btnRemovePicture.Click += BtnRemovePicture_Click;
            body.Controls.Add(btnRemovePicture);

            y += avatarSize + 8;
            Hint("Optional — shown on the user's detail view. Leave blank for a default profile icon.");

            chkActive = new CheckBox
            {
                Text = "Is Active Account",
                Location = new Point(margin, y),
                AutoSize = true,
                Checked = _user.IsActive == true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            body.Controls.Add(chkActive);
            y += 30;

            lblStatus = new Label
            {
                Location = new Point(margin, y),
                AutoSize = false,
                Size = new Size(contentWidth, 24),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent
            };
            body.Controls.Add(lblStatus);
            y += 28;

            body.Size = new Size(formWidth, y);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadRoles()
        {
            try
            {
                using var db = new InventoryDbContext();
                _roles = db.Roles.ToList();
                cmbRole.Items.Clear();
                foreach (var r in _roles)
                    cmbRole.Items.Add(r.RoleName);
                if (cmbRole.Items.Count > 0)
                    cmbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AddEditUserForm.LoadRoles");
            }
        }

        private void PopulateFields()
        {
            txtUsername.Text = _user.Username;
            txtFullName.Text = _user.FullName;
            txtEmail.Text = _user.Email ?? "";
            chkActive.Checked = _user.IsActive == true;

            if (!string.IsNullOrWhiteSpace(_user.PicturePath))
            {
                _avatarImage = AvatarHelper.TryLoadUserImage(_user.PicturePath);
                pnlAvatar.Invalidate();
            }

            try
            {
                using var db = new InventoryDbContext();
                var ur = db.UserRoles.FirstOrDefault(x => x.UserId == _user.Id);
                if (ur != null)
                {
                    var role = _roles.FirstOrDefault(r => r.Id == ur.RoleId);
                    if (role != null) cmbRole.SelectedItem = role.RoleName;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AddEditUserForm.PopulateFields (UserRole query)");
            }
        }

        // ── Profile picture handlers ────────────────────────────────────
        private void BtnUploadPicture_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Profile Picture",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var newImage = Image.FromFile(ofd.FileName);
                _avatarImage?.Dispose();
                _avatarImage = newImage;
                _pendingPictureFile = ofd.FileName;
                _removePicture = false;
                pnlAvatar.Invalidate();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not load image: " + ex.Message;
            }
        }

        private void BtnRemovePicture_Click(object? sender, EventArgs e)
        {
            _avatarImage?.Dispose();
            _avatarImage = null;
            _pendingPictureFile = null;
            _removePicture = true;
            pnlAvatar.Invalidate();
        }

        private static string SaveUserPictureToShare(string sourceFile, string username)
        {
            NetworkShareHelper.EnsureConnected();
            Directory.CreateDirectory(NetworkPaths.UserPicturesDir);

            string ext = Path.GetExtension(sourceFile);
            string fileName = $"{username}_{Guid.NewGuid():N}{ext}";
            string destPath = Path.Combine(NetworkPaths.UserPicturesDir, fileName);

            File.Copy(sourceFile, destPath, true);
            return fileName;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                lblStatus.Text = "Username and Full Name are required.";
                return;
            }

            if (!_isEditMode && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblStatus.Text = "Password is required for new users.";
                return;
            }

            if (!_isEditMode && txtPassword.Text != txtConfirm?.Text)
            {
                lblStatus.Text = "Passwords do not match.";
                return;
            }

            if (cmbRole.SelectedIndex < 0)
            {
                lblStatus.Text = "Please select a role.";
                return;
            }

            try
            {
                using var db = new InventoryDbContext();

                User entity;
                if (_isEditMode)
                {
                    entity = db.Users.FirstOrDefault(u => u.Id == _user.Id)
                             ?? throw new Exception("User not found.");
                }
                else
                {
                    if (db.Users.Any(u => u.Username == txtUsername.Text.Trim()))
                    {
                        lblStatus.Text = "Username already exists.";
                        return;
                    }
                    entity = new User();
                    db.Users.Add(entity);
                }

                string newEmail = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newEmail) &&
                    db.Users.Any(u => u.Email == newEmail && u.Id != _user.Id))
                {
                    lblStatus.Text = "Email is already in use by another user.";
                    return;
                }

                entity.Username = txtUsername.Text.Trim();
                entity.FullName = txtFullName.Text.Trim();
                entity.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                entity.IsActive = chkActive.Checked;

                if (!string.IsNullOrWhiteSpace(txtPassword.Text))
                    entity.PasswordHash = txtPassword.Text;

                // ── Profile picture ──────────────────────────────────────
                if (_removePicture)
                {
                    entity.PicturePath = null;
                }
                else if (_pendingPictureFile != null)
                {
                    try
                    {
                        entity.PicturePath = SaveUserPictureToShare(_pendingPictureFile, entity.Username);
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "Could not save picture: " + ex.Message;
                        return;
                    }
                }

                db.SaveChanges();

                // ── Update role ──────────────────────────────────────────
                // UserRole has a composite primary key of (UserId, RoleId).
                // EF Core does not allow modifying a property that is part
                // of an entity's primary key on a tracked row — attempting
                // `existingUR.RoleId = selectedRole.Id` throws:
                // "The property 'UserRole.RoleId' is part of a key and so
                // cannot be modified or marked as modified."
                // Fix: remove the old assignment row and insert a new one
                // instead of mutating RoleId in place.
                var selectedRole = _roles[cmbRole.SelectedIndex];
                var existingUR = db.UserRoles.FirstOrDefault(ur => ur.UserId == entity.Id);

                if (existingUR == null)
                {
                    db.UserRoles.Add(new UserRole { UserId = entity.Id, RoleId = selectedRole.Id });
                    db.SaveChanges();
                }
                else if (existingUR.RoleId != selectedRole.Id)
                {
                    db.UserRoles.Remove(existingUR);
                    db.SaveChanges();

                    db.UserRoles.Add(new UserRole { UserId = entity.Id, RoleId = selectedRole.Id });
                    db.SaveChanges();
                }
                // else: role unchanged — nothing to do

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                string detail = ex.InnerException?.Message ?? ex.Message;
                lblStatus.Text = "Error: " + detail;
            }
        }
    }
}