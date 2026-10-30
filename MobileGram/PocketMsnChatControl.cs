using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MobileGram.Models;

namespace MobileGram.Controls
{
    public class PocketMsnChatControl : Control
    {
        private List<MessageItem> _messages = new List<MessageItem>();
        private VScrollBar _scrollBar;
        private Bitmap _offScreenBitmap;
        private Graphics _offScreenGraphics;

        // Touch/stylus dragging support
        private bool _isDragging = false;
        private int _dragStartY = 0;
        private int _dragStartScrollVal = 0;

        // Timer for Tap & Hold gesture
        private Timer _holdTimer;
        private int _holdX = 0;
        private int _holdY = 0;
        private MessageItem _heldMessage = null;

        // Message highlight upon reply jump
        private long _highlightedMsgId = 0;
        private Timer _highlightTimer;

        // Control events
        public event Action<long, string> AuthorProfileRequested;
        public event Action<string, string, long> ReplyRequested;
        public event Action<long, string> ReactionRequested;
        public event Action<long, string> MediaRequested;
        public event Action<long, string> BanUserRequested;

        public bool IsGroupChat { get; set; }

        // Cached GDI resources (static to avoid allocations in OnPaint)
        private static readonly Font FontNick = new Font("Tahoma", 8.25f, FontStyle.Bold);
        private static readonly Font FontText = new Font("Tahoma", 8.25f, FontStyle.Regular);
        private static readonly Font FontTime = new Font("Tahoma", 7.0f, FontStyle.Regular);
        private static readonly Font FontService = new Font("Tahoma", 7.0f, FontStyle.Regular);
        private static readonly Font FontReply = new Font("Tahoma", 7.0f, FontStyle.Regular);
        private static readonly Font FontReaction = new Font("Tahoma", 7.0f, FontStyle.Bold);
        private static readonly Font FontPollQuestion = new Font("Tahoma", 8.0f, FontStyle.Bold);
        private static readonly Font FontPollOption = new Font("Tahoma", 7.5f, FontStyle.Regular);
        private static readonly Font FontPollPercent = new Font("Tahoma", 7.0f, FontStyle.Bold);

        private static readonly Brush BrushBack = new SolidBrush(Color.White);
        private static readonly Brush BrushHighlight = new SolidBrush(Color.FromArgb(228, 240, 255));
        private static readonly Brush BrushSelfNick = new SolidBrush(Color.FromArgb(128, 0, 0));
        private static readonly Brush BrushOtherNick = new SolidBrush(Color.FromArgb(0, 51, 153));
        private static readonly Brush BrushOrangeNick = new SolidBrush(Color.FromArgb(184, 92, 0));
        private static readonly Brush BrushPurpleNick = new SolidBrush(Color.FromArgb(128, 0, 128));
        private static readonly Brush BrushGreenNick = new SolidBrush(Color.FromArgb(0, 102, 0));

        private static readonly Brush BrushText = new SolidBrush(Color.Black);
        private static readonly Brush BrushTime = new SolidBrush(Color.FromArgb(85, 85, 85));
        private static readonly Brush BrushTickSending = new SolidBrush(Color.FromArgb(140, 140, 140));

        private static readonly Brush BrushIconSelf = new SolidBrush(Color.FromArgb(0, 85, 170));
        private static readonly Brush BrushIconGreen = new SolidBrush(Color.FromArgb(0, 128, 0));
        private static readonly Brush BrushIconOrange = new SolidBrush(Color.FromArgb(184, 92, 0));
        private static readonly Brush BrushIconPurple = new SolidBrush(Color.FromArgb(128, 0, 128));

        // Reply and reactions
        private static readonly Brush BrushReplyText = new SolidBrush(Color.FromArgb(70, 100, 140));
        private static readonly Pen PenReplyBar = new Pen(Color.FromArgb(80, 130, 190));
        private static readonly Brush BrushReactionBg = new SolidBrush(Color.FromArgb(240, 244, 250));
        private static readonly Brush BrushReactionChosenBg = new SolidBrush(Color.FromArgb(215, 232, 255));
        private static readonly Brush BrushReactionText = new SolidBrush(Color.FromArgb(60, 80, 110));
        private static readonly Brush BrushReactionChosenText = new SolidBrush(Color.FromArgb(0, 60, 160));
        private static readonly Pen PenReactionBorder = new Pen(Color.FromArgb(200, 215, 235));
        private static readonly Pen PenReactionChosen = new Pen(Color.FromArgb(49, 106, 197));

        // Service messages and polls
        private static readonly Brush BrushServiceBg = new SolidBrush(Color.FromArgb(234, 239, 245));
        private static readonly Brush BrushServiceText = new SolidBrush(Color.FromArgb(70, 85, 105));
        private static readonly Pen PenServiceBorder = new Pen(Color.FromArgb(200, 214, 229));

        private static readonly Brush BrushPollBarBg = new SolidBrush(Color.FromArgb(235, 240, 246));
        private static readonly Brush BrushPollBarFill = new SolidBrush(Color.FromArgb(49, 106, 197));
        private static readonly Brush BrushPollBarVoted = new SolidBrush(Color.FromArgb(0, 150, 80));
        private static readonly Pen PenPollRadio = new Pen(Color.FromArgb(120, 140, 170));
        private static readonly Pen PenSeparator = new Pen(Color.FromArgb(232, 232, 232));
        private static readonly Pen PenTick = new Pen(Color.FromArgb(0, 85, 170));

        public PocketMsnChatControl()
        {
            this._scrollBar = new VScrollBar();
            this._scrollBar.Dock = DockStyle.Right;
            this._scrollBar.Width = 14;
            this._scrollBar.ValueChanged += new EventHandler(OnScrollChanged);
            this.Controls.Add(this._scrollBar);

            _holdTimer = new Timer();
            _holdTimer.Interval = 500;
            _holdTimer.Tick += new EventHandler(HoldTimer_Tick);

            _highlightTimer = new Timer();
            _highlightTimer.Interval = 1600;
            _highlightTimer.Tick += new EventHandler(HighlightTimer_Tick);

            this.Resize += new EventHandler(OnControlResize);
        }

        public void SetMessages(List<MessageItem> messages)
        {
            this._messages = messages ?? new List<MessageItem>();
            CalculateLayout();
            this.Invalidate();
        }

        public void RecalculateAndInvalidate()
        {
            CalculateLayout();
            this.Invalidate();
        }

        public void ScrollToBottom()
        {
            if (_scrollBar != null && _scrollBar.Enabled)
            {
                int maxVal = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
                if (maxVal > 0)
                {
                    _scrollBar.Value = maxVal;
                    this.Invalidate();
                }
            }
        }

        public void JumpToMessage(long msgId)
        {
            if (_messages == null || _messages.Count == 0) return;

            int targetScrollY = 0;
            bool found = false;

            for (int i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].MsgId == msgId)
                {
                    found = true;
                    break;
                }
                targetScrollY += _messages[i].ComputedHeight;
            }

            if (found && _scrollBar != null && _scrollBar.Enabled)
            {
                int maxVal = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
                if (targetScrollY > maxVal) targetScrollY = maxVal;
                if (targetScrollY < 0) targetScrollY = 0;

                _scrollBar.Value = targetScrollY;
                _highlightedMsgId = msgId;
                _highlightTimer.Enabled = true;
                this.Invalidate();
            }
        }

        private void HighlightTimer_Tick(object sender, EventArgs e)
        {
            _highlightTimer.Enabled = false;
            _highlightedMsgId = 0;
            this.Invalidate();
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
            CalculateLayout();
            this.Invalidate();
        }

        private void OnScrollChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void CalculateLayout()
        {
            int scrollWidth = (_scrollBar != null) ? _scrollBar.Width : 14;
            int renderWidth = this.Width - scrollWidth - 22;
            if (renderWidth <= 20) return;

            using (Graphics g = this.CreateGraphics())
            {
                int totalHeight = 0;
                for (int i = 0; i < _messages.Count; i++)
                {
                    MessageItem m = _messages[i];
                    int h = 0;

                    if (m.IsService)
                    {
                        h = 24;
                    }
                    else if (m.IsPoll)
                    {
                        h = 16 + 18;
                        h += m.PollOptions.Count * 28 + 6;
                    }
                    else
                    {
                        h = 16;

                        // Extra height for reply quote block
                        if (m.ReplyToMsgId > 0)
                        {
                            h += 14;
                        }

                        int textHeight = CalculateTextHeight(g, m.Text, renderWidth);
                        h += textHeight + 6;

                        // Extra height for reactions block
                        if (m.Reactions != null && m.Reactions.Count > 0)
                        {
                            h += 18;
                        }
                    }

                    m.ComputedHeight = h;
                    totalHeight += h;
                }

                if (_scrollBar != null)
                {
                    int maxScroll = totalHeight - this.Height;
                    if (maxScroll > 0)
                    {
                        _scrollBar.Enabled = true;
                        _scrollBar.Minimum = 0;
                        _scrollBar.SmallChange = 16;
                        _scrollBar.LargeChange = Math.Max(16, this.Height / 3);
                        _scrollBar.Maximum = maxScroll + _scrollBar.LargeChange - 1;
                    }
                    else
                    {
                        _scrollBar.Value = 0;
                        _scrollBar.Enabled = false;
                    }
                }
            }
        }

        private int CalculateTextHeight(Graphics g, string text, int renderWidth)
        {
            if (string.IsNullOrEmpty(text)) return 14;
            if (renderWidth <= 10) return 14;

            string[] rawLines = text.Split('\n');
            int lineCount = 0;
            for (int i = 0; i < rawLines.Length; i++)
            {
                string line = rawLines[i].TrimEnd('\r');
                if (line.Length == 0)
                {
                    lineCount++;
                    continue;
                }

                SizeF size = g.MeasureString(line, FontText);
                if (size.Width <= renderWidth)
                {
                    lineCount++;
                }
                else
                {
                    string[] words = line.Split(' ');
                    float currentLineWidth = 0;
                    int paraLines = 1;
                    for (int w = 0; w < words.Length; w++)
                    {
                        string word = (w == 0 ? "" : " ") + words[w];
                        SizeF wSize = g.MeasureString(word, FontText);
                        if (currentLineWidth + wSize.Width > renderWidth && currentLineWidth > 0)
                        {
                            paraLines++;
                            currentLineWidth = wSize.Width;
                        }
                        else
                        {
                            currentLineWidth += wSize.Width;
                        }
                    }
                    lineCount += paraLines;
                }
            }
            return lineCount * 14;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
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
            int startY = -scrollVal;
            int scrollWidth = (_scrollBar != null) ? _scrollBar.Width : 14;
            int renderWidth = this.Width - scrollWidth;

            for (int i = 0; i < _messages.Count; i++)
            {
                MessageItem msg = _messages[i];
                int itemHeight = msg.ComputedHeight;

                if (startY + itemHeight >= 0 && startY <= this.Height)
                {
                    // Highlight background when jumping to reply
                    if (msg.MsgId == _highlightedMsgId)
                    {
                        _offScreenGraphics.FillRectangle(BrushHighlight, 0, startY, renderWidth, itemHeight);
                    }

                    if (msg.IsService)
                    {
                        DrawServiceMessage(_offScreenGraphics, msg, startY, renderWidth);
                    }
                    else if (msg.IsPoll)
                    {
                        DrawPoll(_offScreenGraphics, msg, startY, renderWidth);
                    }
                    else
                    {
                        DrawNormalMessage(_offScreenGraphics, msg, startY, renderWidth, itemHeight);
                    }
                }

                startY += itemHeight;
            }

            e.Graphics.DrawImage(_offScreenBitmap, 0, 0);
        }

        private void DrawServiceMessage(Graphics g, MessageItem msg, int startY, int renderWidth)
        {
            SizeF size = g.MeasureString(msg.Text, FontService);
            int pillWidth = (int)size.Width + 16;
            int pillX = (renderWidth - pillWidth) / 2;
            int pillY = startY + 3;

            g.FillRectangle(BrushServiceBg, pillX, pillY, pillWidth, 18);
            g.DrawRectangle(PenServiceBorder, pillX, pillY, pillWidth, 18);
            g.DrawString(msg.Text, FontService, BrushServiceText, pillX + 8, pillY + 3);
        }

        private void DrawPoll(Graphics g, MessageItem msg, int startY, int renderWidth)
        {
            Brush iconBrush = GetIconBrush(msg);
            g.FillRectangle(iconBrush, 4, startY + 3, 10, 10);
            Brush nickBrush = GetNickBrush(msg);
            g.DrawString(msg.SenderName, FontNick, nickBrush, 18, startY);

            SizeF nickSize = g.MeasureString(msg.SenderName, FontNick);
            string timeHeader = "[" + msg.TimeStr + "] Опрос:";
            g.DrawString(timeHeader, FontTime, BrushTime, 18 + (int)nickSize.Width + 2, startY + 1);

            g.DrawString(msg.PollQuestion, FontPollQuestion, BrushText, 18, startY + 16);

            int totalVotes = 0;
            for (int v = 0; v < msg.PollVotes.Count; v++) totalVotes += msg.PollVotes[v];

            int optY = startY + 34;
            for (int o = 0; o < msg.PollOptions.Count; o++)
            {
                bool isMyVote = (msg.MyVoteIndex == o);
                int votes = msg.PollVotes[o];
                int pct = totalVotes > 0 ? (votes * 100 / totalVotes) : 0;

                g.DrawEllipse(PenPollRadio, 18, optY + 2, 10, 10);
                if (isMyVote)
                {
                    g.FillEllipse(BrushPollBarVoted, 20, optY + 4, 6, 6);
                }

                string optText = msg.PollOptions[o];
                g.DrawString(optText, FontPollOption, BrushText, 32, optY);

                int barWidth = renderWidth - 80;
                int fillWidth = barWidth * pct / 100;
                int barY = optY + 14;

                g.FillRectangle(BrushPollBarBg, 32, barY, barWidth, 6);
                if (fillWidth > 0)
                {
                    Brush barBrush = isMyVote ? BrushPollBarVoted : BrushPollBarFill;
                    g.FillRectangle(barBrush, 32, barY, fillWidth, 6);
                }

                string pctStr = pct + "%";
                g.DrawString(pctStr, FontPollPercent, BrushTime, 36 + barWidth, barY - 4);

                optY += 28;
            }

            g.DrawLine(PenSeparator, 0, startY + msg.ComputedHeight - 1, renderWidth, startY + msg.ComputedHeight - 1);
        }

        private void DrawNormalMessage(Graphics g, MessageItem msg, int startY, int renderWidth, int itemHeight)
        {
            // Sender icon
            Brush iconBrush = GetIconBrush(msg);
            g.FillRectangle(iconBrush, 4, startY + 3, 10, 10);

            // Sender name
            Brush nickBrush = GetNickBrush(msg);
            g.DrawString(msg.SenderName, FontNick, nickBrush, 18, startY);

            // Timestamp [HH:mm]:
            SizeF nickSize = g.MeasureString(msg.SenderName, FontNick);
            string timeHeader = "[" + msg.TimeStr + "]:";
            g.DrawString(timeHeader, FontTime, BrushTime, 18 + (int)nickSize.Width + 2, startY + 1);

            // Delivery ticks
            if (msg.IsSelf)
            {
                int tickX = renderWidth - 20;
                int tickY = startY + 3;
                if (msg.Status == DeliveryStatus.Read)
                {
                    DrawTick(g, tickX - 7, tickY, PenTick);
                    DrawTick(g, tickX, tickY, PenTick);
                }
                else if (msg.Status == DeliveryStatus.Sent)
                {
                    DrawTick(g, tickX, tickY, PenTick);
                }
                else
                {
                    g.DrawString("...", FontTime, BrushTickSending, tickX - 6, startY);
                }
            }

            int contentY = startY + 16;

            // 1. Render reply header, if present
            if (msg.ReplyToMsgId > 0)
            {
                string rawPreview = msg.ReplyToText ?? "";
                if (rawPreview.Length > 20) rawPreview = rawPreview.Substring(0, 20) + "...";
                string replyText = "↳ " + (string.IsNullOrEmpty(msg.ReplyToSender) ? "" : msg.ReplyToSender + ": ") + rawPreview;

                g.DrawLine(PenReplyBar, 18, contentY, 18, contentY + 11);
                g.DrawLine(PenReplyBar, 18, contentY + 11, 22, contentY + 11);
                g.DrawString(replyText, FontReply, BrushReplyText, 25, contentY);

                contentY += 14;
            }

            // 2. Render message text
            int textHeight = CalculateTextHeight(g, msg.Text, renderWidth - 22);
            RectangleF textRect = new RectangleF(18, contentY, renderWidth - 22, textHeight + 2);
            g.DrawString(msg.Text, FontText, BrushText, textRect);
            contentY += textHeight + 4;

            // 3. Render reactions
            if (msg.Reactions != null && msg.Reactions.Count > 0)
            {
                int rx = 18;
                int ry = contentY + 1;

                for (int r = 0; r < msg.Reactions.Count; r++)
                {
                    ReactionBadge badge = msg.Reactions[r];
                    string badgeText = badge.GetDisplayText();
                    SizeF bSize = g.MeasureString(badgeText, FontReaction);
                    int bw = (int)bSize.Width + 6;
                    int bh = 14;

                    if (rx + bw > renderWidth - 5) break;

                    g.FillRectangle(badge.Chosen ? BrushReactionChosenBg : BrushReactionBg, rx, ry, bw, bh);
                    g.DrawRectangle(badge.Chosen ? PenReactionChosen : PenReactionBorder, rx, ry, bw, bh);
                    g.DrawString(badgeText, FontReaction, badge.Chosen ? BrushReactionChosenText : BrushReactionText, rx + 3, ry + 1);

                    rx += bw + 4;
                }
            }

            // Bottom separator line
            g.DrawLine(PenSeparator, 0, startY + itemHeight - 1, renderWidth, startY + itemHeight - 1);
        }

        private static void DrawTick(Graphics g, int x, int y, Pen pen)
        {
            g.DrawLine(pen, x, y + 4, x + 2, y + 7);
            g.DrawLine(pen, x + 2, y + 7, x + 6, y + 1);
        }

        private static Brush GetNickBrush(MessageItem msg)
        {
            if (msg.IsSelf) return BrushSelfNick;
            if (msg.SenderColor.R == 184 && msg.SenderColor.G == 92) return BrushOrangeNick;
            if (msg.SenderColor.R == 128 && msg.SenderColor.G == 0 && msg.SenderColor.B == 128) return BrushPurpleNick;
            if (msg.SenderColor.R == 0 && msg.SenderColor.G == 102) return BrushGreenNick;
            return BrushOtherNick;
        }

        private static Brush GetIconBrush(MessageItem msg)
        {
            if (msg.IsSelf) return BrushIconSelf;
            if (msg.SenderColor.R == 184 && msg.SenderColor.G == 92) return BrushIconOrange;
            if (msg.SenderColor.R == 128 && msg.SenderColor.G == 0 && msg.SenderColor.B == 128) return BrushIconPurple;
            return BrushIconGreen;
        }

        // --- TOUCH & MOUSE EVENTS ---
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int scrollWidth = (_scrollBar != null) ? _scrollBar.Width : 14;

            if (e.X < this.Width - scrollWidth)
            {
                _isDragging = true;
                _dragStartY = e.Y;
                _dragStartScrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;

                _holdX = e.X;
                _holdY = e.Y;
                _heldMessage = FindMessageAt(e.Y);

                if (_heldMessage != null && !_heldMessage.IsService)
                {
                    _holdTimer.Enabled = true;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (Math.Abs(e.X - _holdX) > 6 || Math.Abs(e.Y - _holdY) > 6)
            {
                _holdTimer.Enabled = false;
            }

            if (_isDragging && _scrollBar != null && _scrollBar.Enabled)
            {
                int deltaY = e.Y - _dragStartY;
                int targetVal = _dragStartScrollVal - deltaY;
                int maxVal = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
                if (targetVal < 0) targetVal = 0;
                if (targetVal > maxVal) targetVal = maxVal;

                if (_scrollBar.Value != targetVal)
                {
                    _scrollBar.Value = targetVal;
                    this.Invalidate();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _holdTimer.Enabled = false;

            if (Math.Abs(e.Y - _dragStartY) < 4)
            {
                // Handle click on reply preview (jump to quoted message)
                if (HandleReplyClick(e.X, e.Y))
                {
                    return;
                }

                // Handle click on media / photo
                if (HandleMediaClick(e.X, e.Y))
                {
                    return;
                }

                // Handle click on poll option
                HandlePollClick(e.X, e.Y);
            }
        }

        private bool HandleReplyClick(int clientX, int clientY)
        {
            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int targetY = clientY + scrollVal;
            int currY = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                MessageItem m = _messages[i];
                if (m.ReplyToMsgId > 0 && targetY >= currY && targetY <= currY + m.ComputedHeight)
                {
                    // Reply block bounds: currY + 16 to currY + 30
                    if (targetY >= currY + 14 && targetY <= currY + 32)
                    {
                        JumpToMessage(m.ReplyToMsgId);
                        return true;
                    }
                }
                currY += m.ComputedHeight;
            }
            return false;
        }

        private bool HandleMediaClick(int clientX, int clientY)
        {
            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int targetY = clientY + scrollVal;
            int currY = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                MessageItem m = _messages[i];
                if (targetY >= currY && targetY <= currY + m.ComputedHeight)
                {
                    bool isMedia = m.HasMedia || (!string.IsNullOrEmpty(m.Text) && (m.Text.IndexOf("[Фото") >= 0 || m.Text.IndexOf("[Файл") >= 0));
                    if (isMedia && MediaRequested != null)
                    {
                        MediaRequested(m.MsgId, string.IsNullOrEmpty(m.MediaType) ? "photo" : m.MediaType);
                        return true;
                    }
                }
                currY += m.ComputedHeight;
            }
            return false;
        }

        private void HoldTimer_Tick(object sender, EventArgs e)
        {
            _holdTimer.Enabled = false;

            if (_heldMessage != null && !_heldMessage.IsService)
            {
                ShowMessageContextMenu(_heldMessage, _holdX, _holdY);
            }
        }

        private MessageItem FindMessageAt(int clientY)
        {
            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int targetY = clientY + scrollVal;
            int currY = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                int h = _messages[i].ComputedHeight;
                if (targetY >= currY && targetY <= currY + h)
                {
                    return _messages[i];
                }
                currY += h;
            }
            return null;
        }

        private void HandlePollClick(int clientX, int clientY)
        {
            int scrollVal = (_scrollBar != null && _scrollBar.Enabled) ? _scrollBar.Value : 0;
            int targetY = clientY + scrollVal;
            int currY = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                MessageItem m = _messages[i];
                if (m.IsPoll && targetY >= currY && targetY <= currY + m.ComputedHeight)
                {
                    int optStartY = currY + 34;
                    for (int o = 0; o < m.PollOptions.Count; o++)
                    {
                        if (targetY >= optStartY && targetY <= optStartY + 28)
                        {
                            if (m.MyVoteIndex != o)
                            {
                                if (m.MyVoteIndex >= 0 && m.MyVoteIndex < m.PollVotes.Count)
                                {
                                    m.PollVotes[m.MyVoteIndex] = Math.Max(0, m.PollVotes[m.MyVoteIndex] - 1);
                                }
                                m.MyVoteIndex = o;
                                m.PollVotes[o]++;
                                this.Invalidate();
                            }
                            return;
                        }
                        optStartY += 28;
                    }
                    return;
                }
                currY += m.ComputedHeight;
            }
        }

        private void ShowMessageContextMenu(MessageItem msg, int x, int y)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem mnuReply = new MenuItem { Text = L10n.Reply };
            mnuReply.Click += new EventHandler(delegate {
                if (ReplyRequested != null) ReplyRequested(msg.SenderName, msg.Text, msg.MsgId);
            });
            menu.MenuItems.Add(mnuReply);

            bool hasMedia = msg.HasMedia || (!string.IsNullOrEmpty(msg.Text) && (msg.Text.IndexOf("[Фото") >= 0 || msg.Text.IndexOf("[Файл") >= 0));
            if (hasMedia)
            {
                MenuItem mnuMedia = new MenuItem { Text = L10n.ViewMedia };
                mnuMedia.Click += new EventHandler(delegate {
                    if (MediaRequested != null) MediaRequested(msg.MsgId, msg.MediaType);
                });
                menu.MenuItems.Add(mnuMedia);
            }

            // Reactions context menu
            MenuItem mnuReactions = new MenuItem { Text = L10n.Reaction };
            string likeText = L10n.IsRussian ? "👍 Лайк (+1)" : "👍 Like (+1)";
            string heartText = L10n.IsRussian ? "❤️ Сердечко (<3)" : "❤️ Heart (<3)";
            string fireText = L10n.IsRussian ? "🔥 Огонь (TOP)" : "🔥 Fire (TOP)";
            string laughText = L10n.IsRussian ? "😂 Смех (XD)" : "😂 Laugh (XD)";
            string dislikeText = L10n.IsRussian ? "👎 Дизлайк (-1)" : "👎 Dislike (-1)";

            AddReactionItem(mnuReactions, msg.MsgId, likeText, "👍");
            AddReactionItem(mnuReactions, msg.MsgId, heartText, "❤️");
            AddReactionItem(mnuReactions, msg.MsgId, fireText, "🔥");
            AddReactionItem(mnuReactions, msg.MsgId, laughText, "😂");
            AddReactionItem(mnuReactions, msg.MsgId, dislikeText, "👎");
            AddReactionItem(mnuReactions, msg.MsgId, L10n.RemoveReaction, "");
            menu.MenuItems.Add(mnuReactions);

            MenuItem mnuProfile = new MenuItem { Text = L10n.Profile + ": " + msg.SenderName };
            mnuProfile.Click += new EventHandler(delegate {
                if (AuthorProfileRequested != null) AuthorProfileRequested(msg.SenderId, msg.SenderName);
            });
            menu.MenuItems.Add(mnuProfile);

            MenuItem mnuCopy = new MenuItem { Text = L10n.CopyText };
            mnuCopy.Click += new EventHandler(delegate {
                Clipboard.SetDataObject(msg.Text);
            });
            menu.MenuItems.Add(mnuCopy);

            if (IsGroupChat && !msg.IsSelf)
            {
                MenuItem mnuBan = new MenuItem { Text = L10n.BanUser };
                mnuBan.Click += new EventHandler(delegate {
                    if (BanUserRequested != null) BanUserRequested(msg.SenderId, msg.SenderName);
                });
                menu.MenuItems.Add(mnuBan);
            }

            menu.Show(this, new Point(x, y));
        }

        private void AddReactionItem(MenuItem parentMenu, long msgId, string label, string emoji)
        {
            MenuItem item = new MenuItem { Text = label };
            item.Click += new EventHandler(delegate {
                if (ReactionRequested != null) ReactionRequested(msgId, emoji);
            });
            parentMenu.MenuItems.Add(item);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_scrollBar != null && _scrollBar.Enabled)
            {
                int maxVal = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
                if (e.KeyCode == Keys.Up)
                {
                    _scrollBar.Value = Math.Max(0, _scrollBar.Value - _scrollBar.SmallChange);
                    this.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    _scrollBar.Value = Math.Min(maxVal, _scrollBar.Value + _scrollBar.SmallChange);
                    this.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp)
                {
                    _scrollBar.Value = Math.Max(0, _scrollBar.Value - _scrollBar.LargeChange);
                    this.Invalidate();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    _scrollBar.Value = Math.Min(maxVal, _scrollBar.Value + _scrollBar.LargeChange);
                    this.Invalidate();
                    e.Handled = true;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_holdTimer != null)
                {
                    _holdTimer.Enabled = false;
                    _holdTimer.Dispose();
                }
                if (_highlightTimer != null)
                {
                    _highlightTimer.Enabled = false;
                    _highlightTimer.Dispose();
                }
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
