using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MobileGram.Models;

namespace MobileGram.Network
{
    public class GatewayClient
    {
        private static GatewayClient _instance;
        public static GatewayClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GatewayClient(ConfigManager.ServerIp, ConfigManager.ServerPort);
                }
                return _instance;
            }
        }

        private string _baseUrl;
        private Thread _pollThread;
        private bool _isPolling;

        // Chat cache for fast lookup and null-safety
        private Dictionary<long, ChatItem> _chatCache = new Dictionary<long, ChatItem>();

        public event Action<long, MessageItem> NewMessageReceived;
        public event Action<long, long> UserTypingReceived;
        public event Action<long, string> UserStatusReceived;
        public event Action<long, long> MessagesReadReceived;

        public GatewayClient(string ip, int port)
        {
            UpdateEndpoint(ip, port);
        }

        public void UpdateEndpoint(string ip, int port)
        {
            _baseUrl = string.Format("http://{0}:{1}/api/v1", ip.Trim(), port);
        }

        public void CacheChats(List<ChatItem> chats)
        {
            if (chats == null) return;
            lock (_chatCache)
            {
                for (int i = 0; i < chats.Count; i++)
                {
                    _chatCache[chats[i].Id] = chats[i];
                }
            }
        }

        public ChatItem GetCachedChat(long chatId)
        {
            lock (_chatCache)
            {
                if (_chatCache.ContainsKey(chatId))
                {
                    return _chatCache[chatId];
                }
            }
            return null;
        }

        public string Get(string endpoint)
        {
            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            StreamReader reader = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
                req.Method = "GET";
                req.Timeout = 12000;
                resp = (HttpWebResponse)req.GetResponse();
                reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
                return reader.ReadToEnd();
            }
            finally
            {
                if (reader != null) reader.Close();
                if (resp != null) resp.Close();
            }
        }

        public string PostJson(string endpoint, string jsonBody)
        {
            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            Stream reqStream = null;
            StreamReader reader = null;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(jsonBody);
                req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = data.Length;
                req.Timeout = 12000;

                reqStream = req.GetRequestStream();
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
                reqStream = null;

                resp = (HttpWebResponse)req.GetResponse();
                reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
                return reader.ReadToEnd();
            }
            finally
            {
                if (reqStream != null) reqStream.Close();
                if (reader != null) reader.Close();
                if (resp != null) resp.Close();
            }
        }

        // ==========================================
        // AUTHORIZATION METHODS
        // ==========================================

        public bool CheckAuthStatus(out string firstName, out string username)
        {
            firstName = "";
            username = "";
            try
            {
                string res = Get("/auth/status");
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                if (dict != null)
                {
                    bool auth = MiniJson.GetBool(dict, "authorized", false);
                    Dictionary<string, object> user = dict.ContainsKey("user") ? dict["user"] as Dictionary<string, object> : null;
                    if (user != null)
                    {
                        firstName = MiniJson.GetString(user, "first_name", "");
                        username = MiniJson.GetString(user, "username", "");
                    }
                    return auth;
                }
            }
            catch { }
            return false;
        }

        public bool SendAuthLogin(string phone, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                string body = string.Format("{{\"phone\":\"{0}\"}}", EscapeJson(phone));
                string res = PostJson("/auth/login", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                if (dict != null)
                {
                    string status = MiniJson.GetString(dict, "status", "");
                    if (status == "code_sent") return true;
                    errorMsg = MiniJson.GetString(dict, "message", "Ошибка отправки кода");
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return false;
        }

        public string SendAuthCode(string phone, string code, out string hint, out string errorMsg)
        {
            hint = "";
            errorMsg = "";
            try
            {
                string body = string.Format("{{\"phone\":\"{0}\",\"code\":\"{1}\"}}", EscapeJson(phone), EscapeJson(code));
                string res = PostJson("/auth/code", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                if (dict != null)
                {
                    string status = MiniJson.GetString(dict, "status", "");
                    hint = MiniJson.GetString(dict, "hint", "");
                    errorMsg = MiniJson.GetString(dict, "message", "");
                    return status;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return "error";
        }

        public bool SendAuth2Fa(string password, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                string body = string.Format("{{\"password\":\"{0}\"}}", EscapeJson(password));
                string res = PostJson("/auth/2fa", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                if (dict != null)
                {
                    string status = MiniJson.GetString(dict, "status", "");
                    if (status == "ok") return true;
                    errorMsg = MiniJson.GetString(dict, "message", "Неверный 2FA пароль");
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return false;
        }

        // ==========================================
        // CHATS, TOPICS AND MESSAGES API
        // ==========================================

        public List<ChatItem> FetchChats(string filter)
        {
            List<ChatItem> list = new List<ChatItem>();
            try
            {
                string res = Get("/chats?filter=" + filter);
                List<object> arr = MiniJson.Deserialize(res) as List<object>;
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Dictionary<string, object> item = arr[i] as Dictionary<string, object>;
                        if (item == null) continue;

                        ChatItem chat = new ChatItem();
                        chat.Id = MiniJson.GetLong(item, "id", 0);
                        chat.Type = MiniJson.GetString(item, "type", "user");
                        chat.Title = MiniJson.GetString(item, "title", "Диалог");
                        chat.Username = MiniJson.GetString(item, "username", "");
                        chat.UnreadCount = MiniJson.GetInt(item, "unread", 0);
                        chat.TimeStr = MiniJson.GetString(item, "time", "");
                        chat.LastMessage = MiniJson.GetString(item, "last_msg", "");
                        chat.IsTyping = MiniJson.GetBool(item, "typing", false);
                        chat.IsMuted = MiniJson.GetBool(item, "muted", false);
                        chat.MembersCount = MiniJson.GetInt(item, "members_cnt", 0);
                        chat.IsForum = MiniJson.GetBool(item, "is_forum", false);

                        string statusStr = MiniJson.GetString(item, "status", "");
                        if (statusStr == "online") chat.Status = PresenceStatus.Online;
                        else if (statusStr == "recently") chat.Status = PresenceStatus.Recently;
                        else chat.Status = PresenceStatus.Offline;

                        list.Add(chat);
                    }
                    CacheChats(list);
                }
            }
            catch { }
            return list;
        }

        public List<TopicItem> FetchTopics(long chatId)
        {
            List<TopicItem> topics = new List<TopicItem>();
            try
            {
                string res = Get(string.Format("/topics?chat_id={0}", chatId));
                List<object> arr = MiniJson.Deserialize(res) as List<object>;
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Dictionary<string, object> item = arr[i] as Dictionary<string, object>;
                        if (item == null) continue;

                        TopicItem t = new TopicItem();
                        t.Id = MiniJson.GetLong(item, "id", 0);
                        t.Title = MiniJson.GetString(item, "title", "Тема");
                        t.IconColor = MiniJson.GetInt(item, "icon_color", 0);
                        t.UnreadCount = MiniJson.GetInt(item, "unread_count", 0);
                        t.Closed = MiniJson.GetBool(item, "closed", false);
                        topics.Add(t);
                    }
                }
            }
            catch { }
            return topics;
        }

        public List<MessageItem> FetchMessages(long chatId, int limit)
        {
            return FetchMessages(chatId, limit, 0);
        }

        public List<MessageItem> FetchMessages(long chatId, int limit, long topicId)
        {
            List<MessageItem> list = new List<MessageItem>();
            try
            {
                string url = string.Format("/messages?chat_id={0}&limit={1}&topic_id={2}", chatId, limit, topicId);
                string res = Get(url);
                List<object> arr = MiniJson.Deserialize(res) as List<object>;
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Dictionary<string, object> item = arr[i] as Dictionary<string, object>;
                        if (item == null) continue;

                        MessageItem msg = new MessageItem();
                        msg.MsgId = MiniJson.GetLong(item, "msg_id", 0);
                        msg.SenderId = MiniJson.GetLong(item, "sender_id", 0);
                        msg.SenderName = MiniJson.GetString(item, "sender_name", "Собеседник");
                        msg.IsSelf = MiniJson.GetBool(item, "is_self", false);
                        msg.TimeStr = MiniJson.GetString(item, "time", "");
                        msg.Text = MiniJson.GetString(item, "text", "");
                        msg.IsService = MiniJson.GetBool(item, "is_service", false);
                        msg.IsPoll = MiniJson.GetBool(item, "is_poll", false);
                        msg.PollQuestion = MiniJson.GetString(item, "poll_question", "");
                        msg.MyVoteIndex = MiniJson.GetInt(item, "my_vote_idx", -1);
                        msg.TopicId = topicId;
                        msg.HasMedia = MiniJson.GetBool(item, "has_media", false);
                        msg.MediaType = MiniJson.GetString(item, "media_type", "");

                        // Quoted replies
                        msg.ReplyToMsgId = MiniJson.GetLong(item, "reply_to_msg_id", 0);
                        msg.ReplyToSender = MiniJson.GetString(item, "reply_to_sender", "");
                        msg.ReplyToText = MiniJson.GetString(item, "reply_to_text", "");

                        // Message reactions
                        if (item.ContainsKey("reactions") && item["reactions"] is List<object>)
                        {
                            List<object> rList = item["reactions"] as List<object>;
                            for (int r = 0; r < rList.Count; r++)
                            {
                                Dictionary<string, object> rDict = rList[r] as Dictionary<string, object>;
                                if (rDict != null)
                                {
                                    ReactionBadge badge = new ReactionBadge();
                                    badge.Emoji = MiniJson.GetString(rDict, "emoji", "👍");
                                    badge.Count = MiniJson.GetInt(rDict, "count", 1);
                                    badge.Chosen = MiniJson.GetBool(rDict, "chosen", false);
                                    msg.Reactions.Add(badge);
                                }
                            }
                        }

                        // Polls
                        if (item.ContainsKey("poll_options") && item["poll_options"] is List<object>)
                        {
                            List<object> opts = item["poll_options"] as List<object>;
                            for (int o = 0; o < opts.Count; o++) msg.PollOptions.Add(opts[o].ToString());
                        }
                        if (item.ContainsKey("poll_votes") && item["poll_votes"] is List<object>)
                        {
                            List<object> votes = item["poll_votes"] as List<object>;
                            for (int v = 0; v < votes.Count; v++)
                            {
                                int cnt = MiniJson.SafeParseInt(votes[v].ToString(), 0);
                                msg.PollVotes.Add(cnt);
                            }
                        }

                        string st = MiniJson.GetString(item, "status", "read");
                        msg.Status = (st == "read") ? DeliveryStatus.Read : DeliveryStatus.Sent;

                        list.Add(msg);
                    }
                }
            }
            catch { }
            return list;
        }

        public bool SendMessage(long chatId, string text)
        {
            return SendMessage(chatId, text, 0, 0);
        }

        public bool SendMessage(long chatId, string text, long topicId, long replyToMsgId)
        {
            try
            {
                string body = string.Format(
                    "{{\"chat_id\":{0},\"text\":\"{1}\",\"topic_id\":{2},\"reply_to_msg_id\":{3}}}",
                    chatId, EscapeJson(text), topicId, replyToMsgId
                );
                string res = PostJson("/send", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                return (dict != null && MiniJson.GetString(dict, "status", "") == "ok");
            }
            catch { }
            return false;
        }

        public bool SendReaction(long chatId, long msgId, string reaction)
        {
            try
            {
                string body = string.Format(
                    "{{\"chat_id\":{0},\"msg_id\":{1},\"reaction\":\"{2}\"}}",
                    chatId, msgId, EscapeJson(reaction)
                );
                string res = PostJson("/reactions/send", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                return (dict != null && MiniJson.GetString(dict, "status", "") == "ok");
            }
            catch { }
            return false;
        }

        public bool MarkAsRead(long chatId, long maxId)
        {
            try
            {
                string body = string.Format("{{\"chat_id\":{0},\"max_id\":{1}}}", chatId, maxId);
                string res = PostJson("/messages/read", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                return (dict != null && MiniJson.GetString(dict, "status", "") == "ok");
            }
            catch { }
            return false;
        }

        public bool SetChatMute(long chatId, bool muted)
        {
            try
            {
                string body = string.Format("{{\"chat_id\":{0},\"muted\":{1}}}", chatId, muted ? "true" : "false");
                string res = PostJson("/chats/mute", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                return (dict != null && MiniJson.GetString(dict, "status", "") == "ok");
            }
            catch { }
            return false;
        }

        public List<ChatItem> SearchGlobal(string query)
        {
            List<ChatItem> list = new List<ChatItem>();
            try
            {
                string res = Get("/search?q=" + Uri.EscapeDataString(query));
                List<object> arr = MiniJson.Deserialize(res) as List<object>;
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Dictionary<string, object> item = arr[i] as Dictionary<string, object>;
                        if (item == null) continue;

                        ChatItem chat = new ChatItem();
                        chat.Id = MiniJson.GetLong(item, "id", 0);
                        chat.Type = MiniJson.GetString(item, "type", "user");
                        chat.Title = MiniJson.GetString(item, "title", "Контакт");
                        chat.Username = MiniJson.GetString(item, "username", "");
                        chat.IsForum = MiniJson.GetBool(item, "is_forum", false);
                        bool isGlobal = MiniJson.GetBool(item, "is_global", false);
                        if (isGlobal && !string.IsNullOrEmpty(chat.Username))
                        {
                            chat.LastMessage = "@" + chat.Username;
                        }
                        list.Add(chat);
                    }
                    CacheChats(list);
                }
            }
            catch { }
            return list;
        }

        public bool VotePoll(long chatId, long msgId, int optionIdx)
        {
            try
            {
                string body = string.Format("{{\"chat_id\":{0},\"msg_id\":{1},\"option\":{2}}}", chatId, msgId, optionIdx);
                string res = PostJson("/poll/vote", body);
                Dictionary<string, object> dict = MiniJson.Deserialize(res) as Dictionary<string, object>;
                return (dict != null && MiniJson.GetString(dict, "status", "") == "ok");
            }
            catch { }
            return false;
        }

        public Bitmap FetchMedia(long chatId, long msgId, int maxW)
        {
            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            try
            {
                string url = string.Format("{0}/media?chat_id={1}&msg_id={2}&max_w={3}", _baseUrl, chatId, msgId, maxW);
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 15000;
                resp = (HttpWebResponse)req.GetResponse();
                using (Stream stream = resp.GetResponseStream())
                {
                    return new Bitmap(stream);
                }
            }
            catch { }
            finally
            {
                if (resp != null) resp.Close();
            }
            return null;
        }

        // ==========================================
        // LONG-POLLING
        // ==========================================

        public void StartPolling()
        {
            if (_isPolling) return;
            _isPolling = true;
            _pollThread = new Thread(new ThreadStart(PollingWorker));
            _pollThread.IsBackground = true;
            _pollThread.Start();
        }

        public void StopPolling()
        {
            _isPolling = false;
        }

        private void PollingWorker()
        {
            while (_isPolling)
            {
                try
                {
                    string res = Get("/updates?timeout=25");
                    Dictionary<string, object> root = MiniJson.Deserialize(res) as Dictionary<string, object>;
                    if (root != null && root.ContainsKey("events") && root["events"] is List<object>)
                    {
                        List<object> eventsList = root["events"] as List<object>;
                        for (int i = 0; i < eventsList.Count; i++)
                        {
                            Dictionary<string, object> ev = eventsList[i] as Dictionary<string, object>;
                            if (ev == null) continue;

                            string type = MiniJson.GetString(ev, "type", "");
                            if (type == "new_message")
                            {
                                long chatId = MiniJson.GetLong(ev, "chat_id", 0);
                                Dictionary<string, object> mDict = ev.ContainsKey("message") ? ev["message"] as Dictionary<string, object> : null;
                                if (mDict != null && NewMessageReceived != null)
                                {
                                    MessageItem msg = new MessageItem();
                                    msg.MsgId = MiniJson.GetLong(mDict, "msg_id", 0);
                                    msg.SenderId = MiniJson.GetLong(mDict, "sender_id", 0);
                                    msg.SenderName = MiniJson.GetString(mDict, "sender_name", "");
                                    msg.IsSelf = MiniJson.GetBool(mDict, "is_self", false);
                                    msg.TimeStr = MiniJson.GetString(mDict, "time", "");
                                    msg.Text = MiniJson.GetString(mDict, "text", "");
                                    msg.IsService = MiniJson.GetBool(mDict, "is_service", false);
                                    msg.ReplyToMsgId = MiniJson.GetLong(mDict, "reply_to_msg_id", 0);
                                    msg.TopicId = MiniJson.GetLong(mDict, "topic_id", 0);

                                    NewMessageReceived(chatId, msg);
                                }
                            }
                            else if (type == "user_typing" && UserTypingReceived != null)
                            {
                                long chatId = MiniJson.GetLong(ev, "chat_id", 0);
                                long userId = MiniJson.GetLong(ev, "user_id", 0);
                                UserTypingReceived(chatId, userId);
                            }
                            else if (type == "user_status" && UserStatusReceived != null)
                            {
                                long userId = MiniJson.GetLong(ev, "user_id", 0);
                                string status = MiniJson.GetString(ev, "status", "offline");
                                UserStatusReceived(userId, status);
                            }
                            else if (type == "messages_read" && MessagesReadReceived != null)
                            {
                                long chatId = MiniJson.GetLong(ev, "chat_id", 0);
                                long maxId = MiniJson.GetLong(ev, "max_id", 0);
                                MessagesReadReceived(chatId, maxId);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(4000);
                }
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
