using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MobileGram.Models;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmTopicList : Form
    {
        private ChatItem _chat;
        private List<TopicItem> _topics = new List<TopicItem>();

        public frmTopicList(ChatItem chat)
        {
            InitializeComponent();
            _chat = chat;
            menuBack.Text = L10n.Back;
            menuOpen.Text = L10n.Ok;
            this.Text = L10n.Topics + ": " + (_chat != null ? _chat.Title : L10n.Group);
            lblHeaderTitle.Text = L10n.Topics + ": " + (_chat != null ? _chat.Title : "");
            LoadTopics();
        }

        private void LoadTopics()
        {
            lstTopics.Items.Clear();

            if (_chat == null) return;

            // Request forum topics list from bridge
            _topics = GatewayClient.Instance.FetchTopics(_chat.Id) ?? new List<TopicItem>();

            for (int i = 0; i < _topics.Count; i++)
            {
                TopicItem t = _topics[i];
                ListViewItem item = new ListViewItem("# " + t.Title);
                item.SubItems.Add(t.UnreadCount > 0 ? ("+" + t.UnreadCount) : "");
                item.Tag = t;
                lstTopics.Items.Add(item);
            }

            if (lstTopics.Items.Count > 0)
            {
                lstTopics.Items[0].Selected = true;
            }
        }

        private void OpenSelectedTopic()
        {
            if (lstTopics.SelectedIndices.Count == 0) return;
            int idx = lstTopics.SelectedIndices[0];
            if (idx >= 0 && idx < _topics.Count)
            {
                TopicItem chosen = _topics[idx];
                frmDirectChat chatForm = new frmDirectChat(_chat, chosen);
                chatForm.Show();
                this.Close();
            }
        }

        private void lstTopics_ItemActivate(object sender, EventArgs e)
        {
            OpenSelectedTopic();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            OpenSelectedTopic();
        }

        private void menuBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
