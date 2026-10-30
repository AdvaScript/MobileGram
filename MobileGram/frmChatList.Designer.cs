namespace MobileGram
{
    partial class frmChatList
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuAction;
        private System.Windows.Forms.MenuItem menuSearch;
        private System.Windows.Forms.MenuItem menuExit;
        private System.Windows.Forms.MenuItem menuSettings;
        private System.Windows.Forms.MenuItem menuRefresh;
        private System.Windows.Forms.MenuItem menuAbout;
        private System.Windows.Forms.MenuItem menuExitApp;
        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.Button btnFilterAll;
        private System.Windows.Forms.Button btnFilterChats;
        private System.Windows.Forms.Button btnFilterGroups;
        private MobileGram.Controls.ChatListControl chatListControl;

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
            this.menuAction = new System.Windows.Forms.MenuItem();
            this.menuSearch = new System.Windows.Forms.MenuItem();
            this.menuSettings = new System.Windows.Forms.MenuItem();
            this.menuRefresh = new System.Windows.Forms.MenuItem();
            this.menuAbout = new System.Windows.Forms.MenuItem();
            this.menuExitApp = new System.Windows.Forms.MenuItem();
            this.menuExit = new System.Windows.Forms.MenuItem();
            this.pnlFilter = new System.Windows.Forms.Panel();
            this.btnFilterGroups = new System.Windows.Forms.Button();
            this.btnFilterChats = new System.Windows.Forms.Button();
            this.btnFilterAll = new System.Windows.Forms.Button();
            this.chatListControl = new MobileGram.Controls.ChatListControl();
            this.pnlFilter.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuAction);
            this.mainMenu1.MenuItems.Add(this.menuExit);
            // 
            // menuAction
            // 
            this.menuAction.MenuItems.Add(this.menuSearch);
            this.menuAction.MenuItems.Add(this.menuSettings);
            this.menuAction.MenuItems.Add(this.menuRefresh);
            this.menuAction.MenuItems.Add(this.menuAbout);
            this.menuAction.MenuItems.Add(this.menuExitApp);
            this.menuAction.Text = "Меню";
            // 
            // menuSearch
            // 
            this.menuSearch.Text = "Поиск в Telegram...";
            this.menuSearch.Click += new System.EventHandler(this.menuSearch_Click);
            // 
            // menuSettings
            // 
            this.menuSettings.Text = "Настройки";
            this.menuSettings.Click += new System.EventHandler(this.menuSettings_Click);
            // 
            // menuRefresh
            // 
            this.menuRefresh.Text = "Обновить";
            this.menuRefresh.Click += new System.EventHandler(this.menuRefresh_Click);
            // 
            // menuAbout
            // 
            this.menuAbout.Text = "О программе MobileGram";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);
            // 
            // menuExitApp
            // 
            this.menuExitApp.Text = "Выход";
            this.menuExitApp.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // menuExit
            // 
            this.menuExit.Text = "Выход";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // pnlFilter
            // 
            this.pnlFilter.BackColor = System.Drawing.Color.FromArgb(236, 233, 216);
            this.pnlFilter.Controls.Add(this.btnFilterGroups);
            this.pnlFilter.Controls.Add(this.btnFilterChats);
            this.pnlFilter.Controls.Add(this.btnFilterAll);
            this.pnlFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilter.Location = new System.Drawing.Point(0, 0);
            this.pnlFilter.Name = "pnlFilter";
            this.pnlFilter.Size = new System.Drawing.Size(240, 22);
            // 
            // btnFilterGroups
            // 
            this.btnFilterGroups.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Regular);
            this.btnFilterGroups.Location = new System.Drawing.Point(160, 1);
            this.btnFilterGroups.Name = "btnFilterGroups";
            this.btnFilterGroups.Size = new System.Drawing.Size(78, 20);
            this.btnFilterGroups.TabIndex = 2;
            this.btnFilterGroups.Text = "Группы";
            this.btnFilterGroups.Click += new System.EventHandler(this.btnFilterGroups_Click);
            // 
            // btnFilterChats
            // 
            this.btnFilterChats.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Regular);
            this.btnFilterChats.Location = new System.Drawing.Point(81, 1);
            this.btnFilterChats.Name = "btnFilterChats";
            this.btnFilterChats.Size = new System.Drawing.Size(78, 20);
            this.btnFilterChats.TabIndex = 1;
            this.btnFilterChats.Text = "Чаты";
            this.btnFilterChats.Click += new System.EventHandler(this.btnFilterChats_Click);
            // 
            // btnFilterAll
            // 
            this.btnFilterAll.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Bold);
            this.btnFilterAll.Location = new System.Drawing.Point(2, 1);
            this.btnFilterAll.Name = "btnFilterAll";
            this.btnFilterAll.Size = new System.Drawing.Size(78, 20);
            this.btnFilterAll.TabIndex = 0;
            this.btnFilterAll.Text = "Все";
            this.btnFilterAll.Click += new System.EventHandler(this.btnFilterAll_Click);
            // 
            // chatListControl
            // 
            this.chatListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chatListControl.Location = new System.Drawing.Point(0, 22);
            this.chatListControl.Name = "chatListControl";
            this.chatListControl.Size = new System.Drawing.Size(240, 246);
            this.chatListControl.TabIndex = 1;
            // 
            // frmChatList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.chatListControl);
            this.Controls.Add(this.pnlFilter);
            this.Menu = this.mainMenu1;
            this.Name = "frmChatList";
            this.Text = "MobileGram (Beta)";
            this.Closed += new System.EventHandler(this.frmChatList_Closed);
            this.Activated += new System.EventHandler(this.frmChatList_Activated);
            this.pnlFilter.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
