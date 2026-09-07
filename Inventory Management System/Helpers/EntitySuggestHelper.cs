// Helpers/EntitySuggestHelper.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Generic version of EquipmentSuggestHelper. Attaches a live fuzzy-suggestion
    /// dropdown to a TextBox for any entity type T. As the user types, the closest
    /// matches (scored by scoreFn) appear in a floating ListBox below the TextBox.
    ///
    /// Key design constraints:
    ///   • The popup is WS_EX_NOACTIVATE so it never steals keyboard focus.
    ///   • ALL keyboard navigation (↑/↓/Enter/Escape) is handled in the TextBox
    ///     KeyDown handler — the ListBox is used only as a visual highlight surface.
    ///   • Mouse selection uses MouseDown to suppress the LostFocus hide-timer,
    ///     then commits on MouseUp.
    /// </summary>
    public sealed class EntitySuggestHelper<T> : IDisposable where T : class
    {
        private const int MaxSuggestions = 100;
        private const int MaxVisibleItems = 8;
        private const int MinQueryLength = 1;
        private const int ItemHeight = 40;

        private readonly TextBox _textBox;
        private readonly Form _hostForm;
        private readonly Action<T> _onSelected;
        private readonly Func<T, string, int> _scoreFn;
        private readonly Func<T, string> _line1;
        private readonly Func<T, string> _line2;
        private readonly ListBox _listBox;
        private readonly PopupForm _popup;
        private readonly Color _accentColor;
        private List<T> _allItems = new();
        private bool _suppressTextChanged;
        private bool _suppressHide;   // true while a listbox mouse-down is in progress
        private int _highlightedIndex = -1; // keyboard-navigation cursor (not focus)

        public EntitySuggestHelper(
       TextBox textBox,
       Form hostForm,
       Action<T> onSelected,
       Func<T, string, int> scoreFn,
       Func<T, string> line1Selector,
       Func<T, string> line2Selector,
       Color? accentColor = null)
        {
            _textBox = textBox;
            _hostForm = hostForm;
            _onSelected = onSelected;
            _scoreFn = scoreFn;
            _line1 = line1Selector;
            _line2 = line2Selector;
            _accentColor = accentColor ?? ThemeHelper.Accent;

            _listBox = new SuggestionListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = ItemHeight,
                IntegralHeight = false,
                TabStop = false,
            };
            _listBox.ItemHeight = ItemHeight;

            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.MouseDown += ListBox_MouseDown;
            _listBox.MouseUp += ListBox_MouseUp;
            // NOTE: no KeyDown on the listbox — all keyboard handling stays in the textbox.

            // Popup host: a separate, non-activating floating window — not a
            // child of _hostForm.Controls. See EquipmentSuggestHelper for why:
            // that collection also holds Dock-based panels, and reparenting/
            // reordering a child into it on every keystroke forces a full
            // dock re-layout of the host form, which is what corrupted the
            // toolbar row. An owned top-level window floats above _hostForm
            // by normal Z-order and never touches its layout.
            _popup = new PopupForm();
            _popup.Controls.Add(_listBox);

            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.LostFocus += TextBox_LostFocus;

            // Hide when the host form is clicked, moved, resized, or loses focus
            _hostForm.MouseClick += Form_MouseClick;
            _hostForm.Move += Form_MoveOrResize;
            _hostForm.Resize += Form_MoveOrResize;
            _hostForm.Deactivate += Form_MoveOrResize;
        }

        /// <summary>Call this whenever the underlying data list is (re)loaded.</summary>
        public void LoadItems(List<T> items) => _allItems = items ?? new List<T>();

        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressTextChanged) return;
            _highlightedIndex = -1; // reset arrow-key cursor on new query
            UpdateSuggestions();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_popup.Visible) return;

            switch (e.KeyCode)
            {
                case Keys.Down:
                    MoveHighlight(+1);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.Up:
                    MoveHighlight(-1);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.Enter:
                    if (_highlightedIndex >= 0 && _highlightedIndex < _listBox.Items.Count)
                    {
                        CommitSelection(_highlightedIndex);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;

                case Keys.Escape:
                    HideDropdown();
                    _highlightedIndex = -1;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        /// <summary>
        /// Moves the keyboard highlight cursor by <paramref name="delta"/> steps,
        /// clamped within the item list. Updates _listBox.SelectedIndex for visual
        /// feedback without transferring keyboard focus to the popup.
        /// </summary>
        private void MoveHighlight(int delta)
        {
            if (_listBox.Items.Count == 0) return;

            int next = _highlightedIndex + delta;
            next = Math.Max(0, Math.Min(_listBox.Items.Count - 1, next));
            _highlightedIndex = next;

            // Update visual selection without activating the popup window.
            _listBox.SelectedIndex = _highlightedIndex;
        }

        private sealed class SuggestionListBox : ListBox
        {
            protected override bool IsInputKey(Keys keyData)
            {
                if (keyData == Keys.Enter || keyData == Keys.Escape ||
                    keyData == Keys.Up || keyData == Keys.Down)
                    return true;

                return base.IsInputKey(keyData);
            }
        }

        // A borderless, non-activating floating window used to host the
        // suggestion ListBox. WS_EX_NOACTIVATE + ShowWithoutActivation means
        // showing it never steals focus from the search TextBox.
        private sealed class PopupForm : Form
        {
            public PopupForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    const int WS_EX_NOACTIVATE = 0x08000000;
                    const int WS_EX_TOOLWINDOW = 0x00000080;
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                    return cp;
                }
            }
        }

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            // Delay hiding so a click on the popup listbox can commit first.
            // If a mouse-down on the listbox is in progress, don't hide at all —
            // MouseUp will commit and then hide.
            if (_suppressHide) return;
            var t = new System.Windows.Forms.Timer { Interval = 200 };
            t.Tick += (s, _) =>
            {
                t.Stop(); t.Dispose();
                if (!_suppressHide) HideDropdown();
            };
            t.Start();
        }

        private void ListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            // Prevent LostFocus from hiding the popup before MouseUp fires.
            _suppressHide = true;
        }

        private void ListBox_MouseUp(object? sender, MouseEventArgs e)
        {
            _suppressHide = false;
            int idx = _listBox.IndexFromPoint(e.Location);
            if (idx >= 0 && idx < _listBox.Items.Count)
                CommitSelection(idx);
        }

        private void Form_MouseClick(object? sender, MouseEventArgs e) => HideDropdown();

        private void Form_MoveOrResize(object? sender, EventArgs e) => HideDropdown();

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;
            if (_listBox.Items[e.Index] is not T item) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;

            Color bgColor = selected
          ? TintColor(ThemeHelper.CardBg, _accentColor, ThemeHelper.IsDarkMode ? 22 : 14)
          : ThemeHelper.CardBg;
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            if (selected)
            {
                using var accentBrush = new SolidBrush(_accentColor);
                e.Graphics.FillRectangle(accentBrush, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            int textX = e.Bounds.X + 10;
            using var line1Brush = new SolidBrush(_accentColor);
            using var line2Brush = new SolidBrush(ThemeHelper.TextSecondary);

            e.Graphics.DrawString(_line1(item) ?? "—", ThemeHelper.FontBold, line1Brush, textX, e.Bounds.Y + 3);
            e.Graphics.DrawString(_line2(item) ?? "—", ThemeHelper.FontSmall, line2Brush, textX, e.Bounds.Y + 20);

            using var sepPen = new Pen(ThemeHelper.CardBorder, 1f);
            e.Graphics.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private static Color TintColor(Color baseColor, Color accent, int percent)
        {
            double a = percent / 100.0;
            int r = (int)(accent.R * a + baseColor.R * (1 - a));
            int g = (int)(accent.G * a + baseColor.G * (1 - a));
            int b = (int)(accent.B * a + baseColor.B * (1 - a));
            return Color.FromArgb(r, g, b);
        }

        private void UpdateSuggestions()
        {
            string query = _textBox.Text.Trim();
            if (query.Length < MinQueryLength || string.IsNullOrWhiteSpace(query))
            {
                HideDropdown();
                return;
            }

            var scored = _allItems
                .Select(item => new { Item = item, Score = _scoreFn(item, query) })
                .Where(x => x.Score > -10000)
                .OrderByDescending(x => x.Score)
                .Take(MaxSuggestions)
                .ToList();

            if (scored.Count == 0)
            {
                HideDropdown();
                return;
            }

            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (var item in scored)
                _listBox.Items.Add(item.Item);
            _listBox.EndUpdate();

            // Reset highlight on new query results
            _listBox.SelectedIndex = -1;

            PositionDropdown(scored.Count);
            if (!_popup.Visible)
                _popup.Show(_hostForm);
        }

        private void PositionDropdown(int itemCount)
        {
            // Screen coordinates — the popup is a top-level window, so it's
            // positioned in screen space, not translated into _hostForm's
            // client coordinates.
            Point screenPos = _textBox.Parent!.PointToScreen(
                new Point(_textBox.Left, _textBox.Bottom));

            int dropH = Math.Min(itemCount, MaxVisibleItems) * ItemHeight + 2;
            int width = Math.Max(_textBox.Width, 260);

            // Keep it on-screen if the textbox is near the bottom edge.
            var workArea = Screen.FromControl(_textBox).WorkingArea;
            if (screenPos.Y + dropH > workArea.Bottom)
                dropH = Math.Max(ItemHeight, workArea.Bottom - screenPos.Y);

            _popup.Bounds = new Rectangle(screenPos.X, screenPos.Y, width, dropH);
        }

        private void CommitSelection(int index)
        {
            if (index < 0 || index >= _listBox.Items.Count) return;
            if (_listBox.Items[index] is not T selected) return;

            _highlightedIndex = -1;
            HideDropdown();

            // Clear the box rather than filling it with the composite "line1"
            // label. This TextBox typically doubles as a live-filter box in the
            // host form (e.g. AllocationForm's search bar), so leaving a label
            // like "EQ008 — Phase Modulator" in place would immediately
            // re-trigger that filter with text that was never meant to be
            // searched — it's a display label, not a query — producing an
            // empty grid right after a successful selection.
            _suppressTextChanged = true;
            _textBox.Text = string.Empty;
            _suppressTextChanged = false;

            _textBox.Focus();
            _onSelected(selected);
        }

        private void HideDropdown()
        {
            _highlightedIndex = -1;
            if (_popup.Visible) _popup.Hide();
        }

        public void Dispose()
        {
            _textBox.TextChanged -= TextBox_TextChanged;
            _textBox.KeyDown -= TextBox_KeyDown;
            _textBox.LostFocus -= TextBox_LostFocus;
            _hostForm.MouseClick -= Form_MouseClick;
            _hostForm.Move -= Form_MoveOrResize;
            _hostForm.Resize -= Form_MoveOrResize;
            _hostForm.Deactivate -= Form_MoveOrResize;

            if (!_popup.IsDisposed) _popup.Dispose(); // also disposes _listBox
        }
    }
}