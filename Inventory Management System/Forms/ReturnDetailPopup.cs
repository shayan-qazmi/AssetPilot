using Inventory_Management_System.Helpers;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public class ReturnDetailPopup : Form
    {
        public ReturnDetailPopup(
            string equipmentCode, string qrCode, string equipmentName,
            string make, string model, string serialNumber,
            string employee, string location,
            string allocatedDate, string returnDate, string timestamp,
            string durationDays, string condition, bool isOverdue, string source)
        {
            const int w = 480;
            Text = "Return Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeHelper.CardBg;
            ClientSize = new Size(w, 100);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = ThemeHelper.Header };
            var lblTitle = new Label
            {
                Text = "↩  Return Details",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            var body = new Panel { Location = new Point(0, header.Height), Width = w, AutoScroll = false, BackColor = ThemeHelper.CardBg };
            Controls.Add(body);

            int y = 18;
            int maxWidth = w - 40;

            void Row(string icon, string label, string value)
            {
                var lbl = new Label
                {
                    Text = $"{icon}  {label}",
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.TextSecondary,
                    AutoSize = true,
                    Location = new Point(20, y)
                };
                body.Controls.Add(lbl);
                y += 18;

                var text = string.IsNullOrWhiteSpace(value) ? "—" : value;
                var sz = TextRenderer.MeasureText(text, ThemeHelper.FontBold,
                    new Size(maxWidth, int.MaxValue), TextFormatFlags.WordBreak);

                var val = new Label
                {
                    Text = text,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = false,
                    Size = new Size(maxWidth, sz.Height + 4),
                    Location = new Point(20, y)
                };
                body.Controls.Add(val);
                y += val.Height + 12;
            }

            Row("🏷", "Equipment Code | QR Code", $"{equipmentCode}  |  {qrCode}");

            string details = string.Join("  •  ", new[] { equipmentName, make, model }
                .Where(v => !string.IsNullOrWhiteSpace(v) && v != "—"));
            Row("🧾", "Name, Make, Model", string.IsNullOrWhiteSpace(details) ? "—" : details);

            Row("🔢", "Serial Number", serialNumber);
            Row("👤", "Returned By", employee);
            Row("📍", "Location", location);

            var divider = new Panel { Location = new Point(20, y), Size = new Size(w - 40, 1), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(220, 225, 230) };
            body.Controls.Add(divider);
            y += 16;

            Row("📅", "Allocated On", allocatedDate);
            Row("↩", "Return Date", returnDate);
            Row("🕒", "Processed At", timestamp);
            Row("⏱", "Duration", string.IsNullOrWhiteSpace(durationDays) ? "—" : $"{durationDays} day(s)");

            var overdueVal = isOverdue ? "Yes — returned late" : "No";
            var overdueLbl = new Label
            {
                Text = "⚠  Overdue",
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = true,
                Location = new Point(20, y)
            };
            body.Controls.Add(overdueLbl);
            y += 18;
            var overdueValLbl = new Label
            {
                Text = overdueVal,
                Font = ThemeHelper.FontBold,
                ForeColor = isOverdue ? ThemeHelper.Danger : ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(maxWidth, 22),
                Location = new Point(20, y)
            };
            body.Controls.Add(overdueValLbl);
            y += overdueValLbl.Height + 12;

            Row("🔖", "Source", source);

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 38),
                Location = new Point((w - 120) / 2, y + 4)
            };
            ThemeHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();
            body.Controls.Add(btnClose);
            y += 60;

            body.Height = y;
            ClientSize = new Size(w, header.Height + body.Height);
        }
    }
}