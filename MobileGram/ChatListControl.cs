using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MobileGram.Models;

namespace MobileGram.Controls
{
    public class ChatListControl : Control
    {
        public const int ItemHeight = 42;

        private List<ChatItem> _allChats = new List<ChatItem>();
        private List<ChatItem> _filteredChats = new List<ChatItem>();
        private string _currentFilter = "all";
        private int _selectedIndex = -1;
        private int _typingDots = 0;

        private VScrollBar _scrollBar;
        private Bitmap _offScreenBitmap;
        private Graphics _offScreenGraphics;

        public event Action<ChatItem> ChatSelected;

        // Cached GDI resources (static to avoid allocations in OnPaint)
        private static readonly Font FontTitle = new Font("Tahoma", 8.25f, FontStyle.Bold);
        private static readonly Font FontMessage = new Font("Tahoma", 7.5f, FontStyle.Regular);
        private static readonly Font FontTyping = new Font("Tahoma", 7.5f, FontStyle.Italic);
        private static readonly Font FontTime = new Font("Tahoma", 7.0f, FontStyle.Regular);
        private static readonly Font FontBadge = new Font("Tahoma", 7.0f, FontStyle.Bold);
        private static readonly Font FontAvatar = new Font("Tahoma", 10.0f, FontStyle.Bold);
        private static readonly Font FontGroupInitials = new Font("Tahoma", 7.5f, FontStyle.Bold);
        private static readonly Font FontGroupBadge = new Font("Tahoma", 6.5f, FontStyle.Bold);
        private static readonly Brush BrushGroupBadgeBg = new SolidBrush(Color.FromArgb(0, 85, 170));
        private static readonly Pen PenGroupBadgeBorder = new Pen(Color.White);

        private static readonly Brush BrushBack = new SolidBrush(Color.White);
        private static readonly Brush BrushSelected = new SolidBrush(Color.FromArgb(49, 106, 197)); // #316AC5
        private static readonly Brush BrushText = new SolidBrush(Color.Black);
        private static readonly Brush BrushTextSelected = new SolidBrush(Color.White);
        private static readonly Brush BrushSubText = new SolidBrush(Color.FromArgb(85, 85, 85)); // #555555
        private static readonly Brush BrushSubTextSelected = new SolidBrush(Color.FromArgb(224, 224, 224));
        private static readonly Brush BrushTyping = new SolidBrush(Color.FromArgb(0, 85, 170)); // #0055AA
        private static readonly Brush BrushTypingSelected = new SolidBrush(Color.FromArgb(192, 224, 255));
        private static readonly Brush BrushBadge = new SolidBrush(Color.FromArgb(0, 85, 170)); // #0055AA
        private static readonly Brush BrushBadgeMuted = new SolidBrush(Color.FromArgb(140, 160, 184)); // #8CA0B8
        private static readonly Brush BrushBadgeText = new SolidBrush(Color.White);

        // Presence indicators
        private static readonly Brush BrushOnline = new SolidBrush(Color.FromArgb(0, 170, 0)); // #00AA00
        private static readonly Brush BrushRecently = new SolidBrush(Color.FromArgb(255, 153, 0)); // #FF9900
        private static readonly Brush BrushOffline = new SolidBrush(Color.FromArgb(160, 160, 160)); // #A0A0A0

        // Avatar color palette (deterministic hash selection)
        private static readonly Brush[] AvatarPalette = new Brush[]
        {
            new SolidBrush(Color.FromArgb(205, 92, 92)),   // Soft terracotta
            new SolidBrush(Color.FromArgb(70, 130, 180)),  // Steel blue
            new SolidBrush(Color.FromArgb(147, 112, 219)), // Lavender
            new SolidBrush(Color.FromArgb(60, 150, 100)),  // Emerald
            new SolidBrush(Color.FromArgb(210, 105, 30)),  // Warm chocolate
            new SolidBrush(Color.FromArgb(0, 136, 204)),   // Telegram blue
            new SolidBrush(Color.FromArgb(178, 34, 34)),   // Crimson brick
            new SolidBrush(Color.FromArgb(95, 158, 160))   // Sea cadet
        };

        // Group icons
        private static readonly Brush BrushGroupHead1 = new SolidBrush(Color.FromArgb(184, 92, 0));
        private static readonly Brush BrushGroupHead2 = new SolidBrush(Color.FromArgb(128, 0, 128));
        private static readonly Brush BrushGroupHead3 = new SolidBrush(Color.FromArgb(0, 102, 204));

        private static readonly Pen PenSeparator = new Pen(Color.FromArgb(208, 208, 208));
        private static readonly Pen PenWhite = new Pen(Color.White);
        private static readonly Pen PenAvatarBorder = new Pen(Color.FromArgb(180, 180, 180));

        public ChatListControl()
        {
            this._scrollBar = new VScrollBar();
            this._scrollBar.Dock = DockStyle.Right;
            this._scrollBar.Width = 14;
            this._scrollBar.ValueChanged += new EventHandler(OnScrollChanged);
            this.Controls.Add(this._scrollBar);

            this.Resize += new EventHandler(OnControlResize);
            InitOffScreen();
        }

        public void SetChats(List<ChatItem> chats)
        {
            this._allChats = chats ?? new List<ChatItem>();
            ApplyFilter();
        }

        public void SetFilter(string filter)
        {
            _currentFilter = filter != null ? filter.ToLower() : "all";
            ApplyFilter();
        }

        public void AdvanceTypingAnimation()
        {
            _typingDots = (_typingDots + 1) % 4;
            this.Invalidate();
        }

        private void ApplyFilter()
        {
            _filteredChats.Clear();
            for (int i = 0; i < _allChats.Count; i++)
            {
                ChatItem item = _allChats[i];
                if (_currentFilter == "chats" && item.Type != "user") continue;
                if (_currentFilter == "groups" && item.Type != "group") continue;
                _filteredChats.Add(item);
            }

            _selectedIndex = _filteredChats.Count > 0 ? 0 : -1;
            UpdateScrollBar();
            this.Invalidate();
        }

        private void UpdateScrollBar()
        {
            int totalHeight = _filteredChats.Count * ItemHeight;
            if (_scrollBar != null)
            {
                if (totalHeight > this.Height)
                {
                    _scrollBar.Enabled = true;
                    _scrollBar.Maximum = totalHeight - this.Height;
                    _scrollBar.SmallChange = ItemHeight;
                    _scrollBar.LargeChange = ItemHeight * 3;
                }
                else
                {
                    _scrollBar.Value = 0;
                    _scrollBar.Enabled = false;
                }
            }
        }

        private void InitOffScreen()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                if (_offScreenGraphics != null)
                {
                    _offScreenGraphics.Dispose();
                    _offScreenGraphics = null;
                }
                if (_offScreenBitmap != null)
                {
                    _offScreenBitmap.Dispose();
                    _offScreenBitmap = null;
                }
                _offScreenBitmap = new Bitmap(this.Width, this.Height);
                _offScreenGraphics = Graphics.FromImage(_offScreenBitmap);
            }
        }

        private void OnControlResize(object sender, EventArgs e)
        {
            InitOffScreen();
            UpdateScrollBar();
            this.Invalidate();
        }

        private void OnScrollChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Suppress background flicker
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_offScreenBitmap == null || _offScreenGraphics == null)
            {
                InitOffScreen();
                if (_offScreenBitmap == null) return;
            }

            _offScreenGraphics.FillRectangle(BrushBack, 0, 0, this.Width, this.Height);

            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int scrollWidth = (_scrollBar != null) ? _scrollBar.Width : 14;
            int renderWidth = this.Width - scrollWidth;

            for (int i = 0; i < _filteredChats.Count; i++)
            {
                int itemY = -scrollVal + (i * ItemHeight);

                if (itemY + ItemHeight >= 0 && itemY <= this.Height)
                {
                    bool isSelected = (i == _selectedIndex);

                    // Background cell
                    Brush bgBrush = isSelected ? BrushSelected : BrushBack;
                    _offScreenGraphics.FillRectangle(bgBrush, 0, itemY, renderWidth, ItemHeight);

                    ChatItem item = _filteredChats[i];

                    // 1. Avatar (X=4, Y=itemY+5, W=32, H=32)
                    DrawAvatar(_offScreenGraphics, item, 4, itemY + 5);

                    // 2. Presence indicator over avatar (X=28, Y=itemY+27, W=8, H=8)
                    DrawPresenceStatus(_offScreenGraphics, item.Status, 28, itemY + 27);

                    // 3. Chat title (X=42, Y=itemY+5)
                    Brush titleBrush = isSelected ? BrushTextSelected : BrushText;
                    _offScreenGraphics.DrawString(item.Title, FontTitle, titleBrush, 42, itemY + 5);

                    // 4. Mute indicator
                    if (item.IsMuted)
                    {
                        Brush muteBrush = isSelected ? BrushSubTextSelected : BrushSubText;
                        _offScreenGraphics.DrawString("[Mute]", FontTime, muteBrush, 150, itemY + 5);
                    }

                    // 5. Last message time (right-aligned)
                    Brush timeBrush = isSelected ? BrushSubTextSelected : BrushSubText;
                    _offScreenGraphics.DrawString(item.TimeStr, FontTime, timeBrush, renderWidth - 36, itemY + 5);

                    // 6. Message preview or "typing..." status
                    if (item.IsTyping)
                    {
                        Brush typingBrush = isSelected ? BrushTypingSelected : BrushTyping;
                        string typingStr = L10n.TypingHeader + new string('.', _typingDots);
                        _offScreenGraphics.DrawString(typingStr, FontTyping, typingBrush, 42, itemY + 22);
                    }
                    else
                    {
                        Brush subTextBrush = isSelected ? BrushSubTextSelected : BrushSubText;
                        string msg = item.LastMessage ?? "";
                        if (msg.Length > 25) msg = msg.Substring(0, 24) + "...";
                        _offScreenGraphics.DrawString(msg, FontMessage, subTextBrush, 42, itemY + 22);
                    }

                    // 7. Unread count badge (X=renderWidth - 28)
                    if (item.UnreadCount > 0)
                    {
                        int badgeX = renderWidth - 26;
                        int badgeY = itemY + 20;
                        Brush badgeBg = item.IsMuted ? BrushBadgeMuted : BrushBadge;
                        _offScreenGraphics.FillRectangle(badgeBg, badgeX, badgeY, 22, 15);
                        _offScreenGraphics.DrawString(item.UnreadCount.ToString(), FontBadge, BrushBadgeText, badgeX + 6, badgeY + 1);
                    }

                    // 8. Bottom separator line
                    _offScreenGraphics.DrawLine(PenSeparator, 0, itemY + ItemHeight - 1, renderWidth, itemY + ItemHeight - 1);
                }
            }

            // BitBlt off-screen buffer to screen
            e.Graphics.DrawImage(_offScreenBitmap, 0, 0);
        }

        private static void DrawAvatar(Graphics g, ChatItem item, int x, int y)
        {
            // Deterministic avatar color based on ID and name hash
            int colorIdx = Math.Abs((int)(item.Id ^ (item.Title != null ? item.Title.GetHashCode() : 0))) % AvatarPalette.Length;
            Brush bgBrush = AvatarPalette[colorIdx];

            g.FillRectangle(bgBrush, x, y, 32, 32);
            g.DrawRectangle(PenAvatarBorder, x, y, 31, 31);

            // Initials
            string initial = "?";
            if (!string.IsNullOrEmpty(item.Title))
            {
                string[] words = item.Title.Trim().Split(' ');
                if (words.Length >= 2 && words[0].Length > 0 && words[1].Length > 0 && (item.Type == "group" || item.Type == "channel"))
                {
                    initial = (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpper();
                }
                else
                {
                    initial = item.Title.Substring(0, 1).ToUpper();
                }
            }

            Font fontToUse = (initial.Length > 1) ? FontGroupInitials : FontAvatar;
            SizeF initSize = g.MeasureString(initial, fontToUse);
            int letterX = x + (32 - (int)initSize.Width) / 2;
            int letterY = y + (32 - (int)initSize.Height) / 2;
            g.DrawString(initial, fontToUse, BrushBadgeText, letterX, letterY);

            // Group / channel mini-badge on bottom-right of avatar
            if (item.Type == "group" || item.Type == "channel")
            {
                int bx = x + 19;
                int by = y + 19;
                g.FillRectangle(BrushGroupBadgeBg, bx, by, 12, 12);
                g.DrawRectangle(PenGroupBadgeBorder, bx, by, 11, 11);

                if (item.IsForum)
                {
                    g.DrawString("#", FontGroupBadge, BrushBadgeText, bx + 1, by);
                }
                else
                {
                    g.FillEllipse(BrushBadgeText, bx + 2, by + 2, 3, 3);
                    g.FillEllipse(BrushBadgeText, bx + 6, by + 4, 3, 3);
                }
            }
        }

        private static void DrawPresenceStatus(Graphics g, PresenceStatus status, int x, int y)
        {
            Brush brush = null;
            if (status == PresenceStatus.Online) brush = BrushOnline;
            else if (status == PresenceStatus.Recently) brush = BrushRecently;
            else if (status == PresenceStatus.Offline) brush = BrushOffline;

            if (brush != null)
            {
                g.FillEllipse(brush, x, y, 8, 8);
                g.DrawEllipse(PenWhite, x, y, 8, 8);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int index = (e.Y + scrollVal) / ItemHeight;

            if (index >= 0 && index < _filteredChats.Count)
            {
                _selectedIndex = index;
                this.Invalidate();

                if (ChatSelected != null)
                {
                    ChatSelected(_filteredChats[_selectedIndex]);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_filteredChats.Count == 0) return;

            if (e.KeyCode == Keys.Up)
            {
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                    EnsureVisible(_selectedIndex);
                    this.Invalidate();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (_selectedIndex < _filteredChats.Count - 1)
                {
                    _selectedIndex++;
                    EnsureVisible(_selectedIndex);
                    this.Invalidate();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _filteredChats.Count && ChatSelected != null)
                {
                    ChatSelected(_filteredChats[_selectedIndex]);
                }
                e.Handled = true;
            }
        }

        private void EnsureVisible(int index)
        {
            if (_scrollBar == null || !_scrollBar.Enabled) return;
            int itemY = index * ItemHeight;
            if (itemY < _scrollBar.Value)
            {
                _scrollBar.Value = Math.Max(0, itemY);
            }
            else if (itemY + ItemHeight > _scrollBar.Value + this.Height)
            {
                _scrollBar.Value = Math.Min(_scrollBar.Maximum, itemY + ItemHeight - this.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_offScreenGraphics != null)
                {
                    _offScreenGraphics.Dispose();
                    _offScreenGraphics = null;
                }
                if (_offScreenBitmap != null)
                {
                    _offScreenBitmap.Dispose();
                    _offScreenBitmap = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
