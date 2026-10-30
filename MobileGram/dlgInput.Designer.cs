namespace MobileGram
{
    partial class dlgInput
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuCancel;
        private System.Windows.Forms.MenuItem menuOk;
        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

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
            this.menuCancel = new System.Windows.Forms.MenuItem();
            this.menuOk = new System.Windows.Forms.MenuItem();
            this.lblPrompt = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuCancel);
            this.mainMenu1.MenuItems.Add(this.menuOk);
            // 
            // menuCancel
            // 
            this.menuCancel.Text = "Отмена";
            this.menuCancel.Click += new System.EventHandler(this.menuCancel_Click);
            // 
            // menuOk
            // 
            this.menuOk.Text = "OK";
            this.menuOk.Click += new System.EventHandler(this.menuOk_Click);
            // 
            // lblPrompt
            // 
            this.lblPrompt.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular);
            this.lblPrompt.Location = new System.Drawing.Point(10, 16);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Size = new System.Drawing.Size(220, 36);
            this.lblPrompt.Text = "Введите код подтверждения Telegram:";
            // 
            // txtValue
            // 
            this.txtValue.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
            this.txtValue.Location = new System.Drawing.Point(10, 56);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(220, 21);
            this.txtValue.TabIndex = 0;
            this.txtValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtValue_KeyDown);
            // 
            // btnOk
            // 
            this.btnOk.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Bold);
            this.btnOk.Location = new System.Drawing.Point(30, 96);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 24);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
            this.btnCancel.Location = new System.Drawing.Point(130, 96);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 24);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // dlgInput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(240, 150);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblPrompt);
            this.Menu = this.mainMenu1;
            this.Name = "dlgInput";
            this.Text = "Авторизация Telegram";
            this.ResumeLayout(false);

        }
    }
}
