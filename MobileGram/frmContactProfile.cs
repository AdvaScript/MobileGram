using System;
using System.Drawing;
using System.Windows.Forms;
using MobileGram.Models;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmContactProfile : Form
    {
        private ChatItem _chat;
        private long _senderId = 0;
        private string _displayName = "Пользователь";

        private static readonly Font FontBigInitial = new Font("Tahoma", 22.0f, FontStyle.Bold);
        private static readonly Brush BrushWhite = new SolidBrush(Color.White);
        private static readonly Pen PenBorder = new Pen(Color.FromArgb(120, 120, 120));

        public frmContactProfile(ChatItem chat)
        {
            InitializeComponent();
            _chat = chat;
            if (_chat != null)
            {
                _displayName = _chat.Title;
                _senderId = _chat.Id;
            }
            LoadProfile();
        }

        public frmContactProfile(long chatId)
        {
            InitializeComponent();
            _senderId = chatId;
            _chat = GatewayClient.Instance.GetCachedChat(chatId);
            if (_chat != null)
            {
                _displayName = _chat.Title;
            }
            else
            {
                _displayName = (L10n.IsRussian ? "Пользователь " : "User ") + chatId;
            }
            LoadProfile();
        }

        public frmContactProfile(long senderId, string senderName)
        {
            InitializeComponent();
            _senderId = senderId;
            _displayName = string.IsNullOrEmpty(senderName) ? ("ID: " + senderId) : senderName;
            _chat = GatewayClient.Instance.GetCachedChat(senderId);
            LoadProfile();
        }

        private void LoadProfile()
        {
            menuBack.Text = L10n.Back;
            menuOk.Text = L10n.Ok;

            this.Text = L10n.Profile + ": " + _displayName;
            lblDisplayName.Text = _displayName;

            if (_chat != null)
            {
                pnlAvatar.BackColor = _chat.AvatarColor;

                if (_chat.Type == "group" || _chat.Type == "channel")
                {
                    string grpType = _chat.IsForum ? (L10n.Forum + " (") : (L10n.Group + " (");
                    lblPresence.Text = grpType + _chat.MembersCount + " " + L10n.Members + ")";
                    lblPresence.ForeColor = Color.FromArgb(0, 102, 204);
                    lblUsername.Text = string.IsNullOrEmpty(_chat.Username) ? "ID: " + _chat.Id : "@" + _chat.Username;
                }
                else if (_chat.Status == PresenceStatus.Online)
                {
                    lblPresence.Text = L10n.Online;
                    lblPresence.ForeColor = Color.FromArgb(0, 128, 0);
                    lblUsername.Text = string.IsNullOrEmpty(_chat.Username) ? "ID: " + _chat.Id : "@" + _chat.Username;
                }
                else if (_chat.Status == PresenceStatus.Recently)
                {
                    lblPresence.Text = L10n.Recently;
                    lblPresence.ForeColor = Color.FromArgb(217, 119, 6);
                    lblUsername.Text = string.IsNullOrEmpty(_chat.Username) ? "ID: " + _chat.Id : "@" + _chat.Username;
                }
                else
                {
                    lblPresence.Text = L10n.Offline;
                    lblPresence.ForeColor = Color.FromArgb(112, 112, 112);
                    lblUsername.Text = string.IsNullOrEmpty(_chat.Username) ? "ID: " + _chat.Id : "@" + _chat.Username;
                }
            }
            else
            {
                pnlAvatar.BackColor = Color.FromArgb(0, 102, 204);
                lblPresence.Text = L10n.IsRussian ? "Участник чата" : "Chat member";
                lblPresence.ForeColor = Color.FromArgb(112, 112, 112);
                lblUsername.Text = "ID: " + _senderId;
            }

            // Profile details table
            lstDetails.Items.Clear();

            AddDetail(L10n.IsRussian ? "Статус" : "Status", lblPresence.Text);
            if (!string.IsNullOrEmpty(lblUsername.Text))
            {
                AddDetail(L10n.IsRussian ? "Имя пользователя" : "Username", lblUsername.Text);
            }
            if (_chat != null)
            {
                string typeLabel = L10n.IsRussian ? "Тип" : "Type";
                if (_chat.IsForum)
                {
                    AddDetail(typeLabel, L10n.IsRussian ? "Группа с темами (Форум)" : "Forum Group");
                }
                else if (_chat.Type == "group")
                {
                    AddDetail(typeLabel, L10n.Group);
                }
                else
                {
                    AddDetail(typeLabel, L10n.DirectChat);
                }

                bool isMuted = _chat.IsMuted || ConfigManager.IsChatMuted(_chat.Id);
                AddDetail(L10n.IsRussian ? "Уведомления" : "Notifications", isMuted ? L10n.NotificationsOff : L10n.NotificationsOn);
            }
        }

        private void AddDetail(string name, string val)
        {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(val);
            lstDetails.Items.Add(item);
        }

        private void pnlAvatar_Paint(object sender, PaintEventArgs e)
        {
            Color bg = (_chat != null) ? _chat.AvatarColor : Color.FromArgb(0, 102, 204);
            using (Brush b = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(b, 0, 0, pnlAvatar.Width, pnlAvatar.Height);
            }
            e.Graphics.DrawRectangle(PenBorder, 0, 0, pnlAvatar.Width - 1, pnlAvatar.Height - 1);

            string title = (_chat != null && !string.IsNullOrEmpty(_chat.Title)) ? _chat.Title : _displayName;
            string letter = (!string.IsNullOrEmpty(title)) ? title.Substring(0, 1).ToUpper() : "?";
            e.Graphics.DrawString(letter, FontBigInitial, BrushWhite, 16, 12);
        }

        private void menuBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
