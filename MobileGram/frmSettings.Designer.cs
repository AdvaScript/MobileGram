namespace MobileGram
{
    partial class frmSettings
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuBack;
        private System.Windows.Forms.MenuItem menuOk;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.CheckBox chkProxy;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.ComboBox cboLanguage;
        private System.Windows.Forms.Button btnAuth;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.menuBack = new System.Windows.Forms.MenuItem();
            this.menuOk = new System.Windows.Forms.MenuItem();
            this.lblServer = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.chkProxy = new System.Windows.Forms.CheckBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.cboLanguage = new System.Windows.Forms.ComboBox();
            this.btnAuth = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuBack);
            this.mainMenu1.MenuItems.Add(this.menuOk);
            // 
            // menuBack
            // 
            this.menuBack.Text = "Выход";
            this.menuBack.Click += new System.EventHandler(this.menuBack_Click);
            // 
            // menuOk
            // 
            this.menuOk.Text = "Войти";
            this.menuOk.Click += new System.EventHandler(this.menuOk_Click);
            // 
            // lblServer
            // 
            this.lblServer.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.lblServer.Location = new System.Drawing.Point(8, 8);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(140, 14);
            this.lblServer.Text = "Сервер-мост (IP):";
            // 
            // txtServer
            // 
            this.txtServer.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.txtServer.Location = new System.Drawing.Point(8, 24);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(142, 21);
            this.txtServer.TabIndex = 1;
            this.txtServer.Text = "192.168.1.100";
            // 
            // lblPort
            // 
            this.lblPort.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.lblPort.Location = new System.Drawing.Point(158, 8);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(74, 14);
            this.lblPort.Text = "Порт:";
            // 
            // txtPort
            // 
            this.txtPort.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.txtPort.Location = new System.Drawing.Point(158, 24);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(74, 21);
            this.txtPort.TabIndex = 2;
            this.txtPort.Text = "8080";
            // 
            // chkProxy
            // 
            this.chkProxy.Checked = true;
            this.chkProxy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkProxy.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.chkProxy.Location = new System.Drawing.Point(8, 52);
            this.chkProxy.Name = "chkProxy";
            this.chkProxy.Size = new System.Drawing.Size(224, 20);
            this.chkProxy.TabIndex = 3;
            this.chkProxy.Text = "Использовать прокси";
            // 
            // lblLanguage
            // 
            this.lblLanguage.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.lblLanguage.Location = new System.Drawing.Point(8, 78);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(224, 14);
            this.lblLanguage.Text = "Язык интерфейса:";
            // 
            // cboLanguage
            // 
            this.cboLanguage.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.cboLanguage.Location = new System.Drawing.Point(8, 94);
            this.cboLanguage.Name = "cboLanguage";
            this.cboLanguage.Size = new System.Drawing.Size(224, 21);
            this.cboLanguage.TabIndex = 4;
            // 
            // btnAuth
            // 
            this.btnAuth.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.btnAuth.Location = new System.Drawing.Point(20, 130);
            this.btnAuth.Name = "btnAuth";
            this.btnAuth.Size = new System.Drawing.Size(200, 32);
            this.btnAuth.TabIndex = 5;
            this.btnAuth.Text = "Войти";
            this.btnAuth.Click += new System.EventHandler(this.btnAuth_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Regular);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(85, 85, 85);
            this.lblStatus.Location = new System.Drawing.Point(8, 172);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(224, 76);
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // frmSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnAuth);
            this.Controls.Add(this.cboLanguage);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.chkProxy);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtServer);
            this.Controls.Add(this.lblServer);
            this.Menu = this.mainMenu1;
            this.Name = "frmSettings";
            this.Text = "MobileGram";
            this.ResumeLayout(false);

        }
    }
}
