// Forms/EmployeesForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Inventory_Management_System.Helpers;
using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System.Drawing.Drawing2D;

namespace Inventory_Management_System.Forms
{
    public partial class EmployeesForm : Form, IThemableForm
    {
        private readonly EmployeeService _employeeService;

        // ── Pagination state ─────────────────────────────────────────────
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;

        // ── Gmail-style checkbox header controls ──
        private Panel _chkHeaderPanel = null!;
        private CheckBox _chkHeaderBox = null!;
        private bool _suppressHeaderSync;

        private string? _pendingSelectCode;
        private bool _hasPendingSelection = false;

        public EmployeesForm()
        {
            InitializeComponent();
            flowToolbar.SizeChanged += (s, e) => panelTop.Height = flowToolbar.Height;
            _employeeService = new EmployeeService();
            SetupPageSizeOptions();
            ApplyTheme();
            SetupCheckboxSelectHeader();
            dgvEmployees.CellDoubleClick += DgvEmployees_CellDoubleClick;
            LoadEmployees();

            this.Shown += (s, e) =>
            {
                if (!_hasPendingSelection)
                {
                    dgvEmployees.ClearSelection();
                    dgvEmployees.CurrentCell = null;
                }
            };
        }

        private void SetupPageSizeOptions()
        {
            string current = cmbPageSize.SelectedItem?.ToString() ?? "25";
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100", "250", "500", "1000" });
            cmbPageSize.SelectedItem = cmbPageSize.Items.Contains(current) ? current : "25";
        }

        public EmployeesForm(string employeeCode) : this()
        {
            _hasPendingSelection = true;
            _pendingSelectCode = employeeCode;
            txtSearch.Text = employeeCode;   // fires TextChanged → LoadEmployees(filtered)
        }



        // Hover handlers in ThemeHelper bake an explicit row.DefaultCellStyle.BackColor
        // onto whichever rows were hovered, using IsDarkMode at that instant. On a
        // theme toggle those baked colors go stale and don't follow the new theme —
        // clearing them lets rows fall back to the grid's (freshly updated) default
        // and alternating-row styles instead.
        private static void ResetRowStyleOverrides(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
                row.DefaultCellStyle.BackColor = Color.Empty;
        }


        public void ApplyTheme()
        {
            ThemeHelper.ApplyFormBackground(this);
            ThemeHelper.StyleToolbarPanel(panelTop);
            ThemeHelper.StyleButton(btnAdd);
            ThemeHelper.StyleButton(btnRefresh, Color.FromArgb(33, 150, 243));
            ThemeHelper.StyleDangerButton(btnDelete);
            ThemeHelper.StyleButton(btnEdit, Color.FromArgb(245, 158, 11));
            ThemeHelper.StyleSecondaryButton(btnExport);
            ThemeHelper.StyleSecondaryButton(btnImport);
            ThemeHelper.StyleDataGridView(dgvEmployees);
            ResetRowStyleOverrides(dgvEmployees);
            ApplyRoundedSearchStyle(txtSearch);
            ThemeHelper.StyleComboBox(cmbPageSize);
            ThemeHelper.StyleSecondaryButton(btnPrevPage);
            ThemeHelper.StyleSecondaryButton(btnNextPage);
        }

