using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MobileGram.Network
{
    /// <summary>
    /// Lightweight JSON parser optimized for .NET Compact Framework 3.5.
    /// Minimal memory allocation footprint for embedded ARM architectures.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int index = 0;
            return ParseValue(json.Trim(), ref index);
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (char.IsDigit(c) || c == '-') return ParseNumber(json, ref index);
            if (c == 't' || c == 'T' || c == 'f' || c == 'F') return ParseBoolean(json, ref index);
            if (c == 'n' || c == 'N') return ParseNull(json, ref index);

            index++;
            return null;
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            index++; // Skip '{'

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}') { index++; break; }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);

                if (index < json.Length && json[index] == ':') index++; // Skip ':'

                object val = ParseValue(json, ref index);
                dict[key] = val;

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            List<object> list = new List<object>();
            index++; // Skip '['

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']') { index++; break; }

                object val = ParseValue(json, ref index);
                list.Add(val);

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != '"') return "";

            index++; // Skip opening quote
            StringBuilder sb = new StringBuilder();

            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') break;
                if (c == '\\' && index < json.Length)
                {
                    char esc = json[index++];
                    if (esc == 'n') sb.Append('\n');
                    else if (esc == 'r') sb.Append('\r');
                    else if (esc == 't') sb.Append('\t');
                    else if (esc == '"') sb.Append('"');
                    else if (esc == '\\') sb.Append('\\');
                    else if (esc == 'u' && index + 4 <= json.Length)
                    {
                        string hex = json.Substring(index, 4);
                        index += 4;
                        try { sb.Append((char)Convert.ToInt32(hex, 16)); }
                        catch { }
                    }
                    else sb.Append(esc);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '-' || json[index] == '.'))
            {
                index++;
            }
            string numStr = json.Substring(start, index - start);
            try { return Convert.ToInt64(numStr); }
            catch
            {
                try { return Convert.ToDouble(numStr); }
                catch { return 0; }
            }
        }

        private static bool ParseBoolean(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                index += 5;
                return false;
            }
            index++;
            return false;
        }

        private static object ParseNull(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("null", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
            }
            else index++;
            return null;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        public static long SafeParseLong(string s, long defVal)
        {
            if (string.IsNullOrEmpty(s)) return defVal;
            try { return Convert.ToInt64(s); }
            catch { return defVal; }
        }

        public static int SafeParseInt(string s, int defVal)
        {
            if (string.IsNullOrEmpty(s)) return defVal;
            try { return Convert.ToInt32(s); }
            catch { return defVal; }
        }

        public static bool SafeParseBool(string s, bool defVal)
        {
            if (string.IsNullOrEmpty(s)) return defVal;
            try { return Convert.ToBoolean(s); }
            catch { return defVal; }
        }

        // Safe typed value extraction helpers
        public static string GetString(Dictionary<string, object> dict, string key, string defVal)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null) return dict[key].ToString();
            return defVal;
        }

        public static long GetLong(Dictionary<string, object> dict, string key, long defVal)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                return SafeParseLong(dict[key].ToString(), defVal);
            }
            return defVal;
        }

        public static int GetInt(Dictionary<string, object> dict, string key, int defVal)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                return SafeParseInt(dict[key].ToString(), defVal);
            }
            return defVal;
        }

        public static bool GetBool(Dictionary<string, object> dict, string key, bool defVal)
        {
            if (dict != null && dict.ContainsKey(key) && dict[key] != null)
            {
                if (dict[key] is bool) return (bool)dict[key];
                return SafeParseBool(dict[key].ToString(), defVal);
            }
            return defVal;
        }
    }
}
