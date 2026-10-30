namespace MobileGram
{
    partial class frmTopicList
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuBack;
        private System.Windows.Forms.MenuItem menuOpen;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.ListView lstTopics;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colUnread;

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
            this.menuOpen = new System.Windows.Forms.MenuItem();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lstTopics = new System.Windows.Forms.ListView();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.colUnread = new System.Windows.Forms.ColumnHeader();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuBack);
            this.mainMenu1.MenuItems.Add(this.menuOpen);
            // 
            // menuBack
            // 
            this.menuBack.Text = "Назад";
            this.menuBack.Click += new System.EventHandler(this.menuBack_Click);
            // 
            // menuOpen
            // 
            this.menuOpen.Text = "Открыть";
            this.menuOpen.Click += new System.EventHandler(this.menuOpen_Click);
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(224, 237, 248);
            this.pnlHeader.Controls.Add(this.lblHeaderTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(240, 24);
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.FromArgb(0, 51, 153);
            this.lblHeaderTitle.Location = new System.Drawing.Point(6, 4);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(228, 16);
            this.lblHeaderTitle.Text = "Выберите тему форума:";
            // 
            // lstTopics
            // 
            this.lstTopics.Columns.Add(this.colTitle);
            this.lstTopics.Columns.Add(this.colUnread);
            this.lstTopics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstTopics.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular);
            this.lstTopics.FullRowSelect = true;
            this.lstTopics.Location = new System.Drawing.Point(0, 24);
            this.lstTopics.Name = "lstTopics";
            this.lstTopics.Size = new System.Drawing.Size(240, 244);
            this.lstTopics.TabIndex = 1;
            this.lstTopics.View = System.Windows.Forms.View.Details;
            this.lstTopics.ItemActivate += new System.EventHandler(this.lstTopics_ItemActivate);
            // 
            // colTitle
            // 
            this.colTitle.Text = "Тема";
            this.colTitle.Width = 175;
            // 
            // colUnread
            // 
            this.colUnread.Text = "Новые";
            this.colUnread.Width = 55;
            // 
            // frmTopicList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.lstTopics);
            this.Controls.Add(this.pnlHeader);
            this.Menu = this.mainMenu1;
            this.Name = "frmTopicList";
            this.Text = "Темы форума";
            this.pnlHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
