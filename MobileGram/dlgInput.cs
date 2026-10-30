using System;
using System.Windows.Forms;

namespace MobileGram
{
    public partial class dlgInput : Form
    {
        public string Value
        {
            get { return txtValue.Text.Trim(); }
            set { txtValue.Text = value; }
        }

        public dlgInput(string title, string prompt, bool isPassword)
        {
            InitializeComponent();
            menuCancel.Text = L10n.Cancel;
            btnCancel.Text = L10n.Cancel;
            menuOk.Text = L10n.Ok;
            btnOk.Text = L10n.Ok;
            this.Text = title;
            lblPrompt.Text = prompt;
            if (isPassword)
            {
                txtValue.PasswordChar = '*';
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void menuOk_Click(object sender, EventArgs e)
        {
            btnOk_Click(sender, e);
        }

        private void menuCancel_Click(object sender, EventArgs e)
        {
            btnCancel_Click(sender, e);
        }

        private void txtValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {
                btnOk_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
