using System;
using System.Globalization;

namespace MobileGram
{
    public enum AppLanguage
    {
        Auto = 0,
        Russian = 1,
        English = 2
    }

    public static class L10n
    {
        private static bool _isRussian = true;

        static L10n()
        {
            UpdateLanguage();
        }

        public static void UpdateLanguage()
        {
            string cfg = Network.ConfigManager.Language;
            if (cfg == "ru")
            {
                _isRussian = true;
            }
            else if (cfg == "en")
            {
                _isRussian = false;
            }
            else // auto
            {
                try
                {
                    string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                    _isRussian = (twoLetter == "ru" || twoLetter == "be" || twoLetter == "uk");
                }
                catch
                {
                    _isRussian = true;
                }
            }
        }

        public static bool IsRussian { get { return _isRussian; } }

        // --- Navigation and Main Screen ---
        public static string TabAll { get { return _isRussian ? "Все" : "All"; } }
        public static string TabChats { get { return _isRussian ? "Чаты" : "Chats"; } }
        public static string TabGroups { get { return _isRussian ? "Группы" : "Groups"; } }
        public static string Menu { get { return _isRussian ? "Меню" : "Menu"; } }
        public static string Exit { get { return _isRussian ? "Выход" : "Exit"; } }
        public static string Settings { get { return _isRussian ? "Настройки" : "Settings"; } }
        public static string Refresh { get { return _isRussian ? "Обновить" : "Refresh"; } }
        public static string Search { get { return _isRussian ? "Поиск в Telegram..." : "Search Telegram..."; } }
        public static string About { get { return _isRussian ? "О программе MobileGram" : "About MobileGram"; } }
        public static string Back { get { return _isRussian ? "Назад" : "Back"; } }
        public static string Options { get { return _isRussian ? "Опции" : "Options"; } }
        public static string Send { get { return _isRussian ? "Отпр." : "Send"; } }
        public static string Cancel { get { return _isRussian ? "Отмена" : "Cancel"; } }
        public static string Ok { get { return "OK"; } }
        public static string LogIn { get { return _isRussian ? "Войти" : "Log In"; } }

        // --- Chat and Context Menu ---
        public static string Reply { get { return _isRussian ? "Ответить" : "Reply"; } }
        public static string Reaction { get { return _isRussian ? "Реакция..." : "Reaction..."; } }
        public static string RemoveReaction { get { return _isRussian ? "Снять реакцию" : "Remove Reaction"; } }
        public static string CopyText { get { return _isRussian ? "Копировать текст" : "Copy Text"; } }
        public static string ViewMedia { get { return _isRussian ? "Просмотреть фото/медиа" : "View Media"; } }
        public static string BanUser { get { return _isRussian ? "Заблокировать (Бан)" : "Ban User"; } }
        public static string Profile { get { return _isRussian ? "Профиль" : "Profile"; } }
        public static string Mute { get { return _isRussian ? "Отключить звук" : "Mute"; } }
        public static string Unmute { get { return _isRussian ? "Включить звук" : "Unmute"; } }
        public static string ClearHistory { get { return _isRussian ? "Очистить историю" : "Clear History"; } }
        public static string Topics { get { return _isRussian ? "Темы группы" : "Topics"; } }

        // --- Statuses ---
        public static string Online { get { return _isRussian ? "В сети" : "Online"; } }
        public static string Offline { get { return _isRussian ? "Не в сети" : "Offline"; } }
        public static string Recently { get { return _isRussian ? "Был(а) недавно" : "Recently"; } }
        public static string Typing { get { return _isRussian ? "печатает..." : "typing..."; } }
        public static string TypingHeader { get { return _isRussian ? "печатает" : "typing"; } }
        public static string Forum { get { return _isRussian ? "Форум" : "Forum"; } }
        public static string Group { get { return _isRussian ? "Группа" : "Group"; } }
        public static string DirectChat { get { return _isRussian ? "Личный чат" : "Chat"; } }
        public static string Members { get { return _isRussian ? "участников" : "members"; } }
        public static string NotificationsOn { get { return _isRussian ? "Включены" : "Enabled"; } }
        public static string NotificationsOff { get { return _isRussian ? "Отключены" : "Muted"; } }

        // --- Settings / Entrance ---
        public static string ServerIp { get { return _isRussian ? "Сервер-мост (IP):" : "Bridge Server (IP):"; } }
        public static string ServerPort { get { return _isRussian ? "Порт:" : "Port:"; } }
        public static string UseProxy { get { return _isRussian ? "Использовать прокси" : "Use proxy"; } }
        public static string LanguageLabel { get { return _isRussian ? "Язык интерфейса:" : "Language:"; } }
        public static string SettingsTitle { get { return _isRussian ? "Вход и настройки" : "Login & Settings"; } }
        public static string SettingsSaved { get { return _isRussian ? "Настройки сохранены." : "Settings saved."; } }

        // --- Dialogs and Errors ---
        public static string Connecting { get { return _isRussian ? "Подключение к шлюзу..." : "Connecting to bridge..."; } }
        public static string ConnectionError { get { return _isRussian ? "Не удалось связаться со шлюзом ({0}:{1}).\nПроверьте IP-адрес и убедитесь, что bridge.py запущен." : "Could not connect to bridge ({0}:{1}).\nCheck IP address and make sure bridge.py is running."; } }
        public static string NotAuthorized { get { return _isRussian ? "Шлюз не авторизован в Telegram!\n\nОткройте в браузере страницу:\n{0}\nи отсканируйте QR-код мобильным приложением Telegram." : "Bridge is not logged in to Telegram!\n\nOpen this URL in your browser:\n{0}\nand scan the QR code with your Telegram mobile app."; } }
        public static string AuthTitle { get { return _isRussian ? "Вход в Telegram" : "Telegram Login"; } }

        public static string SearchPrompt { get { return _isRussian ? "Имя, @username или текст:" : "Name, @username or text:"; } }
        public static string SearchTitle { get { return _isRussian ? "Поиск в Telegram" : "Telegram Search"; } }
        public static string NotFound { get { return _isRussian ? "Ничего не найдено по запросу: " : "Nothing found for: "; } }
        public static string EmptyChats { get { return _isRussian ? "Нет диалогов. Нажмите Меню -> Обновить." : "No chats. Tap Menu -> Refresh."; } }
        public static string MediaLoading { get { return _isRussian ? "Загрузка изображения..." : "Loading image..."; } }
        public static string MediaFailed { get { return _isRussian ? "Не удалось загрузить фото." : "Failed to load image."; } }
    }
}
