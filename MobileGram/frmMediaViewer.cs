using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MobileGram.Network;

namespace MobileGram
{
    public partial class frmMediaViewer : Form
    {
        private long _chatId;
        private long _msgId;
        private Bitmap _loadedBitmap;

        public frmMediaViewer(long chatId, long msgId)
        {
            InitializeComponent();
            _chatId = chatId;
            _msgId = msgId;
            menuBack.Text = L10n.Back;
            this.Text = L10n.ViewMedia;
            LoadImage();
        }

        private void LoadImage()
        {
            lblStatus.Text = L10n.MediaLoading;
            lblStatus.Visible = true;

            new Thread(delegate()
            {
                Bitmap bmp = GatewayClient.Instance.FetchMedia(_chatId, _msgId, 230);
                this.Invoke((Action)delegate
                {
                    if (bmp != null)
                    {
                        _loadedBitmap = bmp;
                        picImage.Image = _loadedBitmap;
                        lblStatus.Visible = false;
                    }
                    else
                    {
                        lblStatus.Text = L10n.MediaFailed;
                    }
                });
            }).Start();
        }

        private void menuBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmMediaViewer_Closed(object sender, EventArgs e)
        {
            if (picImage.Image != null)
            {
                picImage.Image = null;
            }
            if (_loadedBitmap != null)
            {
                _loadedBitmap.Dispose();
                _loadedBitmap = null;
            }
        }
    }
}
