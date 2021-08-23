using System;
using System.Windows.Forms;

namespace CSArp
{
    public partial class Form1 : Form, IView
    {
        private readonly Controller _controller;
        public Form1()
        {
            InitializeComponent();
            _controller = new Controller(this);
        }



        #region IView members
        public ListView ClientListView
        {
            get
            {
                return listView1;
            }
        }
        public ToolStripStatusLabel ToolStripStatus
        {
            get
            {
                return toolStripStatus;
            }
        }
        public ToolStripComboBox ToolStripComboBoxNetworkDeviceList
        {
            get
            {
                return toolStripComboBoxDevicelist;
            }
        }
        public Form MainForm
        {
            get
            {
                return this;
            }
        }
        public NotifyIcon NotifyIcon1
        {
            get
            {
                return notifyIcon1;
            }
        }
        public ToolStripTextBox ToolStripTextBoxClientName
        {
            get
            {
                return toolStripTextBoxClientName;
            }
        }
        public ToolStripStatusLabel ToolStripStatusScan
        {
            get
            {
                return toolStripStatusScan;
            }
        }
        public ToolStripProgressBar ToolStripProgressBarScan
        {
            get
            {
                return toolStripProgressBarScan;
            }
        }
        public ToolStripMenuItem ShowLogToolStripMenuItem
        {
            get
            {
                return showLogToolStripMenuItem;
            }
        }
        public RichTextBox LogRichTextBox
        {
            get
            {
                return richTextBoxLog;
            }
        }
        public SaveFileDialog SaveFileDialogLog
        {
            get
            {
                return saveFileDialog1;
            }
        }
        #endregion

        private void toolStripMenuItemRefreshClients_Click(object sender, EventArgs e)
        {
            _controller.SetFriendlyName();
            _controller.GetGatewayInformation();
            _controller.RefreshClients();
        }

        private void aboutCSArpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show("Author : globalpolicy\nContact : yciloplabolg@gmail.com\nBlog : c0dew0rth.blogspot.com\nGithub : globalpolicy\nContributions are welcome!\n\nContributors:\nZafer Balkan : zafer@zaferbalkan.com", "About CSArp", MessageBoxButtons.OK);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _controller.EnumerateNetworkAdaptersforMenu();
            _controller.SetSavedInterface();
            _controller.SetFriendlyName();
            _controller.GetGatewayInformation();
        }

        private void cutoffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _controller.DisconnectSelectedClients();
        }

        private void reconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _controller.ReconnectClients();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (MainForm.WindowState == FormWindowState.Minimized)
            {
                NotifyIcon1.Visible = true;
                MainForm.Hide();
            }
        }

        private void toolStripTextBoxClientName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (ClientListView.SelectedItems.Count == 1)
                {
                    ClientListView.SelectedItems[0].SubItems[4].Text = ToolStripTextBoxClientName.Text;
                    ToolStripTextBoxClientName.Text = "";
                }
            }
        }

        private void toolStripMenuItemMinimize_Click(object sender, EventArgs e)
        {
            MainForm.WindowState = FormWindowState.Minimized;
        }

        private void toolStripMenuItemSaveSettings_Click(object sender, EventArgs e)
        {
            if (ApplicationSettings.SaveSettings(ClientListView, ToolStripComboBoxNetworkDeviceList.Text))
            {
                ToolStripStatus.Text = "Settings saved!";
            }
        }

        private void showLogToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (ShowLogToolStripMenuItem.Checked == false)
            {
                LogRichTextBox.Visible = false;
                ClientListView.Height = MainForm.Height - 93;
            }
            else
            {
                LogRichTextBox.Visible = true;
                ClientListView.Height = MainForm.Height - 184;
            }
        }

        private void saveStripMenuItem_Click(object sender, EventArgs e)
        {
            _controller.SaveLog();
        }

        private void clearStripMenuItem_Click(object sender, EventArgs e)
        {
            LogRichTextBox.Text = "";
        }
        private void notifyIcon1_OnMouseClick(object sender, EventArgs e)
        {
            NotifyIcon1.Visible = false;
            MainForm.Show();
            MainForm.WindowState = FormWindowState.Normal;
        }
    }
}
