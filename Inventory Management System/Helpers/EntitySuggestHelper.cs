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
    /// </summary>
    /// 

 


    public sealed class EntitySuggestHelper<T> : IDisposable where T : class
    {
        private const int MaxSuggestions = 100;
        private const int MaxVisibleItems = 8;
        private const int MinQueryLength = 1;
        private const int ItemHeight     = 40;

        private readonly TextBox               _textBox;
        private readonly Form                  _hostForm;
        private readonly Action<T>             _onSelected;
        private readonly Func<T, string, int>  _scoreFn;
        private readonly Func<T, string>       _line1;
        private readonly Func<T, string>       _line2;
        private readonly ListBox               _listBox;
        private List<T>                        _allItems = new();
        private bool                           _suppressTextChanged;

        public EntitySuggestHelper(
            TextBox textBox,
            Form hostForm,
            Action<T> onSelected,
            Func<T, string, int> scoreFn,
            Func<T, string> line1Selector,
            Func<T, string> line2Selector)
        {
            _textBox    = textBox;
            _hostForm   = hostForm;
            _onSelected = onSelected;
            _scoreFn    = scoreFn;
            _line1      = line1Selector;
            _line2      = line2Selector;

            _listBox = new SuggestionListBox    
            {
                Visible        = false,
                BorderStyle    = BorderStyle.FixedSingle,
                Font           = ThemeHelper.FontDefault,
                BackColor      = Color.White,
                ForeColor      = ThemeHelper.TextPrimary,
                DrawMode       = DrawMode.OwnerDrawFixed,
                ItemHeight     = ItemHeight,
                IntegralHeight = false,
                TabStop        = false,
            };
            _listBox.ItemHeight = ItemHeight;

            _listBox.DrawItem   += ListBox_DrawItem;
            _listBox.MouseClick += ListBox_MouseClick;
            _listBox.KeyDown    += ListBox_KeyDown;

            _hostForm.Controls.Add(_listBox);
            _listBox.BringToFront();

            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyDown     += TextBox_KeyDown;
            _textBox.LostFocus   += TextBox_LostFocus;

            _hostForm.MouseClick += Form_MouseClick;
        }

        /// <summary>Call this whenever the underlying data list is (re)loaded.</summary>
        public void LoadItems(List<T> items) => _allItems = items ?? new List<T>();

        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressTextChanged) return;
            UpdateSuggestions();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_listBox.Visible) return;
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_listBox.Items.Count > 0)
                    {
                        _listBox.Focus();
                        _listBox.SelectedIndex = 0;
                    }
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    HideDropdown();
                    e.Handled = true;
                    break;
            }
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

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            var t = new System.Windows.Forms.Timer { Interval = 150 };
            t.Tick += (s, _) => { t.Stop(); t.Dispose(); if (!_listBox.Focused) HideDropdown(); };
            t.Start();
        }

        private void ListBox_MouseClick(object? sender, MouseEventArgs e)
        {
            int idx = _listBox.IndexFromPoint(e.Location);
            if (idx >= 0 && idx < _listBox.Items.Count)
                CommitSelection(idx);
        }

        private void ListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (_listBox.SelectedIndex >= 0)
                        CommitSelection(_listBox.SelectedIndex);
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    HideDropdown();
                    _textBox.Focus();
                    e.Handled = true;
                    break;
            }
        }

        private void Form_MouseClick(object? sender, MouseEventArgs e) => HideDropdown();

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;
            if (_listBox.Items[e.Index] is not T item) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;

            Color bgColor = selected ? Color.FromArgb(232, 245, 233) : Color.White;
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            if (selected)
            {
                using var accentBrush = new SolidBrush(ThemeHelper.Accent);
                e.Graphics.FillRectangle(accentBrush, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            int textX = e.Bounds.X + 10;
            using var line1Brush = new SolidBrush(ThemeHelper.Accent);
            using var line2Brush = new SolidBrush(ThemeHelper.TextSecondary);

            e.Graphics.DrawString(_line1(item) ?? "—", ThemeHelper.FontBold,  line1Brush, textX, e.Bounds.Y + 3);
            e.Graphics.DrawString(_line2(item) ?? "—", ThemeHelper.FontSmall, line2Brush, textX, e.Bounds.Y + 20);

            using var sepPen = new Pen(ThemeHelper.CardBorder, 1f);
            e.Graphics.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
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

            PositionDropdown(scored.Count);
            _listBox.Visible = true;
            _listBox.BringToFront();
        }

        private void PositionDropdown(int itemCount)
        {
            Point tbBottomLeft  = _textBox.Parent!.PointToScreen(new Point(_textBox.Left, _textBox.Bottom));
            Point formRelative  = _hostForm.PointToClient(tbBottomLeft);
            int   dropH         = Math.Min(itemCount, MaxVisibleItems) * ItemHeight + 2;

            int maxAllowedHeight = _hostForm.ClientSize.Height - formRelative.Y - 10;
            if (maxAllowedHeight < ItemHeight) maxAllowedHeight = ItemHeight;
            
            if (dropH > maxAllowedHeight)
            {
                dropH = maxAllowedHeight;
            }

            _listBox.SetBounds(formRelative.X, formRelative.Y, Math.Max(_textBox.Width, 260), dropH);
        }

        private void CommitSelection(int index)
        {
            if (index < 0 || index >= _listBox.Items.Count) return;
            if (_listBox.Items[index] is not T selected) return;

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

        private void HideDropdown() => _listBox.Visible = false;

        public void Dispose()
        {
            _textBox.TextChanged -= TextBox_TextChanged;
            _textBox.KeyDown     -= TextBox_KeyDown;
            _textBox.LostFocus   -= TextBox_LostFocus;
            _hostForm.MouseClick -= Form_MouseClick;

            if (_hostForm.Controls.Contains(_listBox))
                _hostForm.Controls.Remove(_listBox);

            _listBox.Dispose();
        }
    }
}