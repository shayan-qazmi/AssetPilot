// Forms/EquipmentDetailPopup.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Services;

namespace Inventory_Management_System.Forms
{
    public class EquipmentDetailPopup : Form
    {
        private Equipment _equipment;

        // ── Layout Constants ─────────────────────────────────────────────────
        private const int DialogWidth = 860;
        private const int PaddingOuter = 20;
        private const int ColLeft = 30;
        private const int ColRight = 450;
        private const int ColWidth = 380;
        private const int RowH = 36;
        private const int SectionGap = 14;
        private const int LabelW = 155;
        private const int ValueX = 168;
        private const int ValueW = 200;

        // ── Color Overrides (supplement ThemeHelper) ─────────────────────────
        private static readonly Color BgPage = Color.FromArgb(245, 247, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color CardBorderColor = Color.FromArgb(220, 225, 230);
        private static readonly Color SectionTitle = Color.FromArgb(46, 125, 50);
        private static readonly Color RowEven = Color.White;
        private static readonly Color RowOdd = Color.FromArgb(248, 250, 248);
        private static readonly Color SeparatorColor = Color.FromArgb(234, 237, 241);
        private static readonly Color BadgeBorder = Color.FromArgb(200, 210, 200);

        public EquipmentDetailPopup(Equipment equipment)
        {
            _equipment = equipment;

            using var db = new InventoryDbContext();
            var eq = db.Equipment
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Location)
                .Include(e => e.Allocations)
                    .ThenInclude(a => a.Employee)
                .Include(e => e.ProcurementType)
                .FirstOrDefault(e => e.Id == equipment.Id);
            if (eq != null) _equipment = eq;

            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"Equipment Details — {_equipment.EquipmentCode}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgPage;

            // ── Header Strip ────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = ThemeHelper.Header,
            };

            var lblTitle = new Label
            {
                Text = $"  Equipment Details — {_equipment.EquipmentCode}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            // ── Card Container ───────────────────────────────────────────────
            var card = new Panel
            {
                BackColor = CardBg,
                Location = new Point(PaddingOuter, header.Height + PaddingOuter),
                Width = DialogWidth - PaddingOuter * 2,
            };
            card.Paint += DrawCardBorder;
            Controls.Add(card);

            int yLeft = SectionGap;
            int yRight = SectionGap;

            // ── Left Column: Equipment Information ───────────────────────────
            AddSectionHeader(card, "Equipment Information", ColLeft, ref yLeft);

            bool altLeft = false;
            AddField(card, "Equipment Code", _equipment.EquipmentCode, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "QR Code", _equipment.QrCode, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Name", _equipment.Name, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Make", _equipment.Make, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Model", _equipment.Model, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Serial Number", _equipment.SerialNumber, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Category", _equipment.Category, ColLeft, ref yLeft, ref altLeft);
            AddStatusField(card, "Status", _equipment.CurrentStatus, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Condition", _equipment.ConditionGrade, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Purchase Date", _equipment.PurchaseDate?.ToString("yyyy-MM-dd"), ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Procurement Type", _equipment.ProcurementType?.TypeName, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Vendor Name", _equipment.VendorName, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Warranty Expiry", _equipment.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "—", ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Description", _equipment.Description, ColLeft, ref yLeft, ref altLeft);
            AddField(card, "Notes", _equipment.Notes, ColLeft, ref yLeft, ref altLeft);
            AddPictureField(card, "Picture", _equipment.PicturePath, ColLeft, ref yLeft, ref altLeft);

            // ── Right Column: Current Allocation ─────────────────────────────
            AddSectionHeader(card, "Current Allocation", ColRight, ref yRight);

            var activeAlloc = _equipment.Allocations.FirstOrDefault(a => a.Status == "approved");
            bool altRight = false;

            AddField(card, "Current Location", activeAlloc?.Location?.LocationName ?? "—", ColRight, ref yRight, ref altRight);
            AddField(card, "Currently Assigned To", activeAlloc?.Employee?.FullName ?? "—", ColRight, ref yRight, ref altRight);
            AddField(card, "Allocation Date", activeAlloc?.AllocationDate.ToString("yyyy-MM-dd") ?? "—", ColRight, ref yRight, ref altRight);
            AddField(card, "Expected Return", activeAlloc?.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "—", ColRight, ref yRight, ref altRight);

            // ── Right Column: Maintenance History ────────────────────────────
            AddSectionHeader(card, "Maintenance History", ColRight, ref yRight);

            List<MaintenanceRecord> records = new();
            try
            {
                var svc = new MaintenanceService();
                var result = svc.GetByEquipmentId(_equipment.Id);
                if (result.Success && result.Data != null)
                    records = result.Data;
            }
            catch { }

            if (records.Count == 0)
            {
                AddField(card, "History", "No maintenance history recorded.", ColRight, ref yRight, ref altRight);
            }
            else
            {
                for (int i = 0; i < records.Count; i++)
                {
                    var rec = records[i];

                    // Small record label so multiple entries are visually separable,
                    // using the exact same row/label styling as every other field.
                    AddField(card, $"Record #{i + 1}", rec.ReportedDate.ToString("yyyy-MM-dd"), ColRight, ref yRight, ref altRight);
                    AddMaintenanceStatusField(card, "Status", rec.Status, ColRight, ref yRight, ref altRight);
                    AddField(card, "Issue", rec.IssueDescription, ColRight, ref yRight, ref altRight);
                    AddField(card, "Serviced By", rec.ServicedBy, ColRight, ref yRight, ref altRight);
                    string costStr = rec.Cost.HasValue ? $"₨{rec.Cost.Value:N2}" : "—";
                    AddField(card, "Cost", costStr, ColRight, ref yRight, ref altRight);
                    AddField(card, "Resolved", rec.ResolvedDate?.ToString("yyyy-MM-dd"), ColRight, ref yRight, ref altRight);
                }
            }

            // ── Divider between columns ──────────────────────────────────────
            int dividerX = ColRight - 10;
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, dividerX, SectionGap, dividerX, Math.Max(yLeft, yRight) - 4);
            };

