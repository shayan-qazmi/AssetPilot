// Forms/AddEditEmployeeForm.cs
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class AddEditEmployeeForm : Form
    {
        private Employee _employee;
        private EmployeeService _employeeService;
        private bool _isEditMode;

        private TextBox txtFullName = null!;
        private TextBox txtDesignation = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        // ── Profile picture (optional) ──────────────────────────────────
        private Panel pnlAvatar = null!;
        private Button btnUploadPicture = null!;
        private Button btnRemovePicture = null!;
        private Image? _avatarImage;
        private string? _pendingPictureFile; // local file chosen via dialog, not yet copied to the share
        private bool _removePicture;         // true if user cleared an existing picture

        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        public AddEditEmployeeForm(Employee? employee = null)
        {
            _employee = employee ?? new Employee() { IsActive = true };
            _isEditMode = employee != null;
            _employeeService = new EmployeeService();

            InitializeUI();
            LoadData();

            FormClosed += (s, e) => _avatarImage?.Dispose();
        }

        private void InitializeUI()
        {
            Text = _isEditMode ? "Edit Employee" : "Add Employee";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScroll = false;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 460;
            const int margin = 24;
            int contentWidth = formWidth - margin * 2;

            // ── Header ──
            Color modeAccent = _isEditMode ? Color.FromArgb(245, 158, 11) : ThemeHelper.Header;
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = modeAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Employee" : "Add Employee",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = _isEditMode ? "Update the employee's details below" : "Enter the employee's details below";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(235, 245, 233),
                AutoSize = false,
                Size = new Size(contentWidth, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 8)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 20;

            // ── Body ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10), AutoScroll = false };
            Controls.Add(body);

            // ── Footer — buttons right-aligned, plus required-field legend on left ──
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

            int y = 24;

            void SectionHeader(string title)
            {
                body.Controls.Add(new Panel { BackColor = modeAccent, Size = new Size(4, 22), Location = new Point(margin, y) });
                body.Controls.Add(new Label
                {
                    Text = title,
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
                y += sz.Height + 4 + 10;
            }

            SectionHeader("Employee Details");

            txtFullName = new TextBox();
            ThemeHelper.StyleTextBox(txtFullName);
            FieldLabel("Full Name", required: true);
            PlaceControl(txtFullName);
            Hint("As it should appear on allocations and reports — must be unique");

            txtDesignation = new TextBox();
            ThemeHelper.StyleTextBox(txtDesignation);
            FieldLabel("Designation");
            PlaceControl(txtDesignation);
            y += 4;

            txtEmail = new TextBox();
            ThemeHelper.StyleTextBox(txtEmail);
            FieldLabel("Email");
            PlaceControl(txtEmail);
            y += 4;

            txtPhone = new TextBox();
            ThemeHelper.StyleTextBox(txtPhone);
            FieldLabel("Phone");
            PlaceControl(txtPhone);
            y += 8;

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
            Hint("Optional — shown on the employee's detail view. Leave blank for a default profile icon.");

            chkIsActive = new CheckBox
            {
                Text = "Is Active",
                Location = new Point(margin, y),
                AutoSize = true,
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary
            };
            body.Controls.Add(chkIsActive);
            y += 30;

            // ── Final sizing — no scrollbar, dialog fits exactly ──
            body.Size = new Size(formWidth, y);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                txtFullName.Text = _employee.FullName;
                txtDesignation.Text = _employee.Department;
                txtEmail.Text = _employee.Email;
                txtPhone.Text = _employee.Phone;

                if (!string.IsNullOrWhiteSpace(_employee.PicturePath))
                {
                    _avatarImage = AvatarHelper.TryLoadEmployeeImage(_employee.PicturePath);
                    pnlAvatar.Invalidate();
                }
            }
            chkIsActive.Checked = _employee.IsActive;
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
                ToastHelper.Show(this, "Could not load image: " + ex.Message, ToastType.Error);
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

        private static string SanitizeForFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(value.Where(c => !invalid.Contains(c)).ToArray()).Replace(' ', '_');
            return string.IsNullOrWhiteSpace(clean) ? "employee" : clean;
        }

        private static string SaveEmployeePictureToShare(string sourceFile, string displayName)
        {
            NetworkShareHelper.EnsureConnected();
            Directory.CreateDirectory(NetworkPaths.EmployeePicturesDir);

            string ext = Path.GetExtension(sourceFile);
            string fileName = $"{SanitizeForFileName(displayName)}_{Guid.NewGuid():N}{ext}";
            string destPath = Path.Combine(NetworkPaths.EmployeePicturesDir, fileName);

            File.Copy(sourceFile, destPath, true);
            return fileName;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ToastHelper.Show(this, "Full Name is required.", ToastType.Warning);
                return;
            }

            // ── Duplicate name check — case-insensitive, excludes this
            // record itself when editing so re-saving the same employee
            // doesn't falsely flag as a duplicate. ──
            var allResult = _employeeService.GetAll();
            if (allResult.Success && allResult.Data != null)
            {
                bool isDuplicate = allResult.Data.Any(emp =>
                    string.Equals(emp.FullName?.Trim(), name, StringComparison.OrdinalIgnoreCase) &&
                    (!_isEditMode || emp.Id != _employee.Id));

                if (isDuplicate)
                {
                    ToastHelper.Show(this, $"An employee named \"{name}\" already exists. Please use a different name.", ToastType.Warning);
                    return;
                }
            }

            // ── Profile picture ──────────────────────────────────────────
            if (_removePicture)
            {
                _employee.PicturePath = null;
            }
            else if (_pendingPictureFile != null)
            {
                try
                {
                    _employee.PicturePath = SaveEmployeePictureToShare(_pendingPictureFile, name);
                }
                catch (Exception ex)
                {
                    ToastHelper.Show(this, "Could not save picture: " + ex.Message, ToastType.Error);
                    return;
                }
            }

            _employee.FullName = name;
            _employee.Department = txtDesignation.Text.Trim();
            _employee.Email = txtEmail.Text.Trim();
            _employee.Phone = txtPhone.Text.Trim();
            _employee.IsActive = chkIsActive.Checked;

            var result = _isEditMode ? _employeeService.Update(_employee) : _employeeService.Add(_employee);

            if (result.Success)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
            }
        }
    }
}