// Forms/ReturnsForm.cs
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Inventory_Management_System.Forms
{
    public partial class ReturnsForm : Form
    {
        private readonly AllocationService _allocationService = new();
        private readonly AuditService _audit = new();
        private List<Allocation> _allReturns = new();
        private EntitySuggestHelper<Allocation>? _suggest;

        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatchingCount = 0;

        // ── Pakistan Standard Time (used to render the Timestamp column) ──
        private static readonly TimeZoneInfo PktZone = ResolvePakistanTimeZone();

        private static TimeZoneInfo ResolvePakistanTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); } // Windows ID
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); } // Linux/IANA ID
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "PKT_Fallback", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT");
                }
            }
        }

        /// Formats a UTC timestamp as 12-hour Pakistan local time + date, e.g. "13 Aug 2026, 03:45 PM".
        private static string FormatPkt(DateTime? utc)
        {
            if (!utc.HasValue) return "—";
            var asUtc = utc.Value.Kind == DateTimeKind.Utc
                ? utc.Value
                : DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
            var pkt = TimeZoneInfo.ConvertTimeFromUtc(asUtc, PktZone);
            return pkt.ToString("dd MMM yyyy, hh:mm tt");
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x200;

        public ReturnsForm()
        {
            InitializeComponent();
            SetupTimestampColumn();
            SetupPageSizeOptions();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            ApplyTheme();
            LoadReturns();
            SetupSuggestions();

            this.Shown += (s, e) =>
            {
                dgvReturns.ClearSelection();
                dgvReturns.CurrentCell = null;
            };
        }

        /// Inserts a "Timestamp" column right after "Return Date" if it doesn't already exist.
        /// Done in code so we don't have to hand-edit the Designer file.
        private void SetupTimestampColumn()
        {
            if (dgvReturns.Columns.Contains("Timestamp")) return;

            var col = new DataGridViewTextBoxColumn
            {
                Name = "Timestamp",
                HeaderText = "Timestamp",
                Width = 175,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            int insertAt = dgvReturns.Columns.Contains("ReturnDate")
                ? dgvReturns.Columns["ReturnDate"].Index + 1
                : dgvReturns.Columns.Count;

            dgvReturns.Columns.Insert(insertAt, col);
        }

        /// Expands the page-size options to include 250 and 500.
        private void SetupPageSizeOptions()
        {
            string current = cmbPageSize.SelectedItem?.ToString() ?? "25";
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500" });
            cmbPageSize.SelectedItem = cmbPageSize.Items.Contains(current) ? current : "25";
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleButton(btnDelete, Color.FromArgb(99, 102, 241)); // indigo — distinct from Edit/Process/Refresh
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            ThemeHelper.StyleDataGridView(dgvReturns);
            panelBottom.BackColor = Color.FromArgb(249, 250, 251);
            lblRowCount.ForeColor = ThemeHelper.TextSecondary;
            lblRowCount.Font = ThemeHelper.FontSmall;
            foreach (var btn in new[] { btnAdd, btnEdit, btnDelete, btnRefresh, btnExport })
            {
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.TextImageRelation = TextImageRelation.Overlay;
                btn.Padding = new Padding(0);
            }
        }

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

        /// Wraps txtSearch (a live FlowLayoutPanel child) in the same rounded,
        /// green-focus-ring style used by MakeTextBox in Settings. Carries the
        /// original size/margin over so flow spacing next to the toolbar
        /// buttons doesn't collapse.
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

        // ── Live fuzzy-suggestion dropdown (same UX as QR Scanner / Check-In-Out) ──
        private void SetupSuggestions()
        {
            try
            {
                _suggest = new EntitySuggestHelper<Allocation>(
       textBox: txtSearch,
       hostForm: this,
       onSelected: a =>
       {
           var row = _allReturnRows.FirstOrDefault(r => r.AllocationRef != null && r.AllocationRef.Id == a.Id);
           if (row != null) ShowOnlyReturnRow(row);
       },
                       scoreFn: (a, term) => Math.Max(
                        FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.EquipmentCode, term),
                        Math.Max(
                            FuzzySearchHelper.FuzzyScoreCode(a.Equipment?.QrCode, term),
                            Math.Max(
                                FuzzySearchHelper.FuzzyScore(a.Equipment?.Name, term),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(a.Employee?.FullName, term),
                                    FuzzySearchHelper.FuzzyScore(a.ActualReturnDate?.ToString("yyyy-MM-dd"), term))))),
                    line1Selector: a => $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name}",
                    line2Selector: a => $"{a.Employee?.FullName ?? "—"} • {a.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—"}");
                _suggest.LoadItems(_allReturns);
            }
            catch { /* non-critical UI enhancement, fail silently */ }
        }

        private void SelectReturnRow(int id)
        {
            foreach (DataGridViewRow row in dgvReturns.Rows)
            {
                if (row.Cells["ReturnID"].Value != null &&
                    Convert.ToInt32(row.Cells["ReturnID"].Value) == id)
                {
                    dgvReturns.ClearSelection();
                    row.Selected = true;
                    dgvReturns.CurrentCell = row.Cells[0];
                    dgvReturns.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        private void ShowOnlyReturnRow(ReturnRow item)
        {
            dgvReturns.Rows.Clear();

            int rowIdx = dgvReturns.Rows.Add(
                            item.Id,
                            item.QrCode,
                            item.EquipmentCode,
                            item.EquipmentName,
                            item.Employee,
                            item.Location,
                            item.ReturnDate,
                            FormatPkt(item.Timestamp),
                            item.Condition,
                            item.Source,
                            item.IsOverdue ? "Yes" : "No"
                        );

            if (item.DurationDays > 30)
                dgvReturns.Rows[rowIdx].Cells["ReturnDate"].Style.ForeColor = ThemeHelper.Warning;

            var overdueCell = dgvReturns.Rows[rowIdx].Cells["Overdue"];
            overdueCell.Style.Font = ThemeHelper.FontBold;
            overdueCell.Style.ForeColor = item.IsOverdue ? ThemeHelper.Danger : ThemeHelper.TextSecondary;

            var sourceCell = dgvReturns.Rows[rowIdx].Cells["Source"];
            sourceCell.Style.ForeColor = item.Source == "Check In/Out"
                ? Color.FromArgb(25, 118, 210)
                : Color.FromArgb(107, 114, 128);
            sourceCell.Style.Font = ThemeHelper.FontBold;

            dgvReturns.ClearSelection();
            dgvReturns.Rows[0].Selected = true;

            lblRowCount.Text = "Showing 1 of 1 returns";
            lblPageIndicator.Text = "Page 1 of 1";
            btnPrevPage.Enabled = false;
            btnNextPage.Enabled = false;
        }

        // ── Display-only view-model row combining Allocation returns + Check In/Out completions
        private class ReturnRow
        {
            public int Id { get; set; }
            public string Source { get; set; } = "Allocation"; // "Allocation" | "Check In/Out"
            public string QrCode { get; set; } = "—";
            public string EquipmentCode { get; set; } = "—";
            public string EquipmentName { get; set; } = "—";
            public string Employee { get; set; } = "—";
            public string Location { get; set; } = "—";
            public DateTime SortDate { get; set; }
            public string ReturnDate { get; set; } = "—";
            public DateTime? Timestamp { get; set; } // real processed-at moment (UTC), when available
            public string Condition { get; set; } = "—";
            public int DurationDays { get; set; }
            public bool IsOverdue { get; set; }
            public Allocation? AllocationRef { get; set; } // non-null only for Allocation-sourced rows
        }

        private List<ReturnRow> _allReturnRows = new();

        internal static string FormatLocation(Location? loc)
        {
            if (loc == null || string.IsNullOrWhiteSpace(loc.LocationName)) return "—";
            bool hasNum = !string.IsNullOrWhiteSpace(loc.RoomNumber);
            bool hasName = !string.IsNullOrWhiteSpace(loc.RoomName);
            if (!hasNum && !hasName) return loc.LocationName;
            string room = hasNum ? $"Room {loc.RoomNumber}" : "";
            if (hasName) room = string.IsNullOrEmpty(room) ? loc.RoomName! : $"{room} ({loc.RoomName})";
            return $"{loc.LocationName} - {room}";
        }

        private void LoadReturns()
        {
            try
            {
                using var db = new InventoryDbContext();

                _allReturns = db.Allocations
                    .Where(a => a.Status == "returned")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .Include(a => a.Location)
                    .ToList()
                    // Ordered in-memory (not translated to SQL) so we can fall back to
                    // ActualReturnDate for older rows that predate ReturnProcessedAt.
                    .OrderByDescending(a => a.ReturnProcessedAt)
                    .ThenByDescending(a => a.ActualReturnDate)
                    .ThenByDescending(a => a.Id)
                    .ToList();

                var allocRows = _allReturns.Select(item =>
                {
                    var returnDate = item.ActualReturnDate.HasValue
                        ? item.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.Today;
                    return new ReturnRow
                    {
                        Id = item.Id,
                        Source = "Allocation",
                        QrCode = string.IsNullOrWhiteSpace(item.Equipment?.QrCode) ? "—" : item.Equipment!.QrCode,
                        EquipmentCode = item.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = item.Equipment?.Name ?? "—",
                        Employee = item.Employee?.FullName ?? "—",
                        Location = FormatLocation(item.Location),
                        SortDate = returnDate,
                        ReturnDate = item.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                        Timestamp = item.ReturnProcessedAt,
                        Condition = item.Equipment?.ConditionGrade ?? "—",
                        DurationDays = (returnDate - item.AllocationDate).Days,
                        IsOverdue = item.ExpectedReturnDate.HasValue && item.ActualReturnDate.HasValue &&
                                    item.ActualReturnDate.Value > item.ExpectedReturnDate.Value,
                        AllocationRef = item
                    };
                }).ToList();

                var completedCheckIns = db.Transactions
     .Include(t => t.Equipment)
     .Where(t => t.Status == "Completed" && t.ActualReturn.HasValue && t.TransactionType == "Check-Out")
     .ToList();

                var checkInRows = completedCheckIns.Select(t =>
                {
                    var returnDate = t.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue);
                    return new ReturnRow
                    {
                        Id = -t.TransactionId,
                        Source = "Check In/Out",
                        QrCode = string.IsNullOrWhiteSpace(t.Equipment?.QrCode) ? "—" : t.Equipment!.QrCode,
                        EquipmentCode = t.Equipment?.EquipmentCode ?? "—",
                        EquipmentName = t.Equipment?.Name ?? "—",
                        Employee = string.IsNullOrWhiteSpace(t.IssuedTo) ? "—" : t.IssuedTo,
                        Location = string.IsNullOrWhiteSpace(t.Department) ? "—" : t.Department,
                        SortDate = returnDate,
                        ReturnDate = t.ActualReturn?.ToString("yyyy-MM-dd") ?? "—",
                        // NOTE: requires a ReturnProcessedAt (DateTime?) column added to Transaction,
                        // stamped when the check-in/out record is marked Completed. Until that's
                        // wired up, these rows will show "—" here (same as before this change).
                        Timestamp = t.ReturnProcessedAt,
                        Condition = t.ConditionAfter ?? "—",
                        DurationDays = (returnDate - t.TransactionDate).Days,
                        IsOverdue = t.ExpectedReturn.HasValue && t.ActualReturn.HasValue &&
                                    t.ActualReturn.Value > t.ExpectedReturn.Value,
                        AllocationRef = null
                    };
                }).ToList();

                // Latest activity first: real timestamp wins when we have one, falling back to
                // the return date, and finally the row id, so ties never look "shuffled".
                _allReturnRows = allocRows.Concat(checkInRows)
                    .OrderByDescending(r => r.Timestamp ?? DateTime.MinValue)
                    .ThenByDescending(r => r.SortDate)
                    .ThenByDescending(r => Math.Abs(r.Id))
                    .ToList();

                _suggest?.LoadItems(_allReturns);   // suggestion dropdown stays Allocation-only, unchanged
                RefreshGrid();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading returns: " + ex.Message, ToastType.Error);

            }
        }

        private void RefreshGrid(string filter = "")
        {
            dgvReturns.Rows.Clear();

            List<ReturnRow> matched;
            if (string.IsNullOrWhiteSpace(filter))
            {
                matched = _allReturnRows;
            }
            else
            {
                // Scanned/typed codes need an exact substring match — fuzzy matching
                // lets "QR001" incorrectly match QR002, QR005, etc.
                var codeMatches = _allReturnRows.Where(r =>
                    (!string.IsNullOrEmpty(r.QrCode) && r.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.EquipmentCode) && r.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                matched = codeMatches.Count > 0
                    ? codeMatches
                    : _allReturnRows.Where(r =>
                        FuzzySearchHelper.IsFuzzyMatch(r.EquipmentName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Employee, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.ReturnDate, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(r.Source, filter))
                        .OrderByDescending(r => Math.Max(
                            FuzzySearchHelper.FuzzyScore(r.EquipmentName, filter),
                            FuzzySearchHelper.FuzzyScore(r.Employee, filter)))
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
                int rowIdx = dgvReturns.Rows.Add(
                          item.Id, item.QrCode, item.EquipmentCode, item.EquipmentName,
                          item.Employee, item.Location, item.ReturnDate, FormatPkt(item.Timestamp),
                          item.Condition, item.Source,
                          item.IsOverdue ? "Yes" : "No");

                if (item.DurationDays > 30)
                    dgvReturns.Rows[rowIdx].Cells["ReturnDate"].Style.ForeColor = ThemeHelper.Warning;

                var overdueCell = dgvReturns.Rows[rowIdx].Cells["Overdue"];
                overdueCell.Style.Font = ThemeHelper.FontBold;
                overdueCell.Style.ForeColor = item.IsOverdue ? ThemeHelper.Danger : ThemeHelper.TextSecondary;

                var sourceCell = dgvReturns.Rows[rowIdx].Cells["Source"];
                sourceCell.Style.ForeColor = item.Source == "Check In/Out"
                    ? Color.FromArgb(25, 118, 210)
                    : Color.FromArgb(107, 114, 128);
                sourceCell.Style.Font = ThemeHelper.FontBold;
            }

            lblRowCount.Text = $"Showing {pageItems.Count} of {_totalMatchingCount} returns";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
        }

        private Allocation? GetSelectedReturn()
        {
            if (dgvReturns.SelectedRows.Count == 0) return null;
            if (!int.TryParse(dgvReturns.SelectedRows[0].Cells["ReturnID"].Value?.ToString(), out int id)) return null;
            if (id < 0) return null; // Check In/Out-sourced row — not editable/deletable here
            return _allReturns.FirstOrDefault(a => a.Id == id);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                using var db = new InventoryDbContext();
                var activeAllocations = db.Allocations
                    .Where(a => a.Status == "approved")
                    .Include(a => a.Equipment)
                    .Include(a => a.Employee)
                    .ToList();

                if (activeAllocations.Count == 0)
                {
                    ToastHelper.Show(this, "There are no active approved allocations to return.", ToastType.Info);

                    return;
                }

                using var frm = new ProcessReturnForm(activeAllocations, isEdit: false);
                if (frm.ShowDialog() != DialogResult.OK) return;

                var alloc = db.Allocations
                    .Include(a => a.Equipment)
                    .FirstOrDefault(a => a.Id == frm.AllocationId);

                if (alloc == null)
                {
                    ToastHelper.Show(this, "Allocation not found.", ToastType.Error);
                    return;
                }

                alloc.Status = "returned";
                alloc.ActualReturnDate = frm.ReturnDate;
                alloc.ReturnProcessedAt = DateTime.UtcNow; // real "processed at" moment for sorting/display
                if (alloc.Equipment != null)
                {
                    alloc.Equipment.CurrentStatus = "in_store";
                    alloc.Equipment.ConditionGrade = frm.ConditionGrade;
                }
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned, "allocated", "in_store");
                ToastHelper.Show(this, $"Return processed for '{alloc.Equipment?.EquipmentCode}'.", ToastType.Success);
                LoadReturns();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Failed to process return: " + ex.Message, ToastType.Error);

            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null)
            {
                ToastHelper.Show(this, "Please select a return to edit.", ToastType.Warning);
                return;
            }

            using var frm = new ProcessReturnForm(new List<Allocation> { ret }, isEdit: true, existing: ret);
            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var db = new InventoryDbContext();
                var alloc = db.Allocations.Include(a => a.Equipment).FirstOrDefault(a => a.Id == ret.Id);
                if (alloc == null)
                {
                    ToastHelper.Show(this, "Return record not found.", ToastType.Error);
                    return;
                }
                alloc.ActualReturnDate = frm.ReturnDate;
                alloc.ReturnProcessedAt = DateTime.UtcNow; // bump to reflect the edit as the latest activity
                if (alloc.Equipment != null) alloc.Equipment.ConditionGrade = frm.ConditionGrade;
                db.SaveChanges();
                ToastHelper.Show(this, "Return updated successfully.", ToastType.Success);
                LoadReturns();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Failed to update return: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var ret = GetSelectedReturn();
            if (ret == null)
            {
                ToastHelper.Show(this, "Please select a return to delete.", ToastType.Warning);
                return;
            }

            try
            {
                using var db = new InventoryDbContext();
                var alloc = db.Allocations.Include(a => a.Equipment).FirstOrDefault(a => a.Id == ret.Id);
                if (alloc == null)
                {
                    ToastHelper.Show(this, "Return record not found.", ToastType.Error);
                    return;
                }

                var activeAlloc = db.Allocations
                    .Include(a => a.Employee)
                    .FirstOrDefault(a => a.EquipmentId == alloc.EquipmentId
                                       && a.Status == "approved"
                                       && a.Id != alloc.Id);

                if (activeAlloc != null)
                {
                    ToastHelper.Show(this,
                        $"Can't undo — '{alloc.Equipment?.EquipmentCode}' is currently allocated to " +
                        $"{activeAlloc.Employee?.FullName ?? "someone"}. Undo that allocation first.",
                        ToastType.Warning);
                    return;
                }

                if (MessageBox.Show(
                    $"This will undo the return for '{ret.Equipment?.EquipmentCode} — {ret.Equipment?.Name}', reverting it back to an active allocation. Continue?",
                    "Undo Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                string oldValue = $"{alloc.Equipment?.EquipmentCode} - returned";
                alloc.Status = "approved";
                alloc.ActualReturnDate = null;
                alloc.ReturnProcessedAt = null; // no longer a "return" — clear the processed-at stamp
                if (alloc.Equipment != null) alloc.Equipment.CurrentStatus = "allocated";
                db.SaveChanges();
                _audit.Log(alloc.EquipmentId, AuditService.ActionAllocationReturned,
                    oldValue, $"{alloc.Equipment?.EquipmentCode} - approved (return undone)");
                ToastHelper.Show(this, $"Return undone — '{alloc.Equipment?.EquipmentCode}' is active again.", ToastType.Success);
                LoadReturns();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Failed to undo return: " + ex.Message, ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            LoadReturns();
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
                    FileName = $"Returns_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Return ID,QR Code,Equipment Code,Equipment Name,Returned By,Location,Return Date,Timestamp,Condition,Overdue");
                foreach (DataGridViewRow row in dgvReturns.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["ReturnID"].Value), E(row.Cells["QRCode"].Value),
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["Employee"].Value), E(row.Cells["Location"].Value),
                        E(row.Cells["ReturnDate"].Value), E(row.Cells["Timestamp"].Value),
                        E(row.Cells["Condition"].Value), E(row.Cells["Overdue"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(this, "Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Export failed: " + ex.Message, ToastType.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _suggest?.Dispose();
            base.OnFormClosed(e);
        }
    }

    // ── Process Return Dialog ───────────────────────────────────────────
    public class ProcessReturnForm : Form
    {
        public int AllocationId { get; private set; }
        public DateOnly ReturnDate { get; private set; }
        public string ConditionGrade { get; private set; } = "Good";

        private readonly List<Allocation> _allocations;
        private readonly bool _isEdit;

        private TextBox txtEquipmentSearch = null!;
        private ComboBox cmbAllocation = null!;
        private Label lblAllocDetails = null!;
        private DateTimePicker dtpReturnDate = null!;
        private ComboBox cmbCondition = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private EntitySuggestHelper<Allocation>? _equipmentSuggest;
        private bool _suppressSync = false;

        private static readonly Color CardBorder = Color.FromArgb(224, 224, 224);
        private static readonly Color MutedText = Color.FromArgb(110, 110, 120);

        private static readonly string[] Conditions = { "New", "Good", "Fair", "Poor", "Unserviceable" };

        public ProcessReturnForm(List<Allocation> allocations, bool isEdit, Allocation? existing = null)
        {
            _allocations = allocations;
            _isEdit = isEdit;
            BuildUI(existing);
        }

        private void BuildUI(Allocation? existing)
        {
            Text = _isEdit ? "Edit Return" : "Process Return";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScroll = false;
            Font = new Font("Segoe UI", 9F);
            ThemeHelper.ApplyFormBackground(this);

            const int formWidth = 460;
            const int margin = 28;
            int contentWidth = formWidth - margin * 2;

            // ── Header ──
            var pnlHeader = new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 10), BackColor = ThemeHelper.Header };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = _isEdit ? "↩  Edit Return" : "↩  Process Return",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(margin, 18)
            };
            pnlHeader.Controls.Add(lblTitle);

            var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            string subtitleText = _isEdit
                ? "Update the return date or condition for this record"
                : "Mark an allocated item as returned and log its condition";
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

            // ── Footer — buttons centered ──
            const int footerHeight = 72;
            var pnlFooter = new Panel { Size = new Size(formWidth, footerHeight), BackColor = Color.FromArgb(249, 250, 251) };
            pnlFooter.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(formWidth, 1), BackColor = CardBorder });

            btnCancel = new Button { Text = "Cancel", Size = new Size(140, 42) };
            ThemeHelper.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnSave = new Button { Text = _isEdit ? "✓  Save" : "✓  Process Return", Size = new Size(_isEdit ? 150 : 210, 42) };
            ThemeHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;

            int totalButtonsWidth = btnCancel.Width + 12 + btnSave.Width;
            int buttonsStartX = (formWidth - totalButtonsWidth) / 2;
            btnCancel.Location = new Point(buttonsStartX, 15);
            btnSave.Location = new Point(buttonsStartX + btnCancel.Width + 12, 15);

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSave);
            Controls.Add(pnlFooter);

            int y = 24;

            void SectionHeader(string title)
            {
                body.Controls.Add(new Panel { BackColor = ThemeHelper.Header, Size = new Size(4, 22), Location = new Point(margin, y) });
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

            SectionHeader("↩  Return Details");

            // ── Search box ──
            txtEquipmentSearch = new TextBox { PlaceholderText = "Search by equipment code, name, or serial…" };
            ThemeHelper.StyleTextBox(txtEquipmentSearch);
            FieldLabel("Find Equipment", required: !_isEdit);
            PlaceControl(txtEquipmentSearch);
            Hint("Type to search, then pick a suggestion");
            txtEquipmentSearch.Leave += (s, e) => TryResolveEquipmentFromText();

            // ── Allocation dropdown ──
            cmbAllocation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAllocation.DisplayMember = "AllocationDisplay";
            cmbAllocation.ValueMember = "Id";
            cmbAllocation.DataSource = _allocations.Select(a => new
            {
                a.Id,
                AllocationDisplay = $"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name} → {a.Employee?.FullName}"
            }).ToList<object>();
            cmbAllocation.SelectedIndexChanged += CmbAllocation_SelectedIndexChanged;
            ThemeHelper.StyleComboBox(cmbAllocation);
            FieldLabel(_isEdit ? "Allocation" : "Select Allocation", required: false);
            PlaceControl(cmbAllocation);
            Hint(_isEdit ? "This record cannot be changed" : "Only approved allocations are listed");

            lblAllocDetails = new Label
            {
                Text = "",
                Font = ThemeHelper.FontBold,
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = false,
                Size = new Size(contentWidth, 36),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            PlaceControl(lblAllocDetails, height: 36);

            if (_isEdit && existing != null)
            {
                _suppressSync = true;
                cmbAllocation.SelectedValue = existing.Id;
                txtEquipmentSearch.Text = $"{existing.Equipment?.EquipmentCode} — {existing.Equipment?.Name}";
                _suppressSync = false;
                cmbAllocation.Enabled = false;
                txtEquipmentSearch.Enabled = false;
                UpdateAllocDetails(existing);
            }
            else
            {
                // Nothing selected until the person actually picks something
                cmbAllocation.SelectedIndex = -1;

                _equipmentSuggest = new EntitySuggestHelper<Allocation>(
                    textBox: txtEquipmentSearch,
                    hostForm: this,
                 onSelected: alloc =>
                 {
                     _suppressSync = true;
                     cmbAllocation.SelectedValue = alloc.Id;
                     txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
                     txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
                     _suppressSync = false;
                     UpdateAllocDetails(alloc);
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

            dtpReturnDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Long,
                ShowUpDown = false,
                MaxDate = DateTime.Today,
                Value = existing?.ActualReturnDate.HasValue == true
                    ? existing.ActualReturnDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today
            };
            FieldLabel("Return Date", required: true);
            PlaceControl(dtpReturnDate);
            Hint("The date this equipment was actually returned");

            cmbCondition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCondition.Items.AddRange(Conditions);
            cmbCondition.SelectedItem = existing?.Equipment?.ConditionGrade ?? "Good";
            if (cmbCondition.SelectedIndex < 0) cmbCondition.SelectedIndex = 1;
            ThemeHelper.StyleComboBox(cmbCondition);
            FieldLabel("Equipment Condition on Return", required: true);
            PlaceControl(cmbCondition);
            Hint("How the equipment looked or functioned at the time of return");

            y += 4;

            body.Size = new Size(formWidth, y);
            int totalHeight = pnlHeader.Height + body.Height + footerHeight;
            ClientSize = new Size(formWidth, totalHeight);
            pnlFooter.Location = new Point(0, ClientSize.Height - footerHeight);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void CmbAllocation_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressSync) return;
            if (cmbAllocation.SelectedValue == null) return;

            int selectedId = (int)cmbAllocation.SelectedValue;
            var alloc = _allocations.FirstOrDefault(a => a.Id == selectedId);
            if (alloc == null) return;

            _suppressSync = true;
            txtEquipmentSearch.Text = $"{alloc.Equipment?.EquipmentCode} — {alloc.Equipment?.Name}";
            txtEquipmentSearch.SelectionStart = txtEquipmentSearch.Text.Length;
            _suppressSync = false;
            UpdateAllocDetails(alloc);
        }

        private void UpdateAllocDetails(Allocation? alloc)
        {
            if (alloc == null) { lblAllocDetails.Text = ""; return; }
            string emp = alloc.Employee?.FullName ?? "—";
            string loc = ReturnsForm.FormatLocation(alloc.Location);
            lblAllocDetails.Text = $"With: {emp}\nLocation: {loc}";
        }

        // ── Resolves typed search text to an allocation, in case the
        // suggestion popup click never fired. ──
        private void TryResolveEquipmentFromText()
        {
            if (cmbAllocation.SelectedValue != null) return; // already selected

            var typed = txtEquipmentSearch.Text.Trim();
            if (string.IsNullOrEmpty(typed)) return;

            var match = _allocations.FirstOrDefault(a =>
                string.Equals($"{a.Equipment?.EquipmentCode} — {a.Equipment?.Name}", typed,
                    StringComparison.OrdinalIgnoreCase));
            if (match == null) return;

            _suppressSync = true;
            cmbAllocation.SelectedValue = match.Id;
            _suppressSync = false;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            TryResolveEquipmentFromText(); // last-chance resolve before validating

            if (cmbAllocation.SelectedValue == null)
            {
                ToastHelper.Show(this, "Please select an allocation.", ToastType.Warning);
                return;
            }
            if (cmbCondition.SelectedItem == null)
            {
                ToastHelper.Show(this, "Please select a condition grade.", ToastType.Warning);
                return;
            }
            AllocationId = (int)cmbAllocation.SelectedValue;
            ReturnDate = DateOnly.FromDateTime(dtpReturnDate.Value);
            ConditionGrade = cmbCondition.SelectedItem.ToString()!;
            DialogResult = DialogResult.OK;
        }

    }
}