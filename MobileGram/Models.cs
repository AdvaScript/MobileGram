using System;
using System.Collections.Generic;
using System.Drawing;

namespace MobileGram.Models
{
    public enum PresenceStatus
    {
        Offline,
        Online,
        Recently,
        LastWeek,
        LastMonth
    }

    public enum DeliveryStatus
    {
        Sending,
        Sent,
        Read
    }

    public class ChatItem
    {
        public long Id { get; set; }
        public string Type { get; set; } // "user", "group", "channel"
        public string Title { get; set; }
        public string Username { get; set; }
        public int UnreadCount { get; set; }
        public string TimeStr { get; set; }
        public string LastMessage { get; set; }
        public PresenceStatus Status { get; set; }
        public bool IsTyping { get; set; }
        public bool IsMuted { get; set; }
        public int MembersCount { get; set; }
        public Color AvatarColor { get; set; }

        // Telegram forum topics support
        public bool IsForum { get; set; }

        public ChatItem()
        {
            Type = "user";
            Status = PresenceStatus.Offline;
            AvatarColor = Color.FromArgb(0, 102, 204);
            IsForum = false;
        }
    }

    public class TopicItem
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public int IconColor { get; set; }
        public int UnreadCount { get; set; }
        public bool Closed { get; set; }

        public Color GetColor()
        {
            if (IconColor != 0)
            {
                int r = (IconColor >> 16) & 0xFF;
                int g = (IconColor >> 8) & 0xFF;
                int b = IconColor & 0xFF;
                return Color.FromArgb(r, g, b);
            }
            return Color.FromArgb(70, 130, 180);
        }
    }

    public class ReactionBadge
    {
        public string Emoji { get; set; }
        public int Count { get; set; }
        public bool Chosen { get; set; }

        public string GetDisplayText()
        {
            if (Emoji == "👍") return "[+1 " + Count + "]";
            if (Emoji == "❤️") return "[<3 " + Count + "]";
            if (Emoji == "🔥") return "[TOP " + Count + "]";
            if (Emoji == "😂") return "[XD " + Count + "]";
            if (Emoji == "👎") return "[-1 " + Count + "]";
            return "[" + Emoji + " " + Count + "]";
        }
    }

    public class MessageItem
    {
        public long MsgId { get; set; }
        public long SenderId { get; set; }
        public string SenderName { get; set; }
        public bool IsSelf { get; set; }
        public string TimeStr { get; set; }
        public string Text { get; set; }
        public DeliveryStatus Status { get; set; }
        public Color SenderColor { get; set; }

        // Service / system messages
        public bool IsService { get; set; }

        // Polls
        public bool IsPoll { get; set; }
        public string PollQuestion { get; set; }
        public List<string> PollOptions { get; set; }
        public List<int> PollVotes { get; set; }
        public int MyVoteIndex { get; set; }

        // Reply support
        public long ReplyToMsgId { get; set; }
        public string ReplyToSender { get; set; }
        public string ReplyToText { get; set; }

        // Message reactions
        public List<ReactionBadge> Reactions { get; set; }

        // Forum topic ID
        public long TopicId { get; set; }

        // Media attachments (Photo, Document, GIF)
        public bool HasMedia { get; set; }
        public string MediaType { get; set; }

        // Computed rendering height
        public int ComputedHeight { get; set; }

        public MessageItem()
        {
            Status = DeliveryStatus.Sent;
            SenderColor = Color.FromArgb(0, 51, 153);
            IsService = false;
            IsPoll = false;
            HasMedia = false;
            MediaType = "";
            MyVoteIndex = -1;
            PollOptions = new List<string>();
            PollVotes = new List<int>();
            Reactions = new List<ReactionBadge>();
            ReplyToMsgId = 0;
            ReplyToSender = "";
            ReplyToText = "";
            TopicId = 0;
        }

        public MessageItem(long msgId, long senderId, string senderName, bool isSelf, string timeStr, string text, DeliveryStatus status, Color senderColor)
            : this()
        {
            this.MsgId = msgId;
            this.SenderId = senderId;
            this.SenderName = senderName;
            this.IsSelf = isSelf;
            this.TimeStr = timeStr;
            this.Text = text;
            this.Status = status;
            this.SenderColor = senderColor;
        }

        public static MessageItem CreateService(string text, string timeStr)
        {
            MessageItem s = new MessageItem();
            s.MsgId = DateTime.Now.Ticks;
            s.IsService = true;
            s.Text = text;
            s.TimeStr = timeStr;
            return s;
        }

        public static MessageItem CreatePoll(long senderId, string senderName, string question, string[] options, int[] initialVotes, string timeStr)
        {
            MessageItem p = new MessageItem();
            p.MsgId = DateTime.Now.Ticks;
            p.SenderId = senderId;
            p.SenderName = senderName;
            p.IsPoll = true;
            p.PollQuestion = question;
            p.TimeStr = timeStr;
            p.Text = "[Опрос] " + question;
            for (int i = 0; i < options.Length; i++)
            {
                p.PollOptions.Add(options[i]);
                p.PollVotes.Add(initialVotes != null && i < initialVotes.Length ? initialVotes[i] : 0);
            }
            return p;
        }
    }
}
