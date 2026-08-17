using System;
using System.Drawing;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Forms
{
    // Read-only detail view for a decommissioned equipment row.
    // Mirrors LogDetailPopup's layout, but themed with the danger-red
    // accent used throughout the app's decommission flows.
    public class DecommissionDetailPopup : Form
    {
        private static Color CardBg => ThemeHelper.CardBg;
        private static Color BgPage => ThemeHelper.DashboardBg;
        private static Color DividerColor => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(220, 225, 230);

        public DecommissionDetailPopup(string equipmentCode, string equipmentName, string make,
            string model, string serialNumber, string reason, string decommDate)
        {
            const int w = 480;
            Text = "Decommission Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgPage;
            ClientSize = new Size(w, 100);

            var header = new Panel { Dock = DockStyle.Top, BackColor = ThemeHelper.Danger };
            var titleFont = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            string titleText = $"🗑  Decommission Details — {equipmentCode}";
            var titleSize = TextRenderer.MeasureText(titleText, titleFont, new Size(w - 40, int.MaxValue), TextFormatFlags.WordBreak);
            var lblTitle = new Label
            {
                Text = titleText,
                Font = titleFont,
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(w - 40, titleSize.Height),
                Location = new Point(20, 18)
            };
            header.Controls.Add(lblTitle);
            header.Height = lblTitle.Bottom + 18;
            Controls.Add(header);

            var body = new Panel { Location = new Point(0, header.Height), Width = w, BackColor = CardBg };
            Controls.Add(body);

            int y = 18;

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
                    new Size(w - 40, int.MaxValue), TextFormatFlags.WordBreak);

                var val = new Label
                {
                    Text = text,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = false,
                    Size = new Size(w - 40, sz.Height + 4),
                    Location = new Point(20, y)
                };
                body.Controls.Add(val);
                y += val.Height + 12;
            }

            Row("🏷", "Equipment Code", equipmentCode);
            Row("🖥", "Equipment Name", equipmentName);
            Row("🔢", "Make / Model / Serial", $"{make} • {model} • {serialNumber}");

            var divider = new Panel { Location = new Point(20, y), Size = new Size(w - 40, 1), BackColor = DividerColor };
            body.Controls.Add(divider);
            y += 16;

            Row("⚠", "Reason", reason);
            Row("📅", "Decommissioned On", decommDate);

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 38),
                Location = new Point((w - 120) / 2, y + 4)
            };
            ThemeHelper.StyleDangerButton(btnClose);
            btnClose.Click += (s, e) => Close();
            body.Controls.Add(btnClose);
            y += 60;

            body.Height = y;
            ClientSize = new Size(w, header.Height + body.Height);
        }
    }
}