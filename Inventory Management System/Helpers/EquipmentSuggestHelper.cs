// Helpers/EquipmentSuggestHelper.cs
using Inventory_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Sets the textbox's text without triggering the suggestion dropdown.
    /// Use this when programmatically populating the search box (e.g. jumping
    /// to a specific record from an external search), where showing the
    /// fuzzy-suggestion popup would be unwanted UI noise.
    /// </summary>
  
    public sealed class EquipmentSuggestHelper : IDisposable
    {
        // ── Configuration ──────────────────────────────────────────────────
        private const int MaxSuggestions  = 8;
        private const int MinQueryLength  = 1;
        private const int ItemHeight      = 40;

        // ── State ──────────────────────────────────────────────────────────
        private readonly TextBox             _textBox;
        private readonly Form                _hostForm;
        private readonly Action<Equipment>   _onSelected;
        private readonly ListBox             _listBox;
        private List<Equipment>              _allEquipment = new();
        private bool                         _suppressTextChanged;

        // ── Constructor ────────────────────────────────────────────────────
        public EquipmentSuggestHelper(TextBox textBox, Form hostForm, Action<Equipment> onSelected)
        {
            _textBox    = textBox;
            _hostForm   = hostForm;
            _onSelected = onSelected;

            // ── Build the floating ListBox ─────────────────────────────────
            _listBox = new SuggestionListBox
            {
                Visible          = false,
                BorderStyle      = BorderStyle.FixedSingle,
                Font             = ThemeHelper.FontDefault,
                BackColor = ThemeHelper.CardBg,
                ForeColor = ThemeHelper.TextPrimary,
                DrawMode         = DrawMode.OwnerDrawFixed,
                ItemHeight       = ItemHeight,
                IntegralHeight   = false,
                TabStop          = false,
            };
            // Explicitly set ItemHeight again after setting DrawMode to guarantee it applies
            _listBox.ItemHeight = ItemHeight;

            _listBox.DrawItem       += ListBox_DrawItem;
            _listBox.MouseClick     += ListBox_MouseClick;
            _listBox.KeyDown        += ListBox_KeyDown;

            // Add to the form so it floats above all other controls
            _hostForm.Controls.Add(_listBox);
            _listBox.BringToFront();

            // ── Wire TextBox events ────────────────────────────────────────
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyDown     += TextBox_KeyDown;
            _textBox.LostFocus   += TextBox_LostFocus;

            // ── Hide when form is clicked elsewhere ────────────────────────
            _hostForm.MouseClick += Form_MouseClick;
        }

        // ── Load equipment data ────────────────────────────────────────────
        /// <summary>
        /// Call this once (e.g. in Form.Load) to provide the full equipment list
        /// that the dropdown will search against.
        /// </summary>
        /// 
        public void SetTextWithoutSuggesting(string text)
        {
            _suppressTextChanged = true;
            _textBox.Text = text;
            _textBox.SelectionStart = _textBox.Text.Length;
            _suppressTextChanged = false;
            HideDropdown();
        }
        public void LoadEquipment(List<Equipment> equipment)
        {
            _allEquipment = equipment ?? new List<Equipment>();
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


        // ── TextBox events ─────────────────────────────────────────────────
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
                    // Move focus into the list
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

        private void TextBox_LostFocus(object? sender, EventArgs e)
        {
            // Small delay so a ListBox click is processed before hiding
            var t = new System.Windows.Forms.Timer { Interval = 150 };
            t.Tick += (s, _) => { t.Stop(); t.Dispose(); if (!_listBox.Focused) HideDropdown(); };
            t.Start();
        }

        // ── ListBox events ─────────────────────────────────────────────────
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

        private void Form_MouseClick(object? sender, MouseEventArgs e)
        {
            HideDropdown();
        }

        // ── Owner-draw: two-line look (code bold + name secondary) ─────────
        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;
            if (_listBox.Items[e.Index] is not Equipment eq) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;

            // Background
            Color bgColor = selected
     ? (ThemeHelper.IsDarkMode ? Color.FromArgb(40, 60, 42) : Color.FromArgb(232, 245, 233))
     : ThemeHelper.CardBg;
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Left accent bar when selected
            if (selected)
            {
                using var accentBrush = new SolidBrush(ThemeHelper.Accent);
                e.Graphics.FillRectangle(accentBrush, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            // Equipment Code  (bold, primary colour)
            int textX  = e.Bounds.X + 10;
            int codeY  = e.Bounds.Y + 3;
            int nameY  = e.Bounds.Y + 20;

            using var codeBrush = new SolidBrush(ThemeHelper.Accent);
            using var nameBrush = new SolidBrush(ThemeHelper.TextSecondary);

            e.Graphics.DrawString(
                eq.EquipmentCode,
                ThemeHelper.FontBold,
                codeBrush,
                textX, codeY);

            e.Graphics.DrawString(
                eq.Name,
                ThemeHelper.FontSmall,
                nameBrush,
                textX, nameY);

            // Separator line
            using var sepPen = new Pen(ThemeHelper.CardBorder, 1f);
            e.Graphics.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        // ── Core logic ────────────────────────────────────────────────────
        private void UpdateSuggestions()
        {
            string query = _textBox.Text.Trim();

            if (query.Length < MinQueryLength || string.IsNullOrWhiteSpace(query))
            {
                HideDropdown();
                return;
            }

            var scored = _allEquipment
      .Select(eq => new
      {
          Equipment = eq,
          Score = Math.Max(
              FuzzySearchHelper.FuzzyScoreCode(eq.EquipmentCode, query),
              Math.Max(
                  FuzzySearchHelper.FuzzyScoreCode(eq.QrCode, query),
                  FuzzySearchHelper.FuzzyScore(eq.Name, query)))
      })
                  .Where(x => x.Score > -10000)
                 .OrderByDescending(x => x.Score)
                 .Take(MaxSuggestions)
                 .ToList();

            if (scored.Count == 0)
            {
                HideDropdown();
                return;
            }

            // Populate the list
            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (var item in scored)
                _listBox.Items.Add(item.Equipment);
            _listBox.EndUpdate();

            // Size and position the dropdown directly below the TextBox
            PositionDropdown(scored.Count);
            _listBox.Visible = true;
            _listBox.BringToFront();
        }

        private void PositionDropdown(int itemCount)
        {
            // Convert TextBox bottom-left to form client coordinates
            Point tbBottomLeft = _textBox.Parent!.PointToScreen(
                new Point(_textBox.Left, _textBox.Bottom));
            Point formRelative = _hostForm.PointToClient(tbBottomLeft);

            int dropH = Math.Min(itemCount, MaxSuggestions) * ItemHeight + 2;

            _listBox.SetBounds(formRelative.X, formRelative.Y, Math.Max(_textBox.Width, 260), dropH);
            _hostForm.Controls.SetChildIndex(_listBox, 0); // index 0 = always topmost, regardless of what else painted since
        }

        private void CommitSelection(int index)
        {
            if (index < 0 || index >= _listBox.Items.Count) return;
            if (_listBox.Items[index] is not Equipment selected) return;

            HideDropdown();

            // Suppress the TextChanged event so we don't re-trigger the dropdown
            _suppressTextChanged = true;
            _textBox.Text = selected.EquipmentCode;
            _textBox.SelectionStart = _textBox.Text.Length;
            _suppressTextChanged = false;

            _textBox.Focus();
            _onSelected(selected);
        }

        private void HideDropdown()
        {
            _listBox.Visible = false;
        }

        // ── IDisposable ────────────────────────────────────────────────────
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