        private void SetupCheckboxSelectHeader()
        {
            if (!dgvEmployees.Columns.Contains("ColSelect"))
            {
                var chkCol = new DataGridViewCheckBoxColumn
                {
                    Name = "ColSelect",
                    HeaderText = "",
                    Width = 40,
                    ReadOnly = false,
                    Visible = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    TrueValue = true,
                    FalseValue = false,
                    IndeterminateValue = false
                };
                dgvEmployees.Columns.Add(chkCol);
                dgvEmployees.Columns["ColSelect"].DisplayIndex = 0;

                dgvEmployees.ReadOnly = false;
                foreach (DataGridViewColumn col in dgvEmployees.Columns)
                    col.ReadOnly = col.Name != "ColSelect";

                dgvEmployees.CurrentCellDirtyStateChanged += (s, ev) =>
                {
                    if (dgvEmployees.CurrentCell is DataGridViewCheckBoxCell)
                        dgvEmployees.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };

                dgvEmployees.CellValueChanged += (s, ev) =>
                {
                    if (ev.ColumnIndex >= 0 && dgvEmployees.Columns[ev.ColumnIndex].Name == "ColSelect")
                    {
                        SyncHeaderCheckbox();
                    }
                };
            }

            _chkHeaderPanel = new Panel
            {
                Name = "pnlCheckHeader",
                Size = new Size(62, 32),
                Margin = new Padding(4, 4, 0, 0),
                Cursor = Cursors.Hand,
                BackColor = ThemeHelper.IsDarkMode
                    ? Color.FromArgb(40, 44, 52)
                    : Color.FromArgb(241, 243, 244)
            };

            _chkHeaderBox = new CheckBox
            {
                Name = "chkHeaderBox",
                Size = new Size(40, 32),
                Location = new Point(0, 0),
                Appearance = Appearance.Normal,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeHelper.TextPrimary,
                Text = "",
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                CheckAlign = ContentAlignment.MiddleCenter,
                ThreeState = false
            };
            _chkHeaderBox.FlatAppearance.BorderSize = 0;
            _chkHeaderBox.FlatAppearance.MouseOverBackColor = Color.Transparent;
            _chkHeaderBox.FlatAppearance.CheckedBackColor = Color.Transparent;
            _chkHeaderBox.CheckedChanged += (s, e) =>
            {
                if (_suppressHeaderSync) return;
                SetAllCheckboxes(_chkHeaderBox.Checked);
            };

            var btnArrow = new Button
            {
                Name = "btnCheckArrow",
                Text = "▾",
                Size = new Size(22, 32),
                Location = new Point(40, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeHelper.TextPrimary,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            btnArrow.FlatAppearance.BorderSize = 0;
            btnArrow.FlatAppearance.MouseOverBackColor = ThemeHelper.IsDarkMode
                ? Color.FromArgb(55, 60, 70)
                : Color.FromArgb(218, 220, 224);
            btnArrow.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip { Font = ThemeHelper.FontDefault };
                var itemAll = new ToolStripMenuItem("All");
                var itemNone = new ToolStripMenuItem("None");
                itemAll.Click += (_, __) => { SetAllCheckboxes(true); _chkHeaderBox.Checked = true; };
                itemNone.Click += (_, __) => { SetAllCheckboxes(false); _chkHeaderBox.Checked = false; };
                menu.Items.Add(itemAll);
                menu.Items.Add(itemNone);
                menu.Show(btnArrow, new Point(0, btnArrow.Height));
            };

            _chkHeaderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeHelper.IsDarkMode
                    ? Color.FromArgb(65, 70, 80)
                    : Color.FromArgb(200, 202, 206), 1f);
                e.Graphics.DrawLine(pen, 40, 4, 40, _chkHeaderPanel.Height - 4);
            };

            _chkHeaderPanel.Controls.Add(_chkHeaderBox);
            _chkHeaderPanel.Controls.Add(btnArrow);

            flowToolbar.Controls.Add(_chkHeaderPanel);
            flowToolbar.Controls.SetChildIndex(_chkHeaderPanel, flowToolbar.Controls.IndexOf(btnAdd));
        }

        private void SetAllCheckboxes(bool check)
        {
            if (!dgvEmployees.Columns.Contains("ColSelect")) return;
            foreach (DataGridViewRow row in dgvEmployees.Rows)
                row.Cells["ColSelect"].Value = check;
            dgvEmployees.RefreshEdit();
        }

        private void SyncHeaderCheckbox()
        {
            if (!dgvEmployees.Columns.Contains("ColSelect") || _chkHeaderBox == null) return;
            _suppressHeaderSync = true;
            int total = dgvEmployees.Rows.Count;
            int checked_ = dgvEmployees.Rows.Cast<DataGridViewRow>()
                .Count(r => Convert.ToBoolean(r.Cells["ColSelect"].Value));
            _chkHeaderBox.Checked = total > 0 && checked_ == total;
            _suppressHeaderSync = false;
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




        // ── Load — DB-level paging when not searching, in-memory paging over fuzzy matches
        private void LoadEmployees(string searchTerm = "")
        {
            try
            {
                dgvEmployees.Rows.Clear();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    var result = _employeeService.GetPage(_currentPage, _pageSize);
                    if (!result.Success)
                    {
                        ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                        return;
                    }

                    _totalPages = Math.Max(1, result.Data!.TotalPages);
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    BindEmployeeRows(result.Data.Items);
                    lblRowCount.Text = $"Showing {result.Data.Items.Count} of {result.Data.TotalCount} records";
                }
                else
                {
                    var result = _employeeService.GetAll();
                    if (!result.Success)
                    {
                        ToastHelper.Show(this, result.ErrorMessage, ToastType.Error);
                        return;
                    }

                    var allEmployees = result.Data ?? new List<Employee>();

                    var codeMatches = allEmployees.Where(e =>
                        (!string.IsNullOrEmpty(e.EmployeeCode) && e.EmployeeCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var fullMatches = codeMatches.Count > 0
                        ? codeMatches
                        : allEmployees.Where(e => FuzzySearchHelper.IsFuzzyMatch(e.FullName, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Department, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Email, searchTerm)
                                                || FuzzySearchHelper.IsFuzzyMatch(e.Phone, searchTerm))
                            .OrderByDescending(e => Math.Max(
                                FuzzySearchHelper.FuzzyScore(e.FullName, searchTerm),
                                Math.Max(
                                    FuzzySearchHelper.FuzzyScore(e.Department, searchTerm),
                                    Math.Max(
                                        FuzzySearchHelper.FuzzyScore(e.Email, searchTerm),
                                        FuzzySearchHelper.FuzzyScore(e.Phone, searchTerm)))))
                            .ToList();

                    _totalPages = Math.Max(1, (int)Math.Ceiling(fullMatches.Count / (double)_pageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    var pageItems = fullMatches
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToList();

                    BindEmployeeRows(pageItems);

                    if (!string.IsNullOrWhiteSpace(_pendingSelectCode))
                    {
                        var match = dgvEmployees.Rows.Cast<DataGridViewRow>()
                            .FirstOrDefault(r => string.Equals(r.Cells[0].Value?.ToString(), _pendingSelectCode, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            dgvEmployees.ClearSelection();
                            match.Selected = true;
                            dgvEmployees.CurrentCell = match.Cells[0];
                            dgvEmployees.FirstDisplayedScrollingRowIndex = match.Index;
                        }
                        _pendingSelectCode = null;
                    }

                    lblRowCount.Text = $"Showing {pageItems.Count} of {fullMatches.Count} records";
                }

                lblPageIndicator.Text = $"Page {_currentPage} of {Math.Max(1, _totalPages)}";
                btnPrevPage.Enabled = _currentPage > 1;
                btnNextPage.Enabled = _currentPage < _totalPages;

                dgvEmployees.AutoResizeColumns();
                SyncHeaderCheckbox();
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, "Error loading employees.\n\n" + ex.Message, ToastType.Error);
            }
        }

        private void BindEmployeeRows(List<Employee> items)
        {
            foreach (var item in items)
            {
                int rowIdx = dgvEmployees.Rows.Add(
                    item.EmployeeCode,
                    item.FullName,
                    item.Department,
                    item.Email,
                    item.Phone,
                    item.IsActive ? "Active" : "Inactive"
                );

                var statusCell = dgvEmployees.Rows[rowIdx].Cells[5];
                statusCell.Style.ForeColor = item.IsActive ? ThemeHelper.Success : ThemeHelper.Danger;
                statusCell.Style.Font = ThemeHelper.FontBold;
            }
        }

        private void DgvEmployees_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var code = dgvEmployees.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(code)) return;

            var resultAll = _employeeService.GetAll();
            if (!resultAll.Success || resultAll.Data == null) return;

            var emp = resultAll.Data.Find(x => string.Equals(x.EmployeeCode, code, StringComparison.OrdinalIgnoreCase));
            if (emp == null) return;

            using var popup = new EmployeeDetailPopup(emp);
            popup.ShowDialog(this);
        }

        // ── Centralised toast helper — delete=red, edit=yellow, everything else=green ──
        private void ShowToast(string message, ToastType type = ToastType.Success, ToastIntent intent = ToastIntent.Normal)
        {
            Color? col = intent switch
            {
                ToastIntent.Delete => Color.FromArgb(211, 47, 47),
                ToastIntent.Edit   => Color.FromArgb(245, 158, 11),
                _                  => Color.FromArgb(56, 142, 60)
            };
            ToastHelper.Show(this, message, type, 3000, col);
        }
        private enum ToastIntent { Normal, Edit, Delete }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using var form = new AddEditEmployeeForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                ShowToast("Employee added successfully.");
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count == 0)
            {
                ToastHelper.Show(this, "Please select an employee to edit.", ToastType.Info);
                return;
            }
            var code = dgvEmployees.SelectedRows[0].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(code)) return;