            // ── Size card ────────────────────────────────────────────────────
            int cardH = Math.Max(yLeft, yRight) + SectionGap;
            card.Height = cardH;

            // ── Close Button ─────────────────────────────────────────────────
            int btnY = header.Height + PaddingOuter + cardH + 14;
            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 34),
                Location = new Point((DialogWidth - 100) / 2, btnY),
                Font = ThemeHelper.FontDefault,
                Cursor = Cursors.Hand,
            };
            ThemeHelper.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            ClientSize = new Size(DialogWidth, btnY + 34 + PaddingOuter);
        }

        // ── Section Header with left accent bar ──────────────────────────────
        private void AddSectionHeader(Panel card, string title, int x, ref int y)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, 32),
                BackColor = Color.Transparent,
            };

            var accent = new Panel
            {
                Location = new Point(0, 4),
                Size = new Size(3, 22),
                BackColor = SectionTitle,
            };
            pnl.Controls.Add(accent);

            var lbl = new Label
            {
                Text = title,
                Location = new Point(10, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = SectionTitle,
            };
            pnl.Controls.Add(lbl);

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 215, 180), 1);
                e.Graphics.DrawLine(pen, 0, 31, ColWidth, 31);
            };

            card.Controls.Add(pnl);
            y += 38;
        }

        // ── Standard Field Row ────────────────────────────────────────────────
        private void AddField(Panel card, string labelName, string? valueText, int x, ref int y, ref bool isAlt)
        {
            string val = string.IsNullOrWhiteSpace(valueText) ? "—" : valueText;

            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(10, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var lblValue = new Label
            {
                Text = val,
                Location = new Point(ValueX, 0),
                Size = new Size(ValueW, RowH),
                Font = ThemeHelper.FontDefault,
                ForeColor = ThemeHelper.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
            };

            int preferredH = lblValue.GetPreferredSize(new Size(ValueW, 0)).Height;
            int actualH = Math.Max(RowH, preferredH + 14);
            lblName.Height = actualH;
            lblValue.Height = actualH;

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, actualH),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 10, pnl.Height - 1, pnl.Width - 10, pnl.Height - 1);
            };

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblValue);
            card.Controls.Add(pnl);

            y += actualH;
            isAlt = !isAlt;
        }

        // ── Picture Row: "View Picture" button if a picture is attached,
        // otherwise renders exactly like an empty AddField row ("—"). ────────
        private void AddPictureField(Panel card, string labelName, string? picturePath, int x, ref int y, ref bool isAlt)
        {
            bool hasPicture = !string.IsNullOrWhiteSpace(picturePath);

            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(10, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, RowH),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 10, pnl.Height - 1, pnl.Width - 10, pnl.Height - 1);
            };

            pnl.Controls.Add(lblName);

            if (hasPicture)
            {
                var btnView = new Button
                {
                    Text = "🖼 View Picture",
                    Location = new Point(ValueX, 4),
                    Size = new Size(120, 28),
                    Font = ThemeHelper.FontSmall,
                    Cursor = Cursors.Hand,
                };
                ThemeHelper.StyleSecondaryButton(btnView);
                btnView.Click += (s, e) => ShowPicture(picturePath!);
                pnl.Controls.Add(btnView);
            }
            else
            {
                var lblValue = new Label
                {
                    Text = "—",
                    Location = new Point(ValueX, 0),
                    Size = new Size(ValueW, RowH),
                    Font = ThemeHelper.FontDefault,
                    ForeColor = ThemeHelper.TextPrimary,
                    TextAlign = ContentAlignment.MiddleLeft,
                };
                pnl.Controls.Add(lblValue);
            }

            card.Controls.Add(pnl);

            y += RowH;
            isAlt = !isAlt;
        }

        // ── Opens the picture in a small popup (images) or the OS default
        // viewer (PDFs, since building an in-app PDF renderer is out of scope
        // for this feature). ──────────────────────────────────────────────
        private void ShowPicture(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(Application.StartupPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
            {
                MessageBox.Show("The picture file could not be found on disk. It may have been moved or deleted.",
                    "Picture Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ext = System.IO.Path.GetExtension(fullPath).ToLowerInvariant();

            if (ext == ".pdf")
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open PDF: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            Image? img = null;
            try
            {
                using var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                img = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load picture: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var frm = new Form
            {
                Text = $"Picture — {_equipment.EquipmentCode}",
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.Black,
            };

            // Cap the initial window size to something reasonable while
            // preserving the image's aspect ratio, but still allow the user
            // to resize/maximize afterward.
            int maxW = 900, maxH = 700;
            double scale = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);
            if (scale > 1) scale = 1; // never upscale small images
            int initW = Math.Max(300, (int)(img.Width * scale));
            int initH = Math.Max(200, (int)(img.Height * scale));

            var pic = new PictureBox
            {
                Image = img,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
            };
            frm.Controls.Add(pic);
            frm.ClientSize = new Size(initW, initH + 20); // small buffer for title bar rendering quirks

            frm.FormClosed += (s, e) => img?.Dispose();
            frm.ShowDialog(this);
        }

        // ── Status Badge Row (Equipment status) ───────────────────────────────
        private void AddStatusField(Panel card, string labelName, string status, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(10, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Color badgeBg;
            string displayText;
            switch (status)
            {
                case "in_store":
                    badgeBg = Color.FromArgb(34, 139, 34);
                    displayText = "In Store";
                    break;
                case "allocated":
                    badgeBg = Color.FromArgb(217, 119, 6);
                    displayText = "Allocated";
                    break;
                case "under_repair":
                    badgeBg = Color.FromArgb(220, 85, 40);
                    displayText = "Under Repair";
                    break;
                case "decommissioned":
                    badgeBg = Color.FromArgb(185, 28, 28);
                    displayText = "Decommissioned";
                    break;
                default:
                    badgeBg = ThemeHelper.TextSecondary;
                    displayText = status ?? "—";
                    break;
            }

            RenderBadgeRow(card, lblName, badgeBg, displayText, x, y, RowH, isAlt);

            y += RowH;
            isAlt = !isAlt;
        }

        // ── Status Badge Row (Maintenance record status) ──────────────────────
        // Mirrors AddStatusField exactly so the pill looks identical, just with
        // maintenance-specific status values instead of equipment status values.
        private void AddMaintenanceStatusField(Panel card, string labelName, string status, int x, ref int y, ref bool isAlt)
        {
            var lblName = new Label
            {
                Text = labelName + ":",
                Location = new Point(10, 0),
                Size = new Size(LabelW, RowH),
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            Color badgeBg;
            string displayText;
            switch (status)
            {
                case "resolved":
                    badgeBg = Color.FromArgb(34, 139, 34);
                    displayText = "Resolved";
                    break;
                case "in_progress":
                    badgeBg = Color.FromArgb(217, 119, 6);
                    displayText = "In Progress";
                    break;
                default:
                    badgeBg = Color.FromArgb(220, 85, 40);
                    displayText = "Open";
                    break;
            }

            RenderBadgeRow(card, lblName, badgeBg, displayText, x, y, RowH, isAlt);

            y += RowH;
            isAlt = !isAlt;
        }

        // ── Shared badge-row renderer used by both status methods above ──────
        private void RenderBadgeRow(Panel card, Label lblName, Color badgeBg, string displayText,
            int x, int y, int rowH, bool isAlt)
        {
            var badge = new Label
            {
                Text = $"  {displayText}  ",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = badgeBg,
                Padding = new Padding(6, 3, 6, 3),
            };

            badge.Location = new Point(ValueX, (rowH - badge.GetPreferredSize(Size.Empty).Height) / 2);

            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ColWidth, rowH),
                BackColor = isAlt ? RowOdd : RowEven,
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 10, pnl.Height - 1, pnl.Width - 10, pnl.Height - 1);
            };

            badge.Paint += (s, e) =>
            {
                var lbl = (Label)s!;
                var rect = new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1);
                using var path = RoundedRect(rect, 10);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(badgeBg);
                e.Graphics.FillPath(brush, path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString(displayText, badge.Font, textBrush, rect, sf);
            };
            badge.BackColor = Color.Transparent;

            pnl.Controls.Add(lblName);
            pnl.Controls.Add(badge);
            card.Controls.Add(pnl);
        }

        // ── Card Border + Shadow ──────────────────────────────────────────────
        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel pnl) return;
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);

            using var shadowBrush = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillRectangle(shadowBrush, new Rectangle(2, 2, pnl.Width - 1, pnl.Height - 1));

            using var bgBrush = new SolidBrush(CardBg);
            g.FillRectangle(bgBrush, rect);

            using var pen = new Pen(CardBorderColor, 1);
            g.DrawRectangle(pen, rect);
        }

        // ── Rounded Rectangle Helper ─────────────────────────────────────────
        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}