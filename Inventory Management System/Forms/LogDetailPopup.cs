using Inventory_Management_System.Helpers;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public class LogDetailPopup : Form
    {
        public LogDetailPopup(string action, string changedBy, string when,
                               string equipmentCode, string qrCode,
                               string equipmentName, string make, string model,
                               string oldValue, string newValue)
        {
            const int w = 480;
            Text = "Log Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeHelper.CardBg;
            ClientSize = new Size(w, 100);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = ThemeHelper.Header };
            var lblTitle = new Label
            {
                Text = "📋  Log Details",
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

            // Row now measures the text and wraps/grows instead of clipping at a fixed height
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

            Row("🔖", "Action", action);
            Row("👤", "Changed By", changedBy);
            Row("🕒", "When", when);
            Row("🏷", "Equipment Code | QR Code", $"{equipmentCode}  |  {qrCode}");

            string details = string.Join("  •  ", new[] { equipmentName, make, model }
                .Where(v => !string.IsNullOrWhiteSpace(v) && v != "—"));
            Row("🧾", "Name, Make, Model", string.IsNullOrWhiteSpace(details) ? "—" : details);

            var divider = new Panel { Location = new Point(20, y), Size = new Size(w - 40, 1), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(220, 225, 230) };
            body.Controls.Add(divider);
            y += 16;

            void Section(string title, string content, Color bg, Color fg)
            {
                var lblSection = new Label
                {
                    Text = title,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(20, y)
                };
                body.Controls.Add(lblSection);
                y += 22;

                var lines = string.IsNullOrWhiteSpace(content) || content == "—"
                    ? new[] { "—" }
                    : content.Split(" | ", StringSplitOptions.RemoveEmptyEntries);

                var panel = new Panel
                {
                    Location = new Point(20, y),
                    Width = w - 40,
                    BackColor = bg
                };

                int innerY = 8;
                int innerWidth = w - 56;
                foreach (var line in lines)
                {
                    var lineText = "•  " + line.Trim();
                    var lineSz = TextRenderer.MeasureText(lineText, ThemeHelper.FontDefault,
                        new Size(innerWidth, int.MaxValue), TextFormatFlags.WordBreak);

                    var lblLine = new Label
                    {
                        Text = lineText,
                        Font = ThemeHelper.FontDefault,
                        ForeColor = fg,
                        AutoSize = false,
                        Size = new Size(innerWidth, lineSz.Height + 4),
                        Location = new Point(8, innerY)
                    };
                    panel.Controls.Add(lblLine);
                    innerY += lblLine.Height + 4;
                }
                panel.Height = innerY + 6;
                body.Controls.Add(panel);
                y += panel.Height + 14;
            }

            Section("Before", oldValue,
        ThemeHelper.IsDarkMode ? Color.FromArgb(50, 30, 30) : Color.FromArgb(255, 245, 245),
        ThemeHelper.IsDarkMode ? Color.FromArgb(255, 140, 140) : Color.FromArgb(153, 27, 27));
            Section("After", newValue,
                ThemeHelper.IsDarkMode ? Color.FromArgb(25, 40, 30) : Color.FromArgb(240, 253, 244),
                ThemeHelper.IsDarkMode ? Color.FromArgb(140, 220, 160) : Color.FromArgb(22, 101, 52));

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