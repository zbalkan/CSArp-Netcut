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
        #region Public properties
        public string SelectedInterfaceFriendlyName { get; set; }
        #endregion

        #region Private fields
        private readonly IView _view;
        private IPAddress gatewayIpAddress;
        private PhysicalAddress gatewayPhysicalAddress;
        private GatewayIPAddressInformation gatewayInfo;

        #endregion

        #region constructor
        public Controller(IView view)
        {
            _view = view;
            ThreadBuffer.Init();
        }
        #endregion

        /// <summary>
        /// Populate the LAN clients
        /// </summary>
        public void RefreshClients()
        {
            if (!string.IsNullOrEmpty(SelectedInterfaceFriendlyName)) //if a network interface has been selected
            {
                if (_view.ToolStripStatusScan.Text.IndexOf("Scanning") == -1) //if a scan isn't active already
                {
                    DisconnectReconnect.Reconnect(); // first disengage spoofing threads
                    _ = _view.MainForm.BeginInvoke(new Action(() =>
                    {
                        _view.ToolStripStatus.Text = "Ready";
                    }));
                    GetClientList.GetAllClients(_view, SelectedInterfaceFriendlyName, gatewayIpAddress);
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
            // Guard clause
            if (_view.ClientListView.SelectedItems.Count == 0)
            {
                return;
            }

            foreach (ListViewItem item in _view.ClientListView.Items)
            {
                if (item.SubItems[1].Text == gatewayIpAddress.ToString())
                {
                    gatewayPhysicalAddress = PhysicalAddress.Parse(item.SubItems[2].Text.Replace(":", "-"));
                }
            }
            if (gatewayPhysicalAddress == null)
            {
                _ = MessageBox.Show("Gateway Physical Address still undiscovered. Please wait and try again.", "Warning", MessageBoxButtons.OK);
                return;
            }

            _ = _view.MainForm.Invoke(new Action(() =>
            {
                _view.ToolStripStatus.Text = "Arpspoofing active...";
            }));

            var targetlist = new Dictionary<IPAddress, PhysicalAddress>();
            var parseindex = 0;
            foreach (ListViewItem listitem in _view.ClientListView.SelectedItems)
            {
                targetlist.Add(IPAddress.Parse(listitem.SubItems[1].Text), PhysicalAddress.Parse(listitem.SubItems[2].Text.Replace(":", "-")));
                _ = _view.MainForm.BeginInvoke(new Action(() =>
                  {
                      _view.ClientListView.SelectedItems[parseindex++].SubItems[3].Text = "Off";
                  }));
            }
            DisconnectReconnect.Disconnect(_view, targetlist, gatewayIpAddress, gatewayPhysicalAddress, SelectedInterfaceFriendlyName);
        }

        /// <summary>
        /// Reconnects clients by stopping fake ARP requests
        /// </summary>
        public void ReconnectClients() //selective reconnection not availabe at this time and frankly, not that useful
        {
            DisconnectReconnect.Reconnect();
            foreach (ListViewItem entry in _view.ClientListView.Items)
            {
                entry.SubItems[3].Text = "On";
            }
            _view.ToolStripStatus.Text = "Stopped";
        }

        public void GetGatewayInformation()
        {
            if (string.IsNullOrEmpty(SelectedInterfaceFriendlyName))
            {
                return;
            }

            gatewayInfo = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == SelectedInterfaceFriendlyName).GetIPProperties().GatewayAddresses.FirstOrDefault(g => g.Address.AddressFamily
            == System.Net.Sockets.AddressFamily.InterNetwork);
            gatewayIpAddress = gatewayInfo.Address;
            GetClientList.PopulateCaptureDeviceInfo(_view, SelectedInterfaceFriendlyName);
        }
    }
}
