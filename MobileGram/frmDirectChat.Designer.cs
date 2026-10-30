namespace MobileGram
{
    partial class frmDirectChat
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuBack;
        private System.Windows.Forms.MenuItem menuOptions;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlReplyBar;
        private System.Windows.Forms.Label lblReplyPreview;
        private System.Windows.Forms.Button btnCancelReply;
        private System.Windows.Forms.Panel pnlInput;
        private System.Windows.Forms.Panel pnlTyping;
        private System.Windows.Forms.Label lblTyping;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private MobileGram.Controls.PocketMsnChatControl chatControl;
        private Microsoft.WindowsCE.Forms.InputPanel inputPanel1;

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
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.menuBack = new System.Windows.Forms.MenuItem();
            this.menuOptions = new System.Windows.Forms.MenuItem();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlReplyBar = new System.Windows.Forms.Panel();
            this.lblReplyPreview = new System.Windows.Forms.Label();
            this.btnCancelReply = new System.Windows.Forms.Button();
            this.pnlInput = new System.Windows.Forms.Panel();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.pnlTyping = new System.Windows.Forms.Panel();
            this.lblTyping = new System.Windows.Forms.Label();
            this.chatControl = new MobileGram.Controls.PocketMsnChatControl();
            this.inputPanel1 = new Microsoft.WindowsCE.Forms.InputPanel(this.components);
            this.pnlReplyBar.SuspendLayout();
            this.pnlInput.SuspendLayout();
            this.pnlTyping.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuBack);
            this.mainMenu1.MenuItems.Add(this.menuOptions);
            // 
            // menuBack
            // 
            this.menuBack.Text = "Назад";
            this.menuBack.Click += new System.EventHandler(this.menuBack_Click);
            // 
            // menuOptions
            // 
            this.menuOptions.Text = "Опции";
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(224, 237, 248);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(240, 20);
            this.pnlHeader.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlHeader_Paint);
            this.pnlHeader.Click += new System.EventHandler(this.pnlHeader_Click);
            // 
            // pnlReplyBar
            // 
            this.pnlReplyBar.BackColor = System.Drawing.Color.FromArgb(240, 246, 252);
            this.pnlReplyBar.Controls.Add(this.btnCancelReply);
            this.pnlReplyBar.Controls.Add(this.lblReplyPreview);
            this.pnlReplyBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlReplyBar.Location = new System.Drawing.Point(0, 216);
            this.pnlReplyBar.Name = "pnlReplyBar";
            this.pnlReplyBar.Size = new System.Drawing.Size(240, 18);
            this.pnlReplyBar.Visible = false;
            // 
            // lblReplyPreview
            // 
            this.lblReplyPreview.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Regular);
            this.lblReplyPreview.ForeColor = System.Drawing.Color.FromArgb(49, 106, 197);
            this.lblReplyPreview.Location = new System.Drawing.Point(4, 2);
            this.lblReplyPreview.Name = "lblReplyPreview";
            this.lblReplyPreview.Size = new System.Drawing.Size(212, 14);
            this.lblReplyPreview.Text = "↳ Ответ: ...";
            // 
            // btnCancelReply
            // 
            this.btnCancelReply.Font = new System.Drawing.Font("Tahoma", 6.5F, System.Drawing.FontStyle.Bold);
            this.btnCancelReply.Location = new System.Drawing.Point(220, 1);
            this.btnCancelReply.Name = "btnCancelReply";
            this.btnCancelReply.Size = new System.Drawing.Size(18, 16);
            this.btnCancelReply.TabIndex = 1;
            this.btnCancelReply.Text = "X";
            this.btnCancelReply.Click += new System.EventHandler(this.btnCancelReply_Click);
            // 
            // pnlInput
            // 
            this.pnlInput.BackColor = System.Drawing.Color.FromArgb(236, 233, 216);
            this.pnlInput.Controls.Add(this.btnSend);
            this.pnlInput.Controls.Add(this.txtMessage);
            this.pnlInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlInput.Location = new System.Drawing.Point(0, 234);
            this.pnlInput.Name = "pnlInput";
            this.pnlInput.Size = new System.Drawing.Size(240, 34);
            this.pnlInput.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlInput_Paint);
            // 
            // btnSend
            // 
            this.btnSend.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Bold);
            this.btnSend.Location = new System.Drawing.Point(184, 5);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(52, 24);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Отпр.";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular);
            this.txtMessage.Location = new System.Drawing.Point(3, 5);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(178, 21);
            this.txtMessage.TabIndex = 0;
            this.txtMessage.GotFocus += new System.EventHandler(this.txtMessage_GotFocus);
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);
            // 
            // pnlTyping
            // 
            this.pnlTyping.BackColor = System.Drawing.Color.FromArgb(244, 244, 244);
            this.pnlTyping.Controls.Add(this.lblTyping);
            this.pnlTyping.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlTyping.Location = new System.Drawing.Point(0, 198);
            this.pnlTyping.Name = "pnlTyping";
            this.pnlTyping.Size = new System.Drawing.Size(240, 18);
            this.pnlTyping.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlTyping_Paint);
            // 
            // lblTyping
            // 
            this.lblTyping.Font = new System.Drawing.Font("Tahoma", 7.5F, System.Drawing.FontStyle.Italic);
            this.lblTyping.ForeColor = System.Drawing.Color.FromArgb(0, 85, 170);
            this.lblTyping.Location = new System.Drawing.Point(26, 2);
            this.lblTyping.Name = "lblTyping";
            this.lblTyping.Size = new System.Drawing.Size(210, 14);
            this.lblTyping.Text = "Печатает...";
            // 
            // chatControl
            // 
            this.chatControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chatControl.Location = new System.Drawing.Point(0, 20);
            this.chatControl.Name = "chatControl";
            this.chatControl.Size = new System.Drawing.Size(240, 178);
            this.chatControl.TabIndex = 3;
            // 
            // inputPanel1
            // 
            this.inputPanel1.EnabledChanged += new System.EventHandler(this.inputPanel1_EnabledChanged);
            // 
            // frmDirectChat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.chatControl);
            this.Controls.Add(this.pnlTyping);
            this.Controls.Add(this.pnlReplyBar);
            this.Controls.Add(this.pnlInput);
            this.Controls.Add(this.pnlHeader);
            this.Menu = this.mainMenu1;
            this.Name = "frmDirectChat";
            this.Text = "Чат";
            this.Closed += new System.EventHandler(this.frmDirectChat_Closed);
            this.pnlReplyBar.ResumeLayout(false);
            this.pnlInput.ResumeLayout(false);
            this.pnlTyping.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
