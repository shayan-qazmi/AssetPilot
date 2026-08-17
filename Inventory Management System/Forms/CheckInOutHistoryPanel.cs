// Forms/CheckInOutHistoryPanel.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Models;
using Inventory_Management_System.Helpers;

namespace Inventory_Management_System.Forms
{
    public class CheckInOutHistoryPanel : Panel
    {
        public event EventHandler? BackRequested;

        private Panel panelTop = null!;
        private Button btnBack = null!;
        private Label lblTitle = null!;
        private TextBox txtSearch = null!;
        private Panel searchWrapper = null!;
        private Button btnExport = null!;

        private DataGridView dgv = null!;
        private Panel panelBottom = null!;
        private ComboBox cmbPageSize = null!;
        private Label lblRowCount = null!;
        private Label lblPageIndicator = null!;
        private Button btnPrevPage = null!;
        private Button btnNextPage = null!;

        private List<Transaction> _all = new();
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private int _totalMatching = 0;

        public CheckInOutHistoryPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeHelper.DashboardBg;
            BuildUI();
        }
        private class HistoryEntry
        {
            public Transaction Tx = null!;
            public bool IsCheckIn;
        }

        private void BuildUI()
        {
            panelTop = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = ThemeHelper.CardBg };
            ThemeHelper.StyleToolbarPanel(panelTop);

