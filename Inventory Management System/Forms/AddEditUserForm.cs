// Forms/AddEditUserForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Forms
{
    public class AddEditUserForm : Form
    {
        private readonly User    _user;
        private readonly bool    _isEditMode;
        private List<Role>       _roles = new();

        private TextBox  txtUsername  = null!;
        private TextBox  txtFullName  = null!;
        private TextBox  txtEmail     = null!;
        private TextBox  txtPassword  = null!;
        private TextBox  txtConfirm   = null!;
        private ComboBox cmbRole      = null!;
        private CheckBox chkActive    = null!;
        private Label    lblStatus    = null!;
        private Button   btnSave      = null!;
        private Button   btnCancel    = null!;

        public AddEditUserForm(User? user = null)
        {
            _user       = user ?? new User { IsActive = true };
            _isEditMode = user != null;
            InitializeUI();
            LoadRoles();
            if (_isEditMode) PopulateFields();
        }

        private void InitializeUI()
        {
            Text            = _isEditMode ? "Edit User" : "Add User";
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            AutoScroll      = true;
            BackColor       = Color.White;
            Font            = ThemeHelper.FontDefault;

            int x = 24, w = 340, y = 24;

            AddLbl("Username",  x, ref y); txtUsername = AddTxt(x, w, ref y);
            if (_isEditMode) txtUsername.Enabled = false;

            AddLbl("Full Name", x, ref y); txtFullName = AddTxt(x, w, ref y);
            AddLbl("Email",     x, ref y); txtEmail    = AddTxt(x, w, ref y);

            AddLbl(_isEditMode ? "New Password (leave blank to keep)" : "Password", x, ref y);
            txtPassword = AddTxt(x, w, ref y);
            txtPassword.UseSystemPasswordChar = true;

            if (!_isEditMode)
            {
                AddLbl("Confirm Password", x, ref y);
                txtConfirm = AddTxt(x, w, ref y);
                txtConfirm.UseSystemPasswordChar = true;
            }

            AddLbl("Role", x, ref y);
            cmbRole = new ComboBox
            {
                Location      = new Point(x, y),
                Width         = w,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ThemeHelper.StyleComboBox(cmbRole);
            Controls.Add(cmbRole);
            y += 34;

            chkActive = new CheckBox
            {
                Text      = "Active",
                Location  = new Point(x, y),
                AutoSize  = true,
                Checked   = _user.IsActive == true,
                Font      = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            Controls.Add(chkActive);
            y += 34;

            lblStatus = new Label
            {
                Location  = new Point(x, y),
                AutoSize  = false,
                Size      = new Size(w, 22),
                Font      = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                BackColor = Color.Transparent
            };
            Controls.Add(lblStatus);
            y += 28;

            btnSave = new Button
            {
                Text      = "Save",
                Location  = new Point(x + w - 160, y),
                Size      = new Size(75, 32),
                BackColor = ThemeHelper.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = ThemeHelper.FontBold,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(x + w - 76, y),
                Size      = new Size(76, 32),
                FlatStyle = FlatStyle.Flat,
                Font      = ThemeHelper.FontDefault,
                Cursor    = Cursors.Hand
            };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            ClientSize   = new Size(w + x * 2, y + 32 + 20);
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
            txtEmail.Text    = _user.Email ?? "";
            chkActive.Checked = _user.IsActive == true;

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
                entity.Email    = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                entity.IsActive = chkActive.Checked;

                if (!string.IsNullOrWhiteSpace(txtPassword.Text))
                    entity.PasswordHash = txtPassword.Text;

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
                var existingUR   = db.UserRoles.FirstOrDefault(ur => ur.UserId == entity.Id);

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

        private void AddLbl(string text, int x, ref int y)
        {
            var lbl = new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);
            y += 22;
        }

        private TextBox AddTxt(int x, int w, ref int y)
        {
            var txt = new TextBox { Location = new Point(x, y), Size = new Size(w, 28) };
            ThemeHelper.StyleTextBox(txt);
            Controls.Add(txt);
            y += 38;
            return txt;
        }
    }
}