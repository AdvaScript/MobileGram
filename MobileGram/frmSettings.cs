using System;
using System.Threading;
using System.Windows.Forms;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmSettings : Form
    {
        private bool _isFromChatList = false;

        public frmSettings(bool isFromChatList)
        {
            InitializeComponent();
            _isFromChatList = isFromChatList;
            InitLanguages();
            LoadConfig();
            ApplyLocalization();
        }

        public frmSettings() : this(false)
        {
        }

        private void InitLanguages()
        {
            cboLanguage.Items.Clear();
            cboLanguage.Items.Add("Авто / Auto");
            cboLanguage.Items.Add("Русский");
            cboLanguage.Items.Add("English");
            cboLanguage.SelectedIndexChanged += new EventHandler(cboLanguage_SelectedIndexChanged);
        }

        private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboLanguage.SelectedIndex == 1) ConfigManager.Language = "ru";
            else if (cboLanguage.SelectedIndex == 2) ConfigManager.Language = "en";
            else ConfigManager.Language = "auto";

            ConfigManager.Save();
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            L10n.UpdateLanguage();
            this.Text = "MobileGram";
            lblServer.Text = L10n.ServerIp;
            lblPort.Text = L10n.ServerPort;
            chkProxy.Text = L10n.UseProxy;
            lblLanguage.Text = L10n.LanguageLabel;
            btnAuth.Text = _isFromChatList ? L10n.Ok : L10n.LogIn;
            menuBack.Text = _isFromChatList ? L10n.Back : L10n.Exit;
            menuOk.Text = _isFromChatList ? L10n.Ok : L10n.LogIn;
        }

        private void LoadConfig()
        {
            ConfigManager.Load();
            txtServer.Text = ConfigManager.ServerIp;
            txtPort.Text = ConfigManager.ServerPort.ToString();
            chkProxy.Checked = ConfigManager.UseProxy;

            if (ConfigManager.Language == "ru") cboLanguage.SelectedIndex = 1;
            else if (ConfigManager.Language == "en") cboLanguage.SelectedIndex = 2;
            else cboLanguage.SelectedIndex = 0;
        }

        private void SaveConfigFromUi()
        {
            ConfigManager.ServerIp = txtServer.Text.Trim();
            ConfigManager.ServerPort = MiniJson.SafeParseInt(txtPort.Text.Trim(), 8080);
            ConfigManager.UseProxy = chkProxy.Checked;

            if (cboLanguage.SelectedIndex == 1) ConfigManager.Language = "ru";
            else if (cboLanguage.SelectedIndex == 2) ConfigManager.Language = "en";
            else ConfigManager.Language = "auto";

            ConfigManager.Save();
            L10n.UpdateLanguage();
            GatewayClient.Instance.UpdateEndpoint(ConfigManager.ServerIp, ConfigManager.ServerPort);
        }

        private void btnAuth_Click(object sender, EventArgs e)
        {
            SaveConfigFromUi();

            if (_isFromChatList)
            {
                MessageBox.Show(L10n.SettingsSaved, L10n.SettingsTitle);
                this.Close();
                return;
            }

            btnAuth.Enabled = false;
            lblStatus.Text = L10n.Connecting;
            Application.DoEvents();

            string meName, meUser;
            bool isAuth = GatewayClient.Instance.CheckAuthStatus(out meName, out meUser);

            if (isAuth)
            {
                lblStatus.Text = "";
                if (!string.IsNullOrEmpty(meName))
                {
                    ConfigManager.UserName = meName;
                    ConfigManager.Save();
                }

                // Successful auth - navigate directly to chat list
                frmChatList chatList = new frmChatList();
                this.Hide();
                chatList.Show();
                return;
            }

            btnAuth.Enabled = true;
            string qrUrl = string.Format("http://{0}:{1}/qr", ConfigManager.ServerIp, ConfigManager.ServerPort);
            lblStatus.Text = L10n.IsRussian
                ? "Откройте " + qrUrl + "\nв браузере для авторизации"
                : "Open " + qrUrl + "\nin browser to log in";

            string notAuthMsg = string.Format(L10n.NotAuthorized, qrUrl);
            MessageBox.Show(notAuthMsg, L10n.AuthTitle);
        }

        private void menuBack_Click(object sender, EventArgs e)
        {
            if (_isFromChatList)
            {
                this.Close();
            }
            else
            {
                Application.Exit();
            }
        }

        private void menuOk_Click(object sender, EventArgs e)
        {
            btnAuth_Click(sender, e);
        }
    }
}