            btnBack = new Button { Text = "◀  Back", Size = new Size(96, 38) };
            ThemeHelper.StyleSecondaryButton(btnBack);
            btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);
            panelTop.Controls.Add(btnBack);

            lblTitle = new Label
            {
                Text = "Check In/Out History",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = ThemeHelper.TextPrimary,
                AutoSize = true
            };
            panelTop.Controls.Add(lblTitle);

            txtSearch = new TextBox { PlaceholderText = "Search equipment code, issued to, department…" };
            searchWrapper = RoundedSearchBox.Wrap(txtSearch, new Size(380, 38));
            txtSearch.TextChanged += (s, e) => { _currentPage = 1; Refresh_(); };
            panelTop.Controls.Add(searchWrapper);

            btnExport = new Button { Text = "📤  Export", Size = new Size(110, 38) };
            ThemeHelper.StyleSecondaryButton(btnExport);
            btnExport.Click += BtnExport_Click;
            panelTop.Controls.Add(btnExport);

            void LayoutTop()
            {
                int centerY = panelTop.Height / 2;
                btnExport.Location = new Point(20, centerY - btnExport.Height / 2);
                lblTitle.Location = new Point(btnExport.Right + 20, centerY - lblTitle.PreferredHeight / 2);
                btnBack.Location = new Point(panelTop.ClientSize.Width - btnBack.Width - 20, centerY - btnBack.Height / 2);

                int searchLeft = lblTitle.Right + 32;
                searchWrapper.Width = 380;
                searchWrapper.Location = new Point(searchLeft, centerY - searchWrapper.Height / 2);
            }
            panelTop.Resize += (s, e) => LayoutTop();
            LayoutTop();

            Controls.Add(panelTop);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
            };
            dgv.Columns.Add("EquipmentCode", "Equipment Code");
            dgv.Columns.Add("EquipmentName", "Equipment Name");
            dgv.Columns.Add("Type", "Type");
            dgv.Columns.Add("IssuedTo", "Issued To");
            dgv.Columns.Add("Department", "Department");
            dgv.Columns.Add("Operator", "Operator");
            dgv.Columns.Add("ExpectedReturn", "Expected Return");
            dgv.Columns.Add("Status", "Status");
            dgv.Columns.Add("ConditionBefore", "Condition Before");
            dgv.Columns.Add("ConditionAfter", "Condition After");
            dgv.Columns.Add("Date", "Date");
            dgv.Columns.Add("Timestamp", "Timestamp");
            ThemeHelper.StyleDataGridView(dgv);
            Controls.Add(dgv);
            dgv.BringToFront();


            panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = ThemeHelper.IsDarkMode ? Color.FromArgb(22, 24, 29) : Color.FromArgb(249, 250, 251) };
            lblRowCount = new Label { Location = new Point(16, 12), AutoSize = true, Font = ThemeHelper.FontSmall, ForeColor = ThemeHelper.TextSecondary };
            panelBottom.Controls.Add(lblRowCount);

            cmbPageSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = "25";
            ThemeHelper.StyleComboBox(cmbPageSize);
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
                {
                    _pageSize = size;
                    _currentPage = 1;
                    Refresh_();
                }
            };

            btnPrevPage = new Button { Text = "◀ Prev", Size = new Size(80, 28) };
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            btnPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; Refresh_(); } };

            lblPageIndicator = new Label { AutoSize = true, Font = ThemeHelper.FontSmall, ForeColor = ThemeHelper.TextSecondary };

            btnNextPage = new Button { Text = "Next ▶", Size = new Size(80, 28) };
            ThemeHelper.StyleSecondaryButton(btnNextPage);
            btnNextPage.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; Refresh_(); } };

            panelBottom.Controls.Add(cmbPageSize);
            panelBottom.Controls.Add(btnPrevPage);
            panelBottom.Controls.Add(lblPageIndicator);
            panelBottom.Controls.Add(btnNextPage);

            panelBottom.Resize += (s, e) => LayoutBottomRight();
            LayoutBottomRight();

            Controls.Add(panelBottom);
        }

        private void LayoutBottomRight()
        {
            int right = panelBottom.Width - 16;
            btnNextPage.Location = new Point(right - btnNextPage.Width, 8);
            right -= btnNextPage.Width + 10;
            lblPageIndicator.Location = new Point(right - lblPageIndicator.Width, 14);
            right -= lblPageIndicator.Width + 10;
            btnPrevPage.Location = new Point(right - btnPrevPage.Width, 8);
            right -= btnPrevPage.Width + 10;
            cmbPageSize.Location = new Point(right - cmbPageSize.Width, 8);
        }


        public void RefreshData() => LoadData();

        private void LoadData()
        {
            try
            {
                using var db = new InventoryDbContext();
                _all = db.Transactions
                    .Include(t => t.Equipment)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(FindForm(), "Could not load history: " + ex.Message, ToastType.Error);
                _all = new List<Transaction>();
            }
            Refresh_();
        }

        private void Refresh_()
        {
            dgv.Rows.Clear();
            string filter = txtSearch.Text.Trim();

            List<Transaction> matched;
            if (string.IsNullOrWhiteSpace(filter))
            {
                matched = _all;
            }
            else
            {
                var codeMatches = _all.Where(t =>
                    (!string.IsNullOrEmpty(t.Equipment?.EquipmentCode) && t.Equipment!.EquipmentCode.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(t.Equipment?.QrCode) && t.Equipment!.QrCode.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                matched = codeMatches.Count > 0
                    ? codeMatches
                    : _all.Where(t =>
                        FuzzySearchHelper.IsFuzzyMatch(t.Equipment?.Name, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(t.IssuedTo, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(t.Department, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(t.OperatorName, filter) ||
                        FuzzySearchHelper.IsFuzzyMatch(t.Status, filter))
                        .OrderByDescending(t => new[]
                        {
                    FuzzySearchHelper.FuzzyScore(t.Equipment?.Name, filter),
                    FuzzySearchHelper.FuzzyScore(t.IssuedTo, filter),
                    FuzzySearchHelper.FuzzyScore(t.Department, filter),
                    FuzzySearchHelper.FuzzyScore(t.OperatorName, filter)
                        }.Max())
                        .ToList();
            }

            // Expand each transaction into up to two display entries: the original
            // check-out, and (if it's been returned) a synthetic check-in entry.
            var entries = new List<HistoryEntry>();
            foreach (var t in matched)
            {
                entries.Add(new HistoryEntry { Tx = t, IsCheckIn = false });
                if (t.ActualReturn.HasValue)
                    entries.Add(new HistoryEntry { Tx = t, IsCheckIn = true });
            }
            entries = entries
                .OrderByDescending(en => en.IsCheckIn
                    ? (en.Tx.ReturnProcessedAt ?? en.Tx.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue))
                    : en.Tx.TransactionDate)
                .ToList();

            _totalMatching = entries.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatching / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            var page = entries.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

            foreach (var en in page)
            {
                var t = en.Tx;
                string typeLabel = en.IsCheckIn ? "Check-In" : (t.TransactionType ?? "—");
                string dateStr = en.IsCheckIn
                    ? t.ActualReturn!.Value.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd")
                    : t.TransactionDate.ToString("yyyy-MM-dd HH:mm");
                string timestampStr = en.IsCheckIn ? PktTimeHelper.Format(t.ReturnProcessedAt) : "—";
                string statusStr = en.IsCheckIn ? "Completed" : (t.Status ?? "—");

                int idx = dgv.Rows.Add(
                    t.Equipment?.EquipmentCode ?? "—",
                    t.Equipment?.Name ?? "—",
                    typeLabel,
                    t.IssuedTo ?? "—",
                    t.Department ?? "—",
                    t.OperatorName ?? "—",
                    t.ExpectedReturn?.ToString("yyyy-MM-dd") ?? "—",
                    statusStr,
                    t.ConditionBefore ?? "—",
                    t.ConditionAfter ?? "—",
                    dateStr,
                    timestampStr
                );

                var typeCell = dgv.Rows[idx].Cells["Type"];
                typeCell.Style.Font = ThemeHelper.FontBold;
                typeCell.Style.ForeColor = typeLabel switch
                {
                    "Check-Out" => Color.FromArgb(25, 118, 210),
                    "Check-In" => ThemeHelper.Success,
                    _ => ThemeHelper.TextPrimary
                };

                var statusCell = dgv.Rows[idx].Cells["Status"];
                statusCell.Style.Font = ThemeHelper.FontBold;
                statusCell.Style.ForeColor = statusStr switch
                {
                    "Pending" => ThemeHelper.Warning,
                    "Completed" => ThemeHelper.Success,
                    "Overdue" => ThemeHelper.Danger,
                    _ => ThemeHelper.TextPrimary
                };
            }

            lblRowCount.Text = $"Showing {page.Count} of {_totalMatching} records";
            lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
            btnPrevPage.Enabled = _currentPage > 1;
            btnNextPage.Enabled = _currentPage < _totalPages;
            LayoutBottomRight(); // re-flow now that lblPageIndicator's width has changed
        }   

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV files|*.csv",
                    FileName = $"CheckInOutHistory_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("Equipment Code,Equipment Name,Type,Issued To,Department,Operator,Expected Return,Status,Condition Before,Condition After,Date,Timestamp");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    string E(object? v) => $"\"{v?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                    sb.AppendLine(string.Join(",",
                        E(row.Cells["EquipmentCode"].Value), E(row.Cells["EquipmentName"].Value),
                        E(row.Cells["Type"].Value), E(row.Cells["IssuedTo"].Value),
                        E(row.Cells["Department"].Value), E(row.Cells["Operator"].Value),
                        E(row.Cells["ExpectedReturn"].Value), E(row.Cells["Status"].Value),
                        E(row.Cells["ConditionBefore"].Value), E(row.Cells["ConditionAfter"].Value),
                        E(row.Cells["Date"].Value), E(row.Cells["Timestamp"].Value)));
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(FindForm(), "Exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(FindForm(), "Export failed: " + ex.Message, ToastType.Error);
            }
        }
    }
}