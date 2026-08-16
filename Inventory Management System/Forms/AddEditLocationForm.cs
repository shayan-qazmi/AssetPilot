// Forms/AddEditLocationForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class AddEditLocationForm : Form
    {
        private Location _location;
        private LocationService _locationService;
        private bool _isEditMode;

        private TextBox txtName = null!;
        private TextBox txtBuilding = null!;
        private TextBox txtRoomNumber = null!;
        private TextBox txtRoomName = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private Color Accent => _isEditMode ? Color.FromArgb(245, 158, 11) : ThemeHelper.Header;
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224); // matches AllocationForm's dialog border
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        public AddEditLocationForm(Location? location = null)
        {
            _location = location ?? new Location();
            _isEditMode = location != null;
            _locationService = new LocationService();

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Edit Location" : "Add Location";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoScroll = false; // never scrolls — sized to fit exactly, same as the Allocation dialog
this.Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 440;
            const int margin = 28;
            int contentWidth = formWidth - margin * 2;

            // ── Header — auto-sized to its subtitle, never clips ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = Accent };
            this.Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = _isEditMode ? "📍  Edit Location" : "📍  Add Location",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = "A location can have multiple rooms — add one row per room";
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

            // ── Body ──────────────────────────────────────────────
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10) };
            this.Controls.Add(body);

            // ── Footer ──────────────────────────────────────────────
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(120, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "✓  Save", Size = new Size(150, 42) };
ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnSave.Location = new Point(formWidth - btnSave.Width - margin, 15);
            btnCancel.Location = new Point(btnSave.Left - btnCancel.Width - 12, 15);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            this.Controls.Add(pnlFooter);

            int y = 24;

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
                y += 22;
            }

            void PlaceControl(Control ctrl)
            {
                ctrl.Location = new Point(margin, y);
                ctrl.Width = contentWidth;
                if (ctrl.Height < 30) ctrl.Height = 30;
                body.Controls.Add(ctrl);
                y += ctrl.Height + 6;
            }

            void Hint(string text)
            {
                var font = new Font("Segoe UI", 8f, FontStyle.Italic);
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
                y += sz.Height + 4 + 14;
            }

            FieldLabel("Location Name", required: true);
            txtName = new TextBox();
            ThemeHelper.StyleTextBox(txtName);
            PlaceControl(txtName);
            Hint("The site or building this belongs to, e.g. \"Main Store\"");

            FieldLabel("Building");
            txtBuilding = new TextBox();
            ThemeHelper.StyleTextBox(txtBuilding);
            PlaceControl(txtBuilding);
            y += 4;

            // Room Number and Room Name side by side — they describe the
            // same room, so pairing them reads more clearly than stacking.
            int halfWidth = (contentWidth - 12) / 2;

            body.Controls.Add(new Label
            {
                Text = "Room Number",
                Location = new Point(margin, y),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            });
            body.Controls.Add(new Label
            {
                Text = "Room Name",
                Location = new Point(margin + halfWidth + 12, y),
                AutoSize = true,
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary
            });
            y += 22;

            txtRoomNumber = new TextBox { Location = new Point(margin, y), Width = halfWidth, Height = 30 };
            ThemeHelper.StyleTextBox(txtRoomNumber);
            body.Controls.Add(txtRoomNumber);

            txtRoomName = new TextBox { Location = new Point(margin + halfWidth + 12, y), Width = halfWidth, Height = 30 };
            ThemeHelper.StyleTextBox(txtRoomName);
            body.Controls.Add(txtRoomName);
            y += 36;

            Hint("Both optional — fill in either (or both) to identify a specific room within this location");

            y += 8;

            // ── Final sizing — dialog grows to fit exactly, no scrollbar ──
            body.Size = new Size(formWidth, y);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            this.ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, this.ClientSize.Height - footerHeight);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                txtName.Text = _location.LocationName;
                txtBuilding.Text = _location.Building;
                txtRoomNumber.Text = _location.RoomNumber;
                txtRoomName.Text = _location.RoomName ?? "";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ToastHelper.Show(this, "Location Name is required.", ToastType.Warning);
                return;
            }

            _location.LocationName = txtName.Text.Trim();
            _location.Building = txtBuilding.Text.Trim();
            _location.RoomNumber = txtRoomNumber.Text.Trim();
            _location.RoomName = string.IsNullOrWhiteSpace(txtRoomName.Text)
                ? null
                : txtRoomName.Text.Trim();

            var result = _isEditMode ? _locationService.Update(_location) : _locationService.Add(_location);

            if (result.Success)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
            }
        }
    }
}