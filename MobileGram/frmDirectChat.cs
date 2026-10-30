using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsCE.Forms;
using MobileGram.Models;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmDirectChat : Form
    {
        private long _chatId;
        private ChatItem _chat;
        private TopicItem _topic;
        private List<MessageItem> _messages = new List<MessageItem>();

        // Quoted reply message
        private long _replyToMsgId = 0;
        private string _replyToSender = "";
        private string _replyToText = "";

        // Typing animation timer
        private System.Windows.Forms.Timer _typingTimer;
        private int _typingFrame = 0;

        // Demo timer
        private System.Windows.Forms.Timer _replyTimer;

        // System notification
        private Notification _sysNotification;

        // Cached header GDI resources
        private static readonly Font FontHeader = new Font("Tahoma", 8.25f, FontStyle.Bold);
        private static readonly Brush BrushHeaderText = new SolidBrush(Color.Black);
        private static readonly Brush BrushOnline = new SolidBrush(Color.FromArgb(0, 170, 0));
        private static readonly Brush BrushRecently = new SolidBrush(Color.FromArgb(255, 153, 0));
        private static readonly Brush BrushOffline = new SolidBrush(Color.FromArgb(160, 160, 160));
        private static readonly Brush BrushGroupIcon = new SolidBrush(Color.FromArgb(0, 102, 204));
        private static readonly Brush BrushTopicIcon = new SolidBrush(Color.FromArgb(255, 140, 0));
        private static readonly Brush BrushTypingDot = new SolidBrush(Color.FromArgb(0, 85, 170));

        private static readonly Pen PenHeaderBorder = new Pen(Color.FromArgb(153, 187, 232));
        private static readonly Pen PenBorderLight = new Pen(Color.FromArgb(208, 208, 208));
        private static readonly Pen PenWhite = new Pen(Color.White);

        private bool _isWindowActive = true;

        public frmDirectChat(ChatItem chat, TopicItem topic)
        {
            InitializeComponent();
            _chat = chat;
            _topic = topic;
            _chatId = (chat != null) ? chat.Id : 0;

            frmChatList.ActiveChatId = _chatId;
            this.Activated += delegate { _isWindowActive = true; frmChatList.ActiveChatId = _chatId; };
            this.Deactivate += delegate { _isWindowActive = false; };

            chatControl.IsGroupChat = (_chat != null && (_chat.Type == "group" || _chat.Type == "channel"));
            chatControl.AuthorProfileRequested += new Action<long, string>(OnAuthorProfileRequested);
            chatControl.ReplyRequested += new Action<string, string, long>(OnReplyRequested);
            chatControl.ReactionRequested += new Action<long, string>(OnReactionRequested);
            chatControl.MediaRequested += new Action<long, string>(OnMediaRequested);
            chatControl.BanUserRequested += new Action<long, string>(OnBanUserRequested);

            GatewayClient.Instance.NewMessageReceived += new Action<long, MessageItem>(OnGatewayNewMessage);
            GatewayClient.Instance.UserTypingReceived += new Action<long, long>(OnGatewayUserTyping);
            GatewayClient.Instance.MessagesReadReceived += new Action<long, long>(OnGatewayMessagesRead);

            InitChatView();
            BuildOptionsMenu();
            InitTimers();
        }

        public frmDirectChat(long chatId)
            : this(GatewayClient.Instance.GetCachedChat(chatId) ?? new ChatItem { Id = chatId, Title = "ID: " + chatId }, null)
        {
        }

        private void InitChatView()
        {
            if (_chat == null)
            {
                _chat = new ChatItem { Id = _chatId, Title = "Диалог " + _chatId };
            }

            string displayTitle = GetDisplayTitle();
            this.Text = displayTitle;

            pnlTyping.Visible = _chat.IsTyping;

            // Load messages (with topic filter if applicable)
            long topicId = (_topic != null) ? _topic.Id : 0;
            List<MessageItem> remote = GatewayClient.Instance.FetchMessages(_chatId, 35, topicId);
            if (remote != null && remote.Count > 0)
            {
                _messages = remote;
            }
            else
            {
                _messages = new List<MessageItem>();
            }

            chatControl.SetMessages(_messages);
            chatControl.ScrollToBottom();

            // Mark messages as read so peer sees double checkmarks
            long maxIncomingId = 0;
            for (int i = 0; i < _messages.Count; i++)
            {
                if (!_messages[i].IsSelf && _messages[i].MsgId > maxIncomingId)
                    maxIncomingId = _messages[i].MsgId;
            }
            if (maxIncomingId > 0)
            {
                new Thread(delegate() { GatewayClient.Instance.MarkAsRead(_chatId, maxIncomingId); }).Start();
            }
        }

        private string GetDisplayTitle()
        {
            string t = (_chat != null) ? _chat.Title : ("Диалог " + _chatId);
            if (_topic != null)
            {
                t += " #" + _topic.Title;
            }
            else if (_chat != null && _chat.Type == "group")
            {
                t += " (" + _chat.MembersCount + ")";
            }
            return t;
        }

        private void InitTimers()
        {
            _typingTimer = new System.Windows.Forms.Timer();
            _typingTimer.Interval = 350;
            _typingTimer.Tick += new EventHandler(typingTimer_Tick);

            if (_chat != null && _chat.IsTyping)
            {
                _typingTimer.Enabled = true;
            }

            _replyTimer = new System.Windows.Forms.Timer();
            _replyTimer.Tick += new EventHandler(replyTimer_Tick);
        }

        private void BuildOptionsMenu()
        {
            menuOptions.MenuItems.Clear();

            menuBack.Text = L10n.Back;
            btnSend.Text = L10n.Send;
            menuOptions.Text = L10n.Options;

            bool isMuted = (_chat != null && _chat.IsMuted) || ConfigManager.IsChatMuted(_chatId);

            if (_chat != null && (_chat.Type == "group" || _chat.Type == "channel"))
            {
                MenuItem mInfo = new MenuItem { Text = L10n.Profile };
                mInfo.Click += new EventHandler(menuProfile_Click);
                menuOptions.MenuItems.Add(mInfo);

                MenuItem mMute = new MenuItem { Text = isMuted ? L10n.Unmute : L10n.Mute };
                mMute.Click += new EventHandler(menuMute_Click);
                menuOptions.MenuItems.Add(mMute);

                MenuItem mClear = new MenuItem { Text = L10n.ClearHistory };
                mClear.Click += new EventHandler(menuClear_Click);
                menuOptions.MenuItems.Add(mClear);

                MenuItem mAbout = new MenuItem { Text = L10n.About };
                mAbout.Click += new EventHandler(menuAbout_Click);
                menuOptions.MenuItems.Add(mAbout);
            }
            else
            {
                MenuItem mnuProfile = new MenuItem { Text = L10n.Profile };
                mnuProfile.Click += new EventHandler(menuProfile_Click);
                menuOptions.MenuItems.Add(mnuProfile);

                MenuItem mnuMute = new MenuItem { Text = isMuted ? L10n.Unmute : L10n.Mute };
                mnuMute.Click += new EventHandler(menuMute_Click);
                menuOptions.MenuItems.Add(mnuMute);

                MenuItem mnuClear = new MenuItem { Text = L10n.ClearHistory };
                mnuClear.Click += new EventHandler(menuClear_Click);
                menuOptions.MenuItems.Add(mnuClear);

                MenuItem mnuAbout = new MenuItem { Text = L10n.About };
                mnuAbout.Click += new EventHandler(menuAbout_Click);
                menuOptions.MenuItems.Add(mnuAbout);
            }
        }

        private void menuProfile_Click(object sender, EventArgs e)
        {
            if (_chat == null) return;
            frmContactProfile profile = new frmContactProfile(_chat);
            profile.ShowDialog();
        }

        private void menuMute_Click(object sender, EventArgs e)
        {
            if (_chat == null) return;
            _chat.IsMuted = !_chat.IsMuted;
            ConfigManager.SetChatMuted(_chatId, _chat.IsMuted);
            BuildOptionsMenu();

            // Send request to Telegram bridge
            new Thread(delegate() {
                GatewayClient.Instance.SetChatMute(_chatId, _chat.IsMuted);
            }).Start();

            string msg = L10n.IsRussian
                ? (_chat.IsMuted ? "Уведомления отключены." : "Уведомления включены.")
                : (_chat.IsMuted ? "Notifications muted." : "Notifications enabled.");
            MessageBox.Show(msg, L10n.Settings);
        }

        private void menuClear_Click(object sender, EventArgs e)
        {
            string confirmMsg = L10n.IsRussian ? "Очистить историю сообщений?" : "Clear message history?";
            string confirmTitle = L10n.IsRussian ? "Подтверждение" : "Confirmation";
            if (MessageBox.Show(confirmMsg, confirmTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                _messages.Clear();
                chatControl.SetMessages(_messages);
                chatControl.ScrollToBottom();
            }
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(frmChatList.GetSystemAboutInfo(), L10n.About);
        }

        // --- CHAT CONTROL EVENTS ---
        private void OnAuthorProfileRequested(long senderId, string senderName)
        {
            frmContactProfile profile = new frmContactProfile(senderId, senderName);
            profile.ShowDialog();
        }

        private void OnReplyRequested(string author, string text, long msgId)
        {
            _replyToMsgId = msgId;
            _replyToSender = author;
            _replyToText = text;

            string prev = text ?? "";
            if (prev.Length > 20) prev = prev.Substring(0, 20) + "...";
            lblReplyPreview.Text = "↳ Ответ для " + author + ": " + prev;
            pnlReplyBar.Visible = true;

            txtMessage.Focus();
            inputPanel1.Enabled = true;
        }

        private void btnCancelReply_Click(object sender, EventArgs e)
        {
            _replyToMsgId = 0;
            _replyToSender = "";
            _replyToText = "";
            pnlReplyBar.Visible = false;
        }

        private void OnReactionRequested(long msgId, string emoji)
        {
            // Update reaction locally for instant feedback
            for (int i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].MsgId == msgId)
                {
                    MessageItem m = _messages[i];
                    if (m.Reactions == null) m.Reactions = new List<ReactionBadge>();

                    // Clear previous user selection
                    for (int r = 0; r < m.Reactions.Count; r++)
                    {
                        if (m.Reactions[r].Chosen)
                        {
                            m.Reactions[r].Chosen = false;
                            m.Reactions[r].Count = Math.Max(0, m.Reactions[r].Count - 1);
                        }
                    }
                    m.Reactions.RemoveAll(x => x.Count <= 0);

                    if (!string.IsNullOrEmpty(emoji))
                    {
                        ReactionBadge existing = m.Reactions.Find(x => x.Emoji == emoji);
                        if (existing != null)
                        {
                            existing.Count++;
                            existing.Chosen = true;
                        }
                        else
                        {
                            m.Reactions.Add(new ReactionBadge { Emoji = emoji, Count = 1, Chosen = true });
                        }
                    }

                    chatControl.RecalculateAndInvalidate();
                    break;
                }
            }

            // Send reaction to Telegram bridge in background
            new Thread(delegate() {
                GatewayClient.Instance.SendReaction(_chatId, msgId, emoji);
            }).Start();
        }

        private void OnBanUserRequested(long senderId, string senderName)
        {
            string prompt = L10n.IsRussian
                ? string.Format("Заблокировать участника {0} и исключить из группы?", senderName)
                : string.Format("Ban user {0} and remove from group?", senderName);
            string title = L10n.IsRussian ? "Модерация группы" : "Group Moderation";
            if (MessageBox.Show(prompt, title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                string adminName = string.IsNullOrEmpty(ConfigManager.UserName) ? (L10n.IsRussian ? "Вы" : "You") : ConfigManager.UserName;
                string banText = L10n.IsRussian
                    ? string.Format("{0} исключил(а) {1} из группы", adminName, senderName)
                    : string.Format("{0} removed {1} from group", adminName, senderName);

                MessageItem banMsg = MessageItem.CreateService(banText, DateTime.Now.ToString("HH:mm"));
                _messages.Add(banMsg);
                chatControl.SetMessages(_messages);
                chatControl.ScrollToBottom();
            }
        }

        // --- INCOMING MESSAGES FROM TELEGRAM BRIDGE ---
        private void OnGatewayNewMessage(long chatId, MessageItem msg)
        {
            if (chatId != _chatId) return;

            // Filter messages by topic if viewing a specific forum topic
            if (_topic != null && msg.TopicId != 0 && msg.TopicId != _topic.Id)
            {
                return;
            }

            this.Invoke((Action)delegate {
                _messages.Add(msg);
                chatControl.SetMessages(_messages);
                chatControl.ScrollToBottom();

                if (!_isWindowActive)
                {
                    bool isMuted = (_chat != null && _chat.IsMuted) || ConfigManager.IsChatMuted(_chatId);
                    if (!isMuted)
                    {
                        ShowSystemNotification(msg.SenderName, msg.Text);
                    }
                }
                else
                {
                    if (!msg.IsSelf && msg.MsgId > 0)
                    {
                        new Thread(delegate() { GatewayClient.Instance.MarkAsRead(_chatId, msg.MsgId); }).Start();
                    }
                }
            });
        }

        private void OnGatewayUserTyping(long chatId, long userId)
        {
            if (chatId != _chatId) return;

            this.Invoke((Action)delegate {
                if (_chat != null) _chat.IsTyping = true;
                lblTyping.Text = (_chat != null ? _chat.Title : "Собеседник") + " печатает...";
                pnlTyping.Visible = true;
                _typingTimer.Enabled = true;
            });
        }

        private void OnGatewayMessagesRead(long chatId, long maxId)
        {
            if (chatId != _chatId) return;

            this.Invoke((Action)delegate {
                bool changed = false;
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].IsSelf && _messages[i].MsgId <= maxId && _messages[i].Status != DeliveryStatus.Read)
                    {
                        _messages[i].Status = DeliveryStatus.Read;
                        changed = true;
                    }
                }
                if (changed)
                {
                    chatControl.Invalidate();
                }
            });
        }

        private void OnMediaRequested(long msgId, string mediaType)
        {
            frmMediaViewer viewer = new frmMediaViewer(_chatId, msgId);
            viewer.ShowDialog();
        }

        private void ShowSystemNotification(string senderName, string text)
        {
            try
            {
                if (_sysNotification == null)
                {
                    _sysNotification = new Notification();
                    _sysNotification.Caption = "MobileGram";
                }
                _sysNotification.Text = string.Format("<b>{0}</b>: {1}", senderName, text);
                _sysNotification.InitialDuration = 4;
                _sysNotification.Visible = true;
            }
            catch { }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage()
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            txtMessage.Text = "";

            long sendReplyId = _replyToMsgId;
            string sendReplySender = _replyToSender;
            string sendReplyText = _replyToText;
            long sendTopicId = (_topic != null) ? _topic.Id : 0;

            // Hide reply bar
            _replyToMsgId = 0;
            _replyToSender = "";
            _replyToText = "";
            pnlReplyBar.Visible = false;

            MessageItem newMsg = new MessageItem(
                DateTime.Now.Ticks,
                0,
                "Вы",
                true,
                DateTime.Now.ToString("HH:mm"),
                text,
                DeliveryStatus.Sending,
                Color.FromArgb(128, 0, 0)
            );
            newMsg.ReplyToMsgId = sendReplyId;
            newMsg.ReplyToSender = sendReplySender;
            newMsg.ReplyToText = sendReplyText;
            newMsg.TopicId = sendTopicId;

            _messages.Add(newMsg);
            chatControl.SetMessages(_messages);
            chatControl.ScrollToBottom();

            // Mark messages as read so peer sees double checkmarks
            long maxIncomingId = 0;
            for (int i = 0; i < _messages.Count; i++)
            {
                if (!_messages[i].IsSelf && _messages[i].MsgId > maxIncomingId)
                    maxIncomingId = _messages[i].MsgId;
            }
            if (maxIncomingId > 0)
            {
                new Thread(delegate() { GatewayClient.Instance.MarkAsRead(_chatId, maxIncomingId); }).Start();
            }

            // Dispatch via GatewayClient in background
            new Thread(delegate() {
                bool ok = GatewayClient.Instance.SendMessage(_chatId, text, sendTopicId, sendReplyId);
                this.Invoke((Action)delegate {
                    newMsg.Status = ok ? DeliveryStatus.Sent : DeliveryStatus.Sending;
                    chatControl.Invalidate();
                });
            }).Start();
        }

        private void menuBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pnlHeader_Click(object sender, EventArgs e)
        {
            if (_chat == null) return;
            frmContactProfile profile = new frmContactProfile(_chat);
            profile.ShowDialog();
        }

        private void pnlHeader_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawRectangle(PenHeaderBorder, 0, 0, pnlHeader.Width - 1, pnlHeader.Height - 1);

            int x = 6;
            int y = 5;

            // Topic / presence status indicator
            if (_topic != null)
            {
                Color tColor = _topic.GetColor();
                using (Brush tb = new SolidBrush(tColor))
                {
                    g.FillRectangle(tb, x, y, 10, 10);
                }
                g.DrawRectangle(PenWhite, x, y, 10, 10);
            }
            else if (_chat != null && (_chat.Type == "group" || _chat.Type == "channel"))
            {
                g.FillRectangle(BrushGroupIcon, x, y, 10, 10);
                g.DrawRectangle(PenWhite, x, y, 10, 10);
            }
            else if (_chat != null && _chat.Status == PresenceStatus.Online)
            {
                g.FillEllipse(BrushOnline, x, y, 9, 9);
            }
            else if (_chat != null && _chat.Status == PresenceStatus.Recently)
            {
                g.FillEllipse(BrushRecently, x, y, 9, 9);
            }
            else
            {
                g.FillEllipse(BrushOffline, x, y, 9, 9);
            }

            x += 16;
            string title = GetDisplayTitle();
            g.DrawString(title, FontHeader, BrushHeaderText, x, 3);
        }

        private void pnlInput_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(PenBorderLight, 0, 0, pnlInput.Width, 0);
        }

        private void pnlTyping_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int dotY = 7;
            for (int i = 0; i < 3; i++)
            {
                int dotX = 8 + i * 5;
                int offset = (_typingFrame == i) ? -2 : 0;
                g.FillRectangle(BrushTypingDot, dotX, dotY + offset, 3, 3);
            }
        }

        private void typingTimer_Tick(object sender, EventArgs e)
        {
            _typingFrame = (_typingFrame + 1) % 3;
            pnlTyping.Invalidate();
        }

        private void replyTimer_Tick(object sender, EventArgs e)
        {
            _replyTimer.Enabled = false;
        }

        private void txtMessage_GotFocus(object sender, EventArgs e)
        {
            inputPanel1.Enabled = true;
        }

        private void inputPanel1_EnabledChanged(object sender, EventArgs e)
        {
            if (inputPanel1.Enabled)
            {
                this.Height = 268 - inputPanel1.Bounds.Height;
            }
            else
            {
                this.Height = 268;
            }
            chatControl.ScrollToBottom();
        }

        private void frmDirectChat_Closed(object sender, EventArgs e)
        {
            frmChatList.ActiveChatId = 0;
            GatewayClient.Instance.NewMessageReceived -= OnGatewayNewMessage;
            GatewayClient.Instance.UserTypingReceived -= OnGatewayUserTyping;
            GatewayClient.Instance.MessagesReadReceived -= OnGatewayMessagesRead;
            chatControl.MediaRequested -= OnMediaRequested;

            if (_typingTimer != null)
            {
                _typingTimer.Enabled = false;
                _typingTimer.Dispose();
            }
            if (_replyTimer != null)
            {
                _replyTimer.Enabled = false;
                _replyTimer.Dispose();
            }
            if (_sysNotification != null)
            {
                _sysNotification.Visible = false;
                _sysNotification.Dispose();
            }
        }
    }
}
