namespace MobileGram
{
    partial class frmContactProfile
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuBack;
        private System.Windows.Forms.MenuItem menuOk;
        private System.Windows.Forms.Panel pnlAvatar;
        private System.Windows.Forms.Label lblDisplayName;
        private System.Windows.Forms.Label lblPresence;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.ListView lstDetails;
        private System.Windows.Forms.ColumnHeader colParam;
        private System.Windows.Forms.ColumnHeader colValue;

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
            this.pnlAvatar = new System.Windows.Forms.Panel();
            this.lblDisplayName = new System.Windows.Forms.Label();
            this.lblPresence = new System.Windows.Forms.Label();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lstDetails = new System.Windows.Forms.ListView();
            this.colParam = new System.Windows.Forms.ColumnHeader();
            this.colValue = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuBack);
            this.mainMenu1.MenuItems.Add(this.menuOk);
            // 
            // menuBack
            // 
            this.menuBack.Text = "Назад";
            this.menuBack.Click += new System.EventHandler(this.menuBack_Click);
            // 
            // menuOk
            // 
            this.menuOk.Text = "OK";
            this.menuOk.Click += new System.EventHandler(this.menuOk_Click);
            // 
            // pnlAvatar
            // 
            this.pnlAvatar.BackColor = System.Drawing.Color.FromArgb(70, 130, 180);
            this.pnlAvatar.Location = new System.Drawing.Point(8, 8);
            this.pnlAvatar.Name = "pnlAvatar";
            this.pnlAvatar.Size = new System.Drawing.Size(64, 64);
            this.pnlAvatar.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlAvatar_Paint);
            // 
            // lblDisplayName
            // 
            this.lblDisplayName.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblDisplayName.Location = new System.Drawing.Point(80, 10);
            this.lblDisplayName.Name = "lblDisplayName";
            this.lblDisplayName.Size = new System.Drawing.Size(150, 16);
            this.lblDisplayName.Text = "Профиль";
            // 
            // lblPresence
            // 
            this.lblPresence.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Bold);
            this.lblPresence.ForeColor = System.Drawing.Color.FromArgb(0, 128, 0);
            this.lblPresence.Location = new System.Drawing.Point(80, 28);
            this.lblPresence.Name = "lblPresence";
            this.lblPresence.Size = new System.Drawing.Size(150, 14);
            this.lblPresence.Text = "";
            // 
            // lblUsername
            // 
            this.lblUsername.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(85, 85, 85);
            this.lblUsername.Location = new System.Drawing.Point(80, 44);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(150, 14);
            this.lblUsername.Text = "";
            // 
            // lstDetails
            // 
            this.lstDetails.Columns.Add(this.colParam);
            this.lstDetails.Columns.Add(this.colValue);
            this.lstDetails.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.lstDetails.FullRowSelect = true;
            this.lstDetails.Location = new System.Drawing.Point(6, 80);
            this.lstDetails.Name = "lstDetails";
            this.lstDetails.Size = new System.Drawing.Size(228, 180);
            this.lstDetails.TabIndex = 4;
            this.lstDetails.View = System.Windows.Forms.View.Details;
            // 
            // colParam
            // 
            this.colParam.Text = "Параметр";
            this.colParam.Width = 95;
            // 
            // colValue
            // 
            this.colValue.Text = "Значение";
            this.colValue.Width = 125;
            // 
            // frmContactProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.lstDetails);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.lblPresence);
            this.Controls.Add(this.lblDisplayName);
            this.Controls.Add(this.pnlAvatar);
            this.Menu = this.mainMenu1;
            this.Name = "frmContactProfile";
            this.Text = "Профиль";
            this.ResumeLayout(false);
        }
    }
}
