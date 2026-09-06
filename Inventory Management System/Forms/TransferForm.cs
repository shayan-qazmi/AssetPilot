// Forms/TransferForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Inventory_Management_System.Forms
{
    public partial class TransferForm : Form, IThemableForm
    {
        private readonly AllocationService _allocationService = new();
        private readonly EquipmentService _equipmentService = new();
        private readonly EmployeeService _employeeService = new();
        private readonly LocationService _locationService = new();
        private readonly AuditService _audit = new();
        private List<Allocation> _allAllocations = new(); // currently allocated equipment


        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        public TransferForm()
        {
            InitializeComponent();

            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadAllocatedEquipment();

            this.Shown += (s, e) =>
            {
                dgvTransfer.ClearSelection();
                dgvTransfer.CurrentCell = null;
            };
        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x200;

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ApplyRoundedSearchStyle(TextBox txt)
        {
            var parent = txt.Parent;
            if (parent == null) return;

            var originalSize = txt.Size;
            var originalMargin = txt.Margin;
            int idx = parent.Controls.GetChildIndex(txt);
            parent.Controls.Remove(txt);

            var wrapper = new Panel
            {
                Size = originalSize,
                Margin = originalMargin,
                BackColor = Color.White
            };

            Color inputBorder = Color.FromArgb(209, 213, 219);
            Color focusBorder = Color.FromArgb(0x52, 0xA2, 0x3F);
            Color currentBorder = inputBorder;

            void RoundInput(object? s, EventArgs e)
            {
                if (wrapper.Width <= 0 || wrapper.Height <= 0) return;
                using var path = RoundedRectPath(new RectangleF(0, 0, wrapper.Width, wrapper.Height), 8f);
                wrapper.Region = new Region(path);
            }
            wrapper.Resize += RoundInput;

            wrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectPath(new RectangleF(0.5f, 0.5f, wrapper.Width - 1, wrapper.Height - 1), 8f);
                using var pen = new Pen(currentBorder, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 8.5f),
                ForeColor = ThemeHelper.TextSecondary,
                AutoSize = false,
                Size = new Size(24, wrapper.Height),
                Location = new Point(6, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            txt.BorderStyle = BorderStyle.None;
            int exStyle = GetWindowLong(txt.Handle, GWL_EXSTYLE);
            SetWindowLong(txt.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_CLIENTEDGE);
            txt.BackColor = Color.White;
            txt.Location = new Point(30, (wrapper.Height - txt.Height) / 2);
            txt.Width = wrapper.Width - 38;

            wrapper.Controls.Add(searchIcon);
            wrapper.Controls.Add(txt);
            parent.Controls.Add(wrapper);
            parent.Controls.SetChildIndex(wrapper, idx);

            txt.Enter += (s, e) => { currentBorder = focusBorder; wrapper.Invalidate(); };
            txt.Leave += (s, e) => { currentBorder = inputBorder; wrapper.Invalidate(); };

            RoundInput(null, EventArgs.Empty);
        }


        internal static string FormatRoom(Location? loc)
        {
            if (loc == null) return "—";
            bool hasNum = !string.IsNullOrWhiteSpace(loc.RoomNumber);
            bool hasName = !string.IsNullOrWhiteSpace(loc.RoomName);
            if (!hasNum && !hasName) return "—";
            string room = hasNum ? $"Room {loc.RoomNumber}" : "";
            if (hasName) room = string.IsNullOrEmpty(room) ? loc.RoomName! : $"{room} ({loc.RoomName})";
            return room;
        }

        // All toasts in Transfer screen are purple
        private void ShowToast(string message, ToastType type = ToastType.Success)
        {
            ToastHelper.Show(this, message, type, 3000, Color.FromArgb(156, 39, 176));
        }

        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnTransfer, Color.FromArgb(123, 31, 162));
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvTransfer, Color.FromArgb(123, 31, 162));
            panelBottom.BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
        }

        private void LoadAllocatedEquipment()
        {
            try
            {
                using var db = new InventoryDbContext();
                _allAllocations = db.Allocations
                    .Where(a => a.Status == "approved")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .OrderBy(a => a.Equipment!.EquipmentCode)
                    .ToList();

                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Transfer failed — full details");
            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvTransfer.Rows.Clear();

            List<Allocation> matched;
            if (string.IsNullOrWhiteSpace(filter))
            {
                matched = _allAllocations;
            }
            else
            {
                var codeMatches = _allAllocations.Where(a =>
                    (!string.IsNullOrEmpty(a.Equipment?.QrCode) &&
                        a.Equipment.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.Equipment?.EquipmentCode) &&
                        a.Equipment.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                matched = codeMatches.Count > 0
                    ? codeMatches
                    : _allAllocations.Where(a =>
                        FuzzySearchHelper.IsFuzzyMatch(a.Equipment?.Name, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.Employee?.FullName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.Location?.LocationName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(a.AllocationDate.ToString("yyyy-MM-dd"), filter))
                        .OrderByDescending(a => Math.Max(
                            FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, filter),
                            FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, filter)))
                        .ToList();
            }

            _totalMatchingCount = matched.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchingCount / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var pageItems = matched
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            foreach (var item in pageItems)
            {
                string status = (item.ExpectedReturnDate.HasValue &&
                                  item.ExpectedReturnDate.Value < DateOnly.FromDateTime(DateTime.Today))
                                  ? "Overdue" : "approved";

                string locName = item.Location?.LocationName ?? "—";
                string room = FormatRoom(item.Location);
                string combinedLocation = room == "—" ? locName : $"{locName} - {room}";

                int rowIdx = dgvTransfer.Rows.Add(
                    item.Id,
                    string.IsNullOrWhiteSpace(item.Equipment?.QrCode) ? "—" : item.Equipment!.QrCode,
                    item.Equipment?.EquipmentCode ?? "—",
                    item.Equipment?.Name ?? "—",
                    item.Employee?.FullName ?? "—",
                    combinedLocation,
                    item.AllocationDate.ToString("yyyy-MM-dd"),
                    status
                );

                var statusCell = dgvTransfer.Rows[rowIdx].Cells["Status"];
                if (status == "Overdue")
                {
                    statusCell.Style.BackColor = Color.FromArgb(255, 243, 224);
                    statusCell.Style.ForeColor = Color.FromArgb(255, 160, 0);
                }
                else
                {
                    statusCell.Style.BackColor = Color.FromArgb(200, 230, 201);
                    statusCell.Style.ForeColor = Color.FromArgb(27, 94, 32);
                }
                statusCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {_totalMatchingCount} allocated items";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private void btnTransfer_Click(object sender, EventArgs e)
        {
            OpenTransferDialog(preselectedAllocationId: null);
        }

        private void dgvTransfer_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvTransfer.Rows.Count) return;

            var clickedRow = dgvTransfer.Rows[e.RowIndex];
            var idValue = clickedRow.Cells["AllocationId"].Value;
            if (idValue == null || !int.TryParse(idValue.ToString(), out int allocationId)) return;

            OpenTransferDialog(preselectedAllocationId: allocationId);
        }

        private void OpenTransferDialog(int? preselectedAllocationId)
        {
            try
            {
                using var db = new InventoryDbContext();
                var activeAllocations = db.Allocations
      .Where(a => a.Status == "approved")
      .Include(a => a.Equipment)
      .Include(a => a.Employee)
      .Include(a => a.Location)
      .ToList();

                if (activeAllocations.Count == 0)
                {
                    ShowToast("No allocated equipment available to transfer.", ToastType.Info);
                    return;
                }

                using var frm = new TransferDialog(
                    activeAllocations,
                    _employeeService.GetAll().Data ?? new(),
                    _locationService.GetAll().Data ?? new(),
                    preselectedAllocationId);

                if (frm.ShowDialog() != DialogResult.OK) return;

                var oldAlloc = db.Allocations.FirstOrDefault(a => a.Id == frm.OldAllocationId);
                if (oldAlloc == null) return;

                oldAlloc.Status = "returned";
                oldAlloc.ActualReturnDate = DateOnly.FromDateTime(DateTime.Today);

                var newAlloc = new Allocation
                {
                    EquipmentId = oldAlloc.EquipmentId,
                    EmployeeId = frm.NewEmployeeId,
                    LocationId = frm.NewLocationId,
                    Status = "approved",
                    AllocationDate = DateTime.UtcNow,
                    AllocatedByUserId = 1,
                    ApprovedByUserId = 1,
                    ApprovalDate = DateTime.UtcNow,
                    PreviousAllocationId = oldAlloc.Id
                };

                db.Allocations.Add(newAlloc);
                db.SaveChanges();

                string oldEmpName = oldAlloc.EmployeeId.HasValue ? db.Employees.FirstOrDefault(x => x.Id == oldAlloc.EmployeeId)?.FullName ?? "Unknown" : "—";
                string newEmpName = newAlloc.EmployeeId.HasValue ? db.Employees.FirstOrDefault(x => x.Id == newAlloc.EmployeeId)?.FullName ?? "Unknown" : "—";

                var oldLocation = oldAlloc.LocationId.HasValue ? db.Locations.FirstOrDefault(x => x.Id == oldAlloc.LocationId) : null;
                var newLocation = newAlloc.LocationId.HasValue ? db.Locations.FirstOrDefault(x => x.Id == newAlloc.LocationId) : null;

                string oldLocDisplay = oldLocation == null ? "—" :
                    (FormatRoom(oldLocation) == "—" ? oldLocation.LocationName : $"{oldLocation.LocationName} - {FormatRoom(oldLocation)}");
                string newLocDisplay = newLocation == null ? "—" :
                    (FormatRoom(newLocation) == "—" ? newLocation.LocationName : $"{newLocation.LocationName} - {FormatRoom(newLocation)}");

                _audit.Log(oldAlloc.EquipmentId, AuditService.ActionTransferred,
                    $"Employee: {oldEmpName} | Location: {oldLocDisplay}",
                    $"Employee: {newEmpName} | Location: {newLocDisplay}");

                LoadAllocatedEquipment();
                ShowToast("Equipment transferred successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Transfer failed: " + ex.ToString(), ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadAllocatedEquipment();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                RefreshGrid(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            RefreshGrid(txtSearch.Text.Trim());
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV files|*.csv",
                    FileName = $"AllocatedEquipment_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("QR Code,Equipment Code,Equipment Name,Currently With,Current Location,Allocated Since");
                foreach (DataGridViewRow row in dgvTransfer.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
E(row.Cells["QRCode"].Value), E(row.Cells["EquipmentCode"].Value),
                        E(row.Cells["EquipmentName"].Value), E(row.Cells["CurrentEmployee"].Value),
                        E(row.Cells["CurrentLocation"].Value),
                        E(row.Cells["AllocatedSince"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ShowToast("Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Export failed: " + ex.Message, ToastType.Error);
            }
        }
    }

    // ── Small typed option class — replaces the old List<dynamic>, which
    // WinForms' ComboBox data-binding cannot reliably read DisplayMember/
    // ValueMember from, causing SelectedValue to silently stay null. ────
    public class TransferEquipmentOption
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } = "";
    }

    // ── Transfer Dialog — two-column layout (Equipment | Transfer To),
    // matching the Allocation dialog. Transfer button color is
    // intentionally left unchanged (purple). ──
    public class TransferDialog : Form
    {
        public int OldAllocationId { get; private set; }
        public int? NewEmployeeId { get; private set; }
        public int? NewLocationId { get; private set; }

        private TextBox txtEquipmentSearch = null!;
        private ComboBox cmbAllocation = null!;
        private ComboBox cmbEmployee = null!;
        private ComboBox cmbLocation = null!;
        private ComboBox cmbRoom = null!;
        private Label lblCurrentDetails = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly List<Allocation> _allocations;
        private readonly List<Employee> _employees;
        private readonly List<Location> _locations;
        private readonly int? _preselectedAllocationId;
        private EntitySuggestHelper<Allocation>? _equipmentSuggest;
        private List<TransferEquipmentOption> _allocationItems = new();

        private static readonly Color TransferAccent = Color.FromArgb(123, 31, 162); // unchanged
        private static Color CardBorder => ThemeHelper.IsDarkMode ? Color.FromArgb(55, 58, 65) : Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        public TransferDialog(List<Allocation> allocations, List<Employee> employees, List<Location> locations,
            int? preselectedAllocationId = null)
        {
            _allocations = allocations;
            _employees = employees;
            _locations = locations;
            _preselectedAllocationId = preselectedAllocationId;
            BuildUI();
        }

        private void ShowToast(string message, ToastType type = ToastType.Success)
        {
            ToastHelper.Show(this, message, type, 3000, Color.FromArgb(156, 39, 176));
        }

        private class LocationNameOption
        {
            public string? Name { get; set; }
            public string DisplayName { get; set; } = "";
        }

        private class EmployeeOption
        {
            public int? Id { get; set; }
            public string FullName { get; set; } = "";
        }

        private class RoomOption
        {
            public int Id { get; set; }
            public string Display { get; set; } = "";
        }

        private static string RoomOptionDisplay(Location l)
        {
            bool hasNum = !string.IsNullOrWhiteSpace(l.RoomNumber);
            bool hasRoomName = !string.IsNullOrWhiteSpace(l.RoomName);
            if (!hasNum && !hasRoomName) return "— Unspecified room —";
            string room = hasNum ? $"Room {l.RoomNumber}" : "";
            if (hasRoomName) room = string.IsNullOrEmpty(room) ? l.RoomName! : $"{room} ({l.RoomName})";
            return room;
        }

        private bool _suppressSync = false;

        private void BuildUI()
        {
            _suppressSync = true;
            Text = "New Transfer";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScroll = false;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            // ── Widened + split into two columns, like the Allocation dialog ──
            const int formWidth = 820;
            const int margin = 28;
            const int colGap = 30;
            int contentWidth = formWidth - margin * 2;
            int colWidth = (contentWidth - colGap) / 2;
            int leftX = margin;
            int rightX = margin + colWidth + colGap;

            // ── Header — kept in Transfer's purple accent ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = TransferAccent };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "⇄  New Transfer",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = "Move allocated equipment to a new employee and location";
            var subtitleSize = TextRenderer.MeasureText(
                subtitleText, subtitleFont, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak);
            var lblSubtitle = new Label
            {
                Text = subtitleText,
                Font = subtitleFont,
                ForeColor = Color.FromArgb(240, 228, 245),
                AutoSize = false,
                Size = new Size(contentWidth, subtitleSize.Height + 4),
                Location = new Point(margin, lblTitle.Bottom + 8)
            };
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Height = lblSubtitle.Bottom + 20;

            // ── Body ──
            var body = new Panel { Location = new Point(0, pnlHeader.Height), Size = new Size(formWidth, 10), AutoScroll = false };
            Controls.Add(body);

            // ── Vertical divider between the two columns ──
            var divider = new Panel
            {
                BackColor = CardBorder,
                Width = 1,
                Location = new Point(leftX + colWidth + colGap / 2, 24)
            };
            body.Controls.Add(divider);

            // ── Footer — buttons centered, Transfer button color unchanged ──
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(140, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = "⇄  Transfer", Size = new Size(170, 42) };
            ThemeHelper.StyleButton(btnSave, TransferAccent); // unchanged color
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnSave.Location = new Point(buttonsStartX, 15);
            btnCancel.Location = new Point(buttonsStartX + btnSave.Width + 12, 15);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            // ── Column-aware layout helpers ──
            void SectionHeader(string title, int x, int width, ref int y)
            {
                body.Controls.Add(new Panel { BackColor = TransferAccent, Size = new Size(4, 22), Location = new Point(x, y) });
                body.Controls.Add(new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = ThemeHelper.TextPrimary,
                    AutoSize = true,
                    Location = new Point(x + 12, y - 1)
                });
                y += 34;
            }

            void FieldLabel(string label, int x, ref int y, bool required = false)
            {
                body.Controls.Add(new Label
                {
                    Text = required ? $"{label}  *" : label,
                    Location = new Point(x, y),
                    AutoSize = true,
                    Font = ThemeHelper.FontBold,
                    ForeColor = ThemeHelper.TextPrimary
                });
                y += 20;
            }

            void PlaceControl(Control ctrl, int x, int width, ref int y, int height = 30)
            {
                ctrl.Location = new Point(x, y);
                ctrl.Width = width;
                if (ctrl.Height < height) ctrl.Height = height;
                body.Controls.Add(ctrl);
                y += ctrl.Height + 4;
            }

            void Hint(string text, int x, int width, ref int y)
            {
                var font = new Font("Segoe UI", 7.8f, FontStyle.Italic);
                var sz = TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak);
                body.Controls.Add(new Label
                {
                    Text = text,
                    Font = font,
                    ForeColor = MutedText,
                    AutoSize = false,
                    Size = new Size(width, sz.Height + 4),
                    Location = new Point(x, y)
                });
                y += sz.Height + 4 + 10;
            }

            int yLeft = 24;
            int yRight = 24;

            // ══════════════════ LEFT COLUMN — Equipment ══════════════════
            SectionHeader("🖥  Equipment", leftX, colWidth, ref yLeft);

            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by equipment code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            FieldLabel("Find Equipment", leftX, ref yLeft, required: true);
            PlaceControl(txtEquipmentSearch, leftX, colWidth, ref yLeft);
            Hint("Type to search, then pick a suggestion", leftX, colWidth, ref yLeft);
            txtEquipmentSearch.Leave += (s, e) => TryResolveEquipmentFromText();

            _allocationItems = new List<TransferEquipmentOption>
            {
                new TransferEquipmentOption { Id = 0, DisplayText = "-- Select equipment --" }
            };
            _allocationItems.AddRange(_allocations.Select(a => new TransferEquipmentOption
            {
                Id = a.Id,
                DisplayText = $"{a.Equipment?.EquipmentCode} → {a.Employee?.FullName}"
            }));

            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "DisplayText";
            cmbAllocation.ValueMember = "Id";
            cmbAllocation.DataSource = _allocationItems;
            cmbAllocation.SelectedIndexChanged += CmbAllocation_SelectedIndexChanged;
            ThemeHelper.StyleComboBox(cmbAllocation);
            FieldLabel("Currently Allocated As", leftX, ref yLeft, required: true);
            PlaceControl(cmbAllocation, leftX, colWidth, ref yLeft);
            Hint("Only currently approved allocations are listed", leftX, colWidth, ref yLeft);

            lblCurrentDetails = new Label
            {
                Text = "",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(colWidth, 52),
                Location = new Point(leftX, yLeft),
                BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(36, 39, 46) : Color.FromArgb(245, 247, 250)
            };
            body.Controls.Add(lblCurrentDetails);
            yLeft += lblCurrentDetails.Height + 10;

            if (_preselectedAllocationId.HasValue)
            {
                var preAlloc = _allocations.FirstOrDefault(a => a.Id == _preselectedAllocationId.Value);
                if (preAlloc != null)
                {
                    cmbAllocation.SelectedValue = _preselectedAllocationId.Value;
                    txtEquipmentSearch.Text = $"{preAlloc.Equipment?.EquipmentCode} — {preAlloc.Equipment?.Name}";
                    UpdateCurrentDetails(preAlloc);
                }
                txtEquipmentSearch.Enabled = false;
                cmbAllocation.Enabled = false;
            }
            else
            {
                _equipmentSuggest = new EntitySuggestHelper<Allocation>(
                    textBox: txtEquipmentSearch,
                    hostForm: this,
              onSelected: alloc =>
              {
                  UpdateCurrentDetails(alloc);
                  _suppressSync = true;
                  int index = _allocationItems.FindIndex(a => a.Id == alloc.Id);
                  if (index >= 0)
                  {
                      cmbAllocation.SelectedIndex = index;
                  }
                  txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
                  txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                  _suppressSync = false;
              },
                    scoreFn: (alloc, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(alloc.Equipment?.Name, term),
                                FuzzySearchHelper.FuzzyScoreCode(alloc.Equipment?.SerialNumber, term)))),
                    line1Selector: alloc => $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}",
                    line2Selector: alloc => string.IsNullOrWhiteSpace(alloc.Equipment?.SerialNumber)
                        ? $"With: {alloc.Employee?.FullName ?? "—"}"
                        : $"S/N: {alloc.Equipment.SerialNumber} | With: {alloc.Employee?.FullName ?? "—"}");

                _equipmentSuggest.LoadItems(_allocations);
            }

            // ══════════════════ RIGHT COLUMN — Transfer To ══════════════════
            SectionHeader("👤  Transfer To", rightX, colWidth, ref yRight);

            cmbEmployee = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var employeeOptions = new List<EmployeeOption>
{
    new EmployeeOption { Id = null, FullName = "-- Keep current employee --" }
};
            employeeOptions.AddRange(_employees.Where(e => e.IsActive)
                .Select(e => new EmployeeOption { Id = e.Id, FullName = e.FullName }));
            cmbEmployee.DisplayMember = "FullName";
            cmbEmployee.ValueMember = "Id";
            cmbEmployee.DataSource = employeeOptions;
            ThemeHelper.StyleComboBox(cmbEmployee);
            FieldLabel("New Employee", rightX, ref yRight, required: true);
            PlaceControl(cmbEmployee, rightX, colWidth, ref yRight);
            Hint("Who this equipment is being transferred to", rightX, colWidth, ref yRight);

            cmbLocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLocation.DisplayMember = "DisplayName";
            cmbLocation.ValueMember = "Name";
            var locationNameOptions = new List<LocationNameOption>
{
    new LocationNameOption { Name = null, DisplayName = "-- Keep current location --" }
};
            locationNameOptions.AddRange(_locations
                .Select(l => l.LocationName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(n => new LocationNameOption { Name = n ?? "", DisplayName = n ?? "" }));
            cmbLocation.DataSource = locationNameOptions;
            ThemeHelper.StyleComboBox(cmbLocation);
            FieldLabel("New Location", rightX, ref yRight, required: true);
            PlaceControl(cmbLocation, rightX, colWidth, ref yRight);
            Hint("The site or building", rightX, colWidth, ref yRight);

            cmbRoom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoom.DisplayMember = "Display";
            cmbRoom.ValueMember = "Id";
            ThemeHelper.StyleComboBox(cmbRoom);
            FieldLabel("Room", rightX, ref yRight, required: true);
            PlaceControl(cmbRoom, rightX, colWidth, ref yRight);
            Hint("The specific room at that location", rightX, colWidth, ref yRight);

            cmbLocation.SelectedIndexChanged += (s, e) => { if (!_suppressSync) RefreshRoomDropdown(); };
            if (cmbLocation.Items.Count > 0) cmbLocation.SelectedIndex = 0; // placeholder — room stays disabled until a real location is picked

            // ── Row height reconciled, divider sized to match ──
            int columnsBottom = Math.Max(yLeft, yRight);
            divider.Height = columnsBottom - divider.Top - 6;

            int yNote = columnsBottom + 6;
            var lblNote = new Label
            {
                Text = "ℹ  After transferring, you can set a new expected return date from the Allocations screen.",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = MutedText,
                AutoSize = false,
                Size = new Size(contentWidth, 32),
                Location = new Point(margin, yNote)
            };
            int noteH = TextRenderer.MeasureText(lblNote.Text, lblNote.Font, new Size(contentWidth, int.MaxValue), TextFormatFlags.WordBreak).Height;
            lblNote.Height = noteH;
            body.Controls.Add(lblNote);
            yNote += noteH + 8;

            // ── Final sizing — no scrollbar, dialog fits exactly ──
            body.Size = new Size(formWidth, yNote);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            _suppressSync = false;
            if (_preselectedAllocationId.HasValue)
                UpdateCurrentDetails(_allocations.FirstOrDefault(a => a.Id == _preselectedAllocationId.Value));
            RefreshRoomDropdown();

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void CmbAllocation_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressSync) return;
            if (cmbAllocation.SelectedValue == null) return;

            int selectedId = (int)cmbAllocation.SelectedValue;
            if (selectedId == 0) return;

            var alloc = _allocations.FirstOrDefault(a => a.Id == selectedId);
            if (alloc == null) return;

            UpdateCurrentDetails(alloc);

            _suppressSync = true;
            txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
            txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
            _suppressSync = false;
        }

        private void UpdateCurrentDetails(Allocation? alloc)
        {
            if (alloc == null) { lblCurrentDetails.Text = ""; return; }
            string emp = alloc.Employee?.FullName ?? "—";
            string loc = alloc.Location?.LocationName ?? "—";
            string room = TransferForm.FormatRoom(alloc.Location);
            lblCurrentDetails.Text = $"Currently with: {emp}\nLocation: {loc}" + (room != "—" ? $" — {room}" : "");
        }




        private void RefreshRoomDropdown()
        {
            string? selectedName = cmbLocation.SelectedValue as string;

            var rooms = new List<RoomOption>
    {
        new RoomOption { Id = 0, Display = "-- Select a room --" }
    };

            if (selectedName != null)
            {
                rooms.AddRange(_locations
                    .Where(l => l.LocationName == selectedName)
                    .Select(l => new RoomOption { Id = l.Id, Display = RoomOptionDisplay(l) }));
            }

            cmbRoom.DataSource = rooms;
            cmbRoom.Enabled = rooms.Count > 1;   // only real rooms count as "usable"
            if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
        }

        // ── Resolves the typed search text to an allocation and selects it in
        // cmbAllocation, in case the suggestion popup click never fired. ──
        private void TryResolveEquipmentFromText()
        {
            if (cmbAllocation.SelectedValue != null && (int)cmbAllocation.SelectedValue != 0)
                return; // already properly selected, nothing to do

            var typed = txtEquipmentSearch.Text.Trim();
            if (string.IsNullOrEmpty(typed)) return;

            var match = _allocations.FirstOrDefault(a =>
                string.Equals($"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name}", typed,
                    StringComparison.OrdinalIgnoreCase));

            if (match == null) return;

            int index = _allocationItems.FindIndex(a => a.Id == match.Id);
            if (index < 0) return;

            _suppressSync = true;
            cmbAllocation.SelectedIndex = index;
            _suppressSync = false;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            TryResolveEquipmentFromText();

            if (cmbAllocation.SelectedValue == null || (int)cmbAllocation.SelectedValue == 0)
            {
                ShowToast("Please select equipment to transfer.", ToastType.Warning);
                return;
            }

            int allocId = (int)cmbAllocation.SelectedValue;
            var currentAlloc = _allocations.FirstOrDefault(a => a.Id == allocId);
            if (currentAlloc == null) return;

            int? empId = cmbEmployee.SelectedValue is int eid ? eid : (int?)null;
            int? roomId = cmbRoom.Enabled && cmbRoom.SelectedValue is int rid ? rid : (int?)null;

            if (empId == null && roomId == null)
            {
                ShowToast("Select a new employee and/or a new location to transfer to.", ToastType.Warning);
                return;
            }

            OldAllocationId = allocId;
            NewEmployeeId = empId ?? currentAlloc.EmployeeId;   // unchanged if left on "keep current"
            NewLocationId = roomId ?? currentAlloc.LocationId;  // unchanged if left on "keep current"
            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _equipmentSuggest?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
