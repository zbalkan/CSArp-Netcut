/*
 * CSArp 1.4
 * An arpspoofing program
 * Author : globalpolicy
 * Contact : yciloplabolg@gmail.com
 * Blog : c0dew0rth.blogspot.com
 * Github : globalpolicy
 * Time : May 9, 2017 @ 09:07PM
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SharpPcap;
using SharpPcap.WinPcap;
using SharpPcap.AirPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.IO;

namespace CSArp
{
    public class Controller
    {
        #region fields
        private readonly IView _view;
        #endregion

        #region constructor
        public Controller(IView view)
        {
            _view = view;
        }
        #endregion

        /// <summary>
        /// Populate the available network cards. Excludes bridged network adapters, since they are not applicable to spoofing scenario
        /// <see cref="https://github.com/chmorgan/sharppcap/issues/57"/>
        /// </summary>
        public void PopulateInterfaces()
        {
            var capturedevicelist = CaptureDeviceList.Instance;
            var captureDeviceNames = new List<string>();
            capturedevicelist.ToList().ForEach((ICaptureDevice capturedevice) =>
            {
                if (capturedevice is WinPcapDevice winpcapdevice)
                {
                    if (winpcapdevice.Interface.FriendlyName != null)
                    {
                        captureDeviceNames.Add(winpcapdevice.Interface.FriendlyName);
                    }
                }
                else if (capturedevice is AirPcapDevice airpcapdevice)
                {
                    if (airpcapdevice.Interface.FriendlyName != null)
                    {
                        captureDeviceNames.Add(airpcapdevice.Interface.FriendlyName);
                    }
                }
            });

            var nameArray = captureDeviceNames.ToArray();
            _view.ToolStripComboBoxDeviceList.Items.AddRange(nameArray);
        }

        /// <summary>
        /// Populate the LAN clients
        /// </summary>
        public void RefreshClients()
        {
            if (_view.ToolStripComboBoxDeviceList.Text != "") //if a network interface has been selected
            {
                if (_view.ToolStripStatusScan.Text.IndexOf("Scanning") == -1) //if a scan isn't active already
                {
                    DisconnectReconnect.Reconnect(); //first disengage spoofing threads
                    _view.ToolStripStatus.Text = "Ready";
                    GetClientList.GetAllClients(_view, _view.ToolStripComboBoxDeviceList.Text);
                }

            }
            else
            {
                _ = MessageBox.Show("Please select a network interface!", "Interface", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        /// <summary>
        /// Disconnects clients selected in the listview
        /// </summary>
        public void DisconnectSelectedClients()
        {
            if (_view.ListView1.SelectedItems.Count > 0)
            {
                var targetlist = new Dictionary<IPAddress, PhysicalAddress>();
                var parseindex = 0;
                foreach (ListViewItem listitem in _view.ListView1.SelectedItems)
                {
                    targetlist.Add(IPAddress.Parse(listitem.SubItems[1].Text), PhysicalAddress.Parse(listitem.SubItems[2].Text.Replace(":", "-")));
                    _ = _view.MainForm.BeginInvoke(new Action(() =>
                      {
                          _view.ListView1.SelectedItems[parseindex++].SubItems[3].Text = "Off";
                          _view.ToolStripStatus.Text = "Arpspoofing active...";
                      }));
                }
                DisconnectReconnect.Disconnect(_view, targetlist, GetGatewayIP(_view.ToolStripComboBoxDeviceList.Text), GetGatewayMAC(_view.ToolStripComboBoxDeviceList.Text), _view.ToolStripComboBoxDeviceList.Text);

            }
        }

        /// <summary>
        /// Reconnects clients by stopping fake ARP requests
        /// </summary>
        public void ReconnectClients() //selective reconnection not availabe at this time and frankly, not that useful
        {
            DisconnectReconnect.Reconnect();
            foreach (ListViewItem entry in _view.ListView1.Items)
            {
                entry.SubItems[3].Text = "On";
            }
            _view.ToolStripStatus.Text = "Stopped";
        }

        /// <summary>
        /// Sets the text of interface list combobox to saved value if present
        /// </summary>
        public void SetSavedInterface()
        {
            _view.ToolStripComboBoxDeviceList.Text = ApplicationSettingsClass.GetSavedPreferredInterfaceFriendlyName();
        }



        #region Trivial GUI elements control methods
        public void ShowAboutBox()
        {
            _ = MessageBox.Show("Author : globalpolicy\nContact : yciloplabolg@gmail.com\nBlog : c0dew0rth.blogspot.com\nGithub : globalpolicy\nContributions are welcome!", "About CSArp", MessageBoxButtons.OK);
        }
        public void EndApplication()
        {
            Application.Exit();
        }
        public void FormResized(object sender, EventArgs e)
        {
            if (_view.MainForm.WindowState == FormWindowState.Minimized)
            {
                _view.NotifyIcon1.Visible = true;
                _view.MainForm.Hide();
            }
        }
        public void InitializeNotifyIcon()
        {
            _view.NotifyIcon1.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _view.NotifyIcon1.MouseClick += (object sender, MouseEventArgs e) =>
            {
                _view.NotifyIcon1.Visible = false;
                _view.MainForm.Show();
                _view.MainForm.WindowState = FormWindowState.Normal;
            };
        }
        public void ToolStripTextBoxClientNameKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_view.ListView1.SelectedItems.Count == 1)
                {
                    _view.ListView1.SelectedItems[0].SubItems[4].Text = _view.ToolStripTextBoxClientName.Text;
                    _view.ToolStripTextBoxClientName.Text = "";
                }
            }
        }
        public void ToolStripMinimizeClicked()
        {
            _view.MainForm.WindowState = FormWindowState.Minimized;
        }
        public void ToolStripSaveClicked()
        {
            if (ApplicationSettingsClass.SaveSettings(_view.ListView1, _view.ToolStripComboBoxDeviceList.Text))
            {
                _view.ToolStripStatus.Text = "Settings saved!";
            }
        }
        public void AttachOnExitEventHandler()
        {
            Application.ApplicationExit += (object sender, EventArgs e) => GetClientList.CloseAllCaptures();
        }
        public void ShowLogToolStripMenuItemChecked()
        {
            if (_view.ShowLogToolStripMenuItem.Checked == false)
            {
                _view.LogRichTextBox.Visible = false;
                _view.ListView1.Height = _view.MainForm.Height - 93;
            }
            else
            {
                _view.LogRichTextBox.Visible = true;
                _view.ListView1.Height = _view.MainForm.Height - 184;
            }
        }
        public void SaveLogShowDialogBox()
        {
            _view.SaveFileDialogLog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            _view.SaveFileDialogLog.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _view.SaveFileDialogLog.FileName = "CSArp-log";
            _view.SaveFileDialogLog.FileOk += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                if (_view.SaveFileDialogLog.FileName != "" && !File.Exists(_view.SaveFileDialogLog.FileName))
                {
                    try
                    {
                        File.WriteAllText(_view.SaveFileDialogLog.FileName, _view.LogRichTextBox.Text);
                        DebugOutputClass.Print(_view, "Log saved to " + _view.SaveFileDialogLog.FileName);
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            _ = _view.SaveFileDialogLog.ShowDialog();
        }


        public void ClearLog()
        {
            _view.LogRichTextBox.Text = "";
        }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Return the gateway IPAddress of the selected network interface's
        /// </summary>
        /// <param name="friendlyname">The friendly name of the selected network interface</param>
        /// <returns>Returns the gateway IPAddress of the selected network interface's</returns>
        private IPAddress GetGatewayIP(string friendlyname)
        {
            IPAddress retval = null;
            var interfacename = "";
            foreach (var capturedevice in CaptureDeviceList.Instance)
            {
                if (capturedevice is WinPcapDevice winpcapdevice)
                {
                    if (winpcapdevice.Interface.FriendlyName == friendlyname)
                    {
                        interfacename = winpcapdevice.Interface.Name;
                    }
                }
                else if (capturedevice is AirPcapDevice airpcapdevice)
                {
                    if (airpcapdevice.Interface.FriendlyName == friendlyname)
                    {
                        interfacename = airpcapdevice.Interface.Name;
                    }
                }
            }
            if (interfacename != "")
            {
                foreach (var networkinterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkinterface.Name == friendlyname)
                    {
                        foreach (var gateway in networkinterface.GetIPProperties().GatewayAddresses)
                        {
                            if (gateway.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) //filter ipv4 gateway ip address
                            {
                                retval = gateway.Address;
                            }
                        }
                    }
                }
            }
            return retval;
        }
        private PhysicalAddress GetGatewayMAC(string friendlyname)
        {
            PhysicalAddress retval = null;
            var gatewayip = GetGatewayIP(friendlyname).ToString();
            foreach (ListViewItem listviewitem in _view.ListView1.Items)
            {
                if (listviewitem.SubItems[1].Text == gatewayip)
                {
                    retval = PhysicalAddress.Parse(listviewitem.SubItems[2].Text.Replace(":", "-"));
                }
            }
            return retval;
        }
        #endregion
    }
}
