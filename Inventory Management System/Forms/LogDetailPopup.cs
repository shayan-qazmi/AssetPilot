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
                               string equipmentCode, string oldValue, string newValue)
        {
            const int w = 480;
            Text = "Log Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
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

            var body = new Panel { Location = new Point(0, header.Height), Width = w, AutoScroll = false, BackColor = Color.White };
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

                var val = new Label
                {
                    Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = false,
                    Size = new Size(w - 40, 20),
                    Location = new Point(20, y)
                };
                body.Controls.Add(val);
                y += 30;
            }

            Row("🔖", "Action", action);
            Row("👤", "Changed By", changedBy);
            Row("🕒", "When", when);
            Row("🏷", "Equipment", equipmentCode);

            var divider = new Panel { Location = new Point(20, y), Size = new Size(w - 40, 1), BackColor = Color.FromArgb(220, 225, 230) };
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
                foreach (var line in lines)
                {
                    var lblLine = new Label
                    {
                        Text = "•  " + line.Trim(),
                        Font = ThemeHelper.FontDefault,
                        ForeColor = fg,
                        AutoSize = false,
                        Size = new Size(w - 56, 20),
                        Location = new Point(8, innerY)
                    };
                    panel.Controls.Add(lblLine);
                    innerY += 22;
                }
                panel.Height = innerY + 6;
                body.Controls.Add(panel);
                y += panel.Height + 14;
            }

            Section("Before", oldValue, Color.FromArgb(255, 245, 245), Color.FromArgb(153, 27, 27));
            Section("After", newValue, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52));

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