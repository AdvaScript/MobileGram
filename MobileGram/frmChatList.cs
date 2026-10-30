using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MobileGram.Models;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmChatList : Form
    {
        public static long ActiveChatId = 0;

        private static readonly Font FontFilterBold = new Font("Tahoma", 7.5f, FontStyle.Bold);
        private static readonly Font FontFilterRegular = new Font("Tahoma", 7.5f, FontStyle.Regular);

        private Timer _typingTimer;
        private string _activeFilter = "all";
        private List<ChatItem> _currentChats = new List<ChatItem>();
        private Microsoft.WindowsCE.Forms.Notification _globalNotification;

        public frmChatList()
        {
            InitializeComponent();
            ApplyLocalization();

            chatListControl.ChatSelected += new Action<ChatItem>(this.OnChatSelected);

            _typingTimer = new Timer();
            _typingTimer.Interval = 500;
            _typingTimer.Tick += new EventHandler(typingTimer_Tick);
            _typingTimer.Enabled = true;

            // Start background Telegram update listener
            GatewayClient.Instance.NewMessageReceived += new Action<long, MessageItem>(OnGatewayNewMessage);
            GatewayClient.Instance.UserTypingReceived += new Action<long, long>(OnGatewayUserTyping);
            GatewayClient.Instance.UserStatusReceived += new Action<long, string>(OnGatewayUserStatus);
            GatewayClient.Instance.MessagesReadReceived += new Action<long, long>(OnGatewayMessagesRead);
            GatewayClient.Instance.StartPolling();

            LoadChats();
        }

        public void ApplyLocalization()
        {
            L10n.UpdateLanguage();
            btnFilterAll.Text = L10n.TabAll;
            btnFilterChats.Text = L10n.TabChats;
            btnFilterGroups.Text = L10n.TabGroups;
            menuAction.Text = L10n.Menu;
            menuSearch.Text = L10n.Search;
            menuSettings.Text = L10n.Settings;
            menuRefresh.Text = L10n.Refresh;
            menuAbout.Text = L10n.About;
            menuExitApp.Text = L10n.Exit;
            menuExit.Text = L10n.Exit;
            this.Text = "MobileGram";
        }

        private void typingTimer_Tick(object sender, EventArgs e)
        {
            chatListControl.AdvanceTypingAnimation();
        }

        private void LoadChats()
        {
            // Fetch dialogs from Telegram bridge
            List<ChatItem> remote = GatewayClient.Instance.FetchChats(_activeFilter);
            if (remote != null && remote.Count > 0)
            {
                _currentChats = remote;
            }
            else
            {
                _currentChats = new List<ChatItem>();
            }
            chatListControl.SetChats(_currentChats);
        }

        private void frmChatList_Activated(object sender, EventArgs e)
        {
            ApplyLocalization();
            LoadChats();
        }

        private void OnGatewayNewMessage(long chatId, MessageItem msg)
        {
            // Thread-safe marshal to UI thread
            this.Invoke((Action)delegate {
                bool found = false;
                for (int i = 0; i < _currentChats.Count; i++)
                {
                    if (_currentChats[i].Id == chatId)
                    {
                        _currentChats[i].LastMessage = msg.Text;
                        _currentChats[i].TimeStr = msg.TimeStr;
                        if (!msg.IsSelf) _currentChats[i].UnreadCount++;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    chatListControl.Invalidate();
                }
                else
                {
                    LoadChats();
                }

                // Notification if this chat is not currently in foreground
                if (chatId != ActiveChatId && !msg.IsSelf)
                {
                    bool isMuted = ConfigManager.IsChatMuted(chatId);
                    for (int i = 0; i < _currentChats.Count; i++)
                    {
                        if (_currentChats[i].Id == chatId && _currentChats[i].IsMuted)
                        {
                            isMuted = true;
                            break;
                        }
                    }
                    if (!isMuted)
                    {
                        ShowGlobalNotification(msg.SenderName, msg.Text);
                    }
                }
            });
        }

        private void ShowGlobalNotification(string senderName, string text)
        {
            try
            {
                if (_globalNotification == null)
                {
                    _globalNotification = new Microsoft.WindowsCE.Forms.Notification();
                    _globalNotification.Caption = "MobileGram";
                }
                _globalNotification.Text = string.Format("<b>{0}</b>: {1}", senderName, text);
                _globalNotification.InitialDuration = 5;
                _globalNotification.Visible = true;
            }
            catch { }
        }

        private void OnGatewayUserTyping(long chatId, long userId)
        {
            this.Invoke((Action)delegate {
                for (int i = 0; i < _currentChats.Count; i++)
                {
                    if (_currentChats[i].Id == chatId)
                    {
                        _currentChats[i].IsTyping = true;
                        chatListControl.Invalidate();
                        break;
                    }
                }
            });
        }

        private void OnGatewayUserStatus(long userId, string status)
        {
            this.Invoke((Action)delegate {
                PresenceStatus ps = PresenceStatus.Offline;
                if (status == "online") ps = PresenceStatus.Online;
                else if (status == "recently") ps = PresenceStatus.Recently;

                bool changed = false;
                for (int i = 0; i < _currentChats.Count; i++)
                {
                    if (_currentChats[i].Id == userId)
                    {
                        _currentChats[i].Status = ps;
                        changed = true;
                        break;
                    }
                }
                if (changed)
                {
                    chatListControl.Invalidate();
                }
            });
        }

        private void OnGatewayMessagesRead(long chatId, long maxId)
        {
            this.Invoke((Action)delegate {
                for (int i = 0; i < _currentChats.Count; i++)
                {
                    if (_currentChats[i].Id == chatId)
                    {
                        _currentChats[i].UnreadCount = 0;
                        chatListControl.Invalidate();
                        break;
                    }
                }
            });
        }

        private void OnChatSelected(ChatItem chat)
        {
            if (chat == null) return;
            if (chat.IsForum)
            {
                frmTopicList topicForm = new frmTopicList(chat);
                topicForm.Show();
            }
            else
            {
                frmDirectChat chatForm = new frmDirectChat(chat, null);
                chatForm.Show();
            }
        }

        private void btnFilterAll_Click(object sender, EventArgs e)
        {
            _activeFilter = "all";
            btnFilterAll.Font = FontFilterBold;
            btnFilterChats.Font = FontFilterRegular;
            btnFilterGroups.Font = FontFilterRegular;
            chatListControl.SetFilter("all");
            LoadChats();
        }

        private void btnFilterChats_Click(object sender, EventArgs e)
        {
            _activeFilter = "chats";
            btnFilterAll.Font = FontFilterRegular;
            btnFilterChats.Font = FontFilterBold;
            btnFilterGroups.Font = FontFilterRegular;
            chatListControl.SetFilter("chats");
            LoadChats();
        }

        private void btnFilterGroups_Click(object sender, EventArgs e)
        {
            _activeFilter = "groups";
            btnFilterAll.Font = FontFilterRegular;
            btnFilterChats.Font = FontFilterRegular;
            btnFilterGroups.Font = FontFilterBold;
            chatListControl.SetFilter("groups");
            LoadChats();
        }

        private void menuSearch_Click(object sender, EventArgs e)
        {
            using (dlgInput dlg = new dlgInput(L10n.SearchTitle, L10n.SearchPrompt, false))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string q = dlg.Value.Trim();
                    if (string.IsNullOrEmpty(q))
                    {
                        LoadChats();
                        return;
                    }

                    Cursor.Current = Cursors.WaitCursor;
                    List<ChatItem> found = GatewayClient.Instance.SearchGlobal(q);
                    Cursor.Current = Cursors.Default;

                    if (found != null && found.Count > 0)
                    {
                        _currentChats = found;
                        chatListControl.SetChats(_currentChats);
                    }
                    else
                    {
                        MessageBox.Show(L10n.NotFound + q, L10n.SearchTitle);
                    }
                }
            }
        }

        private void menuSettings_Click(object sender, EventArgs e)
        {
            frmSettings settings = new frmSettings(true);
            settings.Show();
        }

        private void menuRefresh_Click(object sender, EventArgs e)
        {
            LoadChats();
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(GetSystemAboutInfo(), L10n.About);
        }

        public static string GetSystemAboutInfo()
        {
            Version ver = Environment.OSVersion.Version;
            string wmVer = "6.0";
            if (ver.Major == 5 && ver.Minor == 2)
            {
                if (ver.Build >= 21000) wmVer = "6.5";
                else if (ver.Build >= 19000) wmVer = "6.1";
                else wmVer = "6.0";
            }
            else if (ver.Major == 5 && ver.Minor == 1)
            {
                wmVer = "5.0";
            }

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;
            string resStr = string.Format("{0}x{1} ({2})", screenW, screenH, (screenW == 240 && screenH == 320) ? "QVGA" : "Custom");

            string platform = Environment.OSVersion.Platform.ToString();
            string deviceName = System.Net.Dns.GetHostName();

            if (L10n.IsRussian)
            {
                return string.Format(
                    "✈ MobileGram by AdvaScriptCC, версия 0.4f\n\n" +
                    "ОС: Windows Mobile {0}\n" +
                    "Ядро: Windows CE {1}.{2} (сборка {3})\n" +
                    "Разрешение: {4}\n" +
                    "Устройство: {5}\n" +
                    "Платформа: {6}\n" +
                    "Среда: .NET Compact Framework 3.5",
                    wmVer, ver.Major, ver.Minor, ver.Build, resStr, deviceName, platform);
            }
            else
            {
                return string.Format(
                    "✈ MobileGram by AdvaScriptCC, version 0.4f\n\n" +
                    "OS: Windows Mobile {0}\n" +
                    "Kernel: Windows CE {1}.{2} (build {3})\n" +
                    "Resolution: {4}\n" +
                    "Device: {5}\n" +
                    "Platform: {6}\n" +
                    "Runtime: .NET Compact Framework 3.5",
                    wmVer, ver.Major, ver.Minor, ver.Build, resStr, deviceName, platform);
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            GatewayClient.Instance.StopPolling();
            Application.Exit();
        }

        private void frmChatList_Closed(object sender, EventArgs e)
        {
            if (_typingTimer != null)
            {
                _typingTimer.Enabled = false;
                _typingTimer.Dispose();
            }
            GatewayClient.Instance.NewMessageReceived -= OnGatewayNewMessage;
            GatewayClient.Instance.UserTypingReceived -= OnGatewayUserTyping;
            GatewayClient.Instance.UserStatusReceived -= OnGatewayUserStatus;
            GatewayClient.Instance.MessagesReadReceived -= OnGatewayMessagesRead;
            GatewayClient.Instance.StopPolling();
            Application.Exit();
        }
    }
}