            var resultAll = _employeeService.GetAll();
            if (resultAll.Success && resultAll.Data != null)
            {
                var emp = resultAll.Data.Find(x => string.Equals(x.EmployeeCode, code, StringComparison.OrdinalIgnoreCase));
                if (emp != null)
                {
                    using var form = new AddEditEmployeeForm(emp);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        ShowToast("Employee updated successfully.", ToastType.Success, ToastIntent.Edit);
                        LoadEmployees(txtSearch.Text.Trim());
                    }
                }
            }
        }

        private List<string> GetSelectedEmployeeCodes()
        {
            var codes = new List<string>();

            if (dgvEmployees.Columns.Contains("ColSelect"))
            {
                foreach (DataGridViewRow row in dgvEmployees.Rows)
                {
                    var val = row.Cells["ColSelect"].Value;
                    if (val is bool b && b)
                    {
                        var code = row.Cells[0].Value?.ToString();
                        if (!string.IsNullOrEmpty(code)) codes.Add(code);
                    }
                }
            }

            if (codes.Count == 0)
            {
                foreach (DataGridViewRow row in dgvEmployees.SelectedRows)
                {
                    var code = row.Cells[0].Value?.ToString();
                    if (!string.IsNullOrEmpty(code)) codes.Add(code);
                }
            }

            return codes;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedCodes = GetSelectedEmployeeCodes();
            if (selectedCodes.Count == 0)
            {
                ShowToast("Please select at least one employee to delete.", ToastType.Info, ToastIntent.Delete);
                return;
            }

            var resultAll = _employeeService.GetAll();
            if (!resultAll.Success || resultAll.Data == null)
            {
                ToastHelper.Show(this, "Unable to retrieve employee data.", ToastType.Error);
                return;
            }

            var selectedEmployees = resultAll.Data
                .Where(emp => selectedCodes.Any(c => string.Equals(c, emp.EmployeeCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (selectedEmployees.Count == 0) return;

            string confirmMsg = selectedEmployees.Count == 1
                ? $"Are you sure you want to delete employee '{selectedEmployees[0].FullName}' ({selectedEmployees[0].EmployeeCode})?"
                : $"Are you sure you want to delete these {selectedEmployees.Count} employees?";

            if (MessageBox.Show(confirmMsg, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int deletedCount = 0;
            string? firstError = null;

            foreach (var emp in selectedEmployees)
            {
                var result = _employeeService.Delete(emp.Id);
                if (result.Success)
                    deletedCount++;
                else if (firstError == null)
                    firstError = result.ErrorMessage;
            }

            if (deletedCount > 0)
            {
                ShowToast($"{deletedCount} employee(s) deleted successfully.", ToastType.Success, ToastIntent.Delete);
                LoadEmployees(txtSearch.Text.Trim());
            }
            else if (firstError != null)
            {
                ShowToast(firstError, ToastType.Error, ToastIntent.Delete);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Import Employees from Excel"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var (items, errors) = ExcelHelper.ImportEmployees(ofd.FileName);

                if (errors.Count > 0)
                {
                    string errorMessage = string.Join("\n", errors.Take(10));
                    if (errors.Count > 10) errorMessage += $"\n...and {errors.Count - 10} more errors.";
                    ToastHelper.Show(this, $"Import Warnings:\n{errorMessage}", ToastType.Error);
                }

                if (items != null && items.Count > 0)
                {
                    int importedCount = 0;
                    var importFailures = new List<string>();

                    foreach (var emp in items)
                    {
                        var result = _employeeService.Add(emp);
                        if (result.Success)
                        {
                            importedCount++;
                        }
                        else if (!string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            importFailures.Add($"{emp.FullName}: {result.ErrorMessage}");
                        }
                    }

                    if (importFailures.Count > 0)
                    {
                        string failMsg = string.Join("\n", importFailures.Take(5));
                        ToastHelper.Show(this, $"Imported {importedCount} of {items.Count}.\nErrors:\n{failMsg}", ToastType.Error);
                    }
                    if (importedCount > 0 && importFailures.Count == 0)
                    {
                        ShowToast($"Successfully imported {importedCount} employees.");
                    }

                    // Reset search filter and pagination to show imported records immediately
                    txtSearch.Text = string.Empty;
                    _currentPage = 1;
                    LoadEmployees();
                }
                else if (errors.Count == 0)
                {
                    ToastHelper.Show(this, "No valid employee records found in the selected file.", ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Import failed: {ex.Message}", ToastType.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            _currentPage = 1;
            SetAllCheckboxes(false);
            if (_chkHeaderBox != null) _chkHeaderBox.Checked = false;
            LoadEmployees();
            ToastHelper.Show(this, "Employees refreshed.", ToastType.Info);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _currentPage = 1;
            LoadEmployees(txtSearch.Text.Trim());
        }

        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cmbPageSize.SelectedItem?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadEmployees(txtSearch.Text.Trim());
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    Title = "Export Employees",
                    FileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Employee Code,Full Name,Designation,Email,Phone,Status");

                foreach (DataGridViewRow row in dgvEmployees.Rows)
                {
                    if (row.IsNewRow) continue;

                    sb.AppendLine(string.Join(",",
                        $"\"{row.Cells[0]?.Value}\"",
                        $"\"{row.Cells[1]?.Value}\"",
                        $"\"{row.Cells[2]?.Value}\"",
                        $"\"{row.Cells[3]?.Value}\"",
                        $"\"{row.Cells[4]?.Value}\"",
                        $"\"{row.Cells[5]?.Value}\""));
                }

                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                ToastHelper.Show(this, "Employees exported successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastHelper.Show(this, $"Export failed: {ex.Message}", ToastType.Error);
            }
        }
    }
}