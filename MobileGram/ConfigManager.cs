using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MobileGram.Network
{
    public static class ConfigManager
    {
        public static string ServerIp = "192.168.1.100";
        public static int ServerPort = 8080;
        public static string UserName = "Вы";
        public static bool UseProxy = true;
        public static string Language = "auto"; // "auto", "ru", "en"
        private static List<long> _mutedChatIds = new List<long>();

        private static string GetConfigPath()
        {
            string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            return Path.Combine(appDir, "config.ini");
        }

        public static bool IsChatMuted(long chatId)
        {
            return _mutedChatIds.Contains(chatId);
        }

        public static void SetChatMuted(long chatId, bool muted)
        {
            if (muted && !_mutedChatIds.Contains(chatId))
            {
                _mutedChatIds.Add(chatId);
                Save();
            }
            else if (!muted && _mutedChatIds.Contains(chatId))
            {
                _mutedChatIds.Remove(chatId);
                Save();
            }
        }

        public static void Load()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return;

                using (StreamReader reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("#") || line.IndexOf('=') < 0) continue;

                        int eqIdx = line.IndexOf('=');
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1).Trim();

                        if (key == "ServerIp") ServerIp = val;
                        else if (key == "ServerPort") ServerPort = MiniJson.SafeParseInt(val, 8080);
                        else if (key == "UserName") UserName = val;
                        else if (key == "UseProxy") UseProxy = MiniJson.SafeParseBool(val, true);
                        else if (key == "Language") Language = val;
                        else if (key == "MutedChats")
                        {
                            _mutedChatIds.Clear();
                            string[] parts = val.Split(',');
                            for (int i = 0; i < parts.Length; i++)
                            {
                                long cid = MiniJson.SafeParseLong(parts[i].Trim(), 0);
                                if (cid != 0 && !_mutedChatIds.Contains(cid))
                                {
                                    _mutedChatIds.Add(cid);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                string path = GetConfigPath();
                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.WriteLine("# MobileGram Configuration");
                    writer.WriteLine("ServerIp=" + ServerIp);
                    writer.WriteLine("ServerPort=" + ServerPort);
                    writer.WriteLine("UserName=" + UserName);
                    writer.WriteLine("UseProxy=" + UseProxy);
                    writer.WriteLine("Language=" + Language);

                    List<string> mList = new List<string>();
                    for (int i = 0; i < _mutedChatIds.Count; i++)
                    {
                        mList.Add(_mutedChatIds[i].ToString());
                    }
                    writer.WriteLine("MutedChats=" + string.Join(",", mList.ToArray()));
                }
            }
            catch { }
        }
    }
}
