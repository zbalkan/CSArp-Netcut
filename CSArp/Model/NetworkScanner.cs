using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using SharpPcap;
using SharpPcap.WinPcap;
using PacketDotNet;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using CSArp.View;
using CSArp.Model.Utilities;
using CSArp.Model.Extensions;

/*
 Reference:
 http://stackoverflow.com/questions/14114971/sending-my-own-arp-packet-using-sharppcap-and-packet-net
 https://www.codeproject.com/Articles/12458/SharpPcap-A-Packet-Capture-Framework-for-NET
*/

namespace CSArp.Model
{
    // TODO: Add a scanning bool, to set the state for cancellation.
    // TODO: Remove GUI related code out of the class.
    public static class NetworkScanner
    {
        /// <summary>
        /// Populates listview with machines connected to the LAN
        /// </summary>
        /// <param name="view"></param>
        /// <param name="networkAdapter"></param>
        public static void StartScan(IView view, WinPcapDevice networkAdapter, IPAddress gatewayIp)
        {
            DebugOutput.Print("Refresh client list");
            #region initialization
            _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = "Please wait..."));
            _ = view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = 0));
            view.ClientListView.Items.Clear();
            #endregion

            // Clear ARP table
            ArpTable.Instance.Clear();

            // Start Foreground Scan with Timeout involved
            StartForegroundScan(view, networkAdapter, gatewayIp, 5000);
        }

        private static void StartForegroundScan(IView view, WinPcapDevice networkAdapter, IPAddress gatewayIp, int foregroundScanTimeout)
        {
            // Obtain subnet information
            var subnet = networkAdapter.ReadCurrentSubnet();

            // Obtain current IP address
            var sourceAddress = networkAdapter.ReadCurrentIpV4Address();

            // TODO: Send and capture ICMP packages for both MAC address and alive status.
            #region Sending ARP requests to probe for all possible IP addresses on LAN
            ThreadBuffer.Add(new Thread(() =>
            {
                InitiateArpRequestQueue(view, networkAdapter, gatewayIp);
            }));
            #endregion

            #region Retrieving ARP packets floating around and finding out the senders' IP and MACs
            networkAdapter.Filter = "arp";
            RawCapture rawcapture = null;
            ThreadBuffer.Add(new Thread(() =>
            {
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while ((rawcapture = networkAdapter.GetNextPacket()) != null && stopwatch.ElapsedMilliseconds <= foregroundScanTimeout)
                    {
                        var packet = Packet.ParsePacket(rawcapture.LinkLayerType, rawcapture.Data);
                        var arppacket = (ARPPacket)packet.Extract(typeof(ARPPacket));
                        if (!ArpTable.Instance.ContainsKey(arppacket.SenderProtocolAddress) && arppacket.SenderProtocolAddress.ToString() != "0.0.0.0" && subnet.Contains(arppacket.SenderProtocolAddress))
                        {
                            DebugOutput.Print("Added " + arppacket.SenderProtocolAddress.ToString() + " @ " + arppacket.SenderHardwareAddress.ToString("-"));
                            ArpTable.Instance.Add(arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                            _ = view.ClientListView.Invoke(new Action(() =>
                            {
                                _ = view.ClientListView.Items.Add(new ListViewItem(new string[] { arppacket.SenderProtocolAddress.ToString(), arppacket.SenderHardwareAddress.ToString("-"), "On", ApplicationSettings.GetSavedClientNameFromMAC(arppacket.SenderHardwareAddress.ToString("-")) }));
                            }));
                            //Debug.Print("{0} @ {1}", arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                        }
                        //int percentageprogress = (int)((float)stopwatch.ElapsedMilliseconds / scanduration * 100);
                        //view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = "Scanning " + percentageprogress + "%"));
                        //view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = percentageprogress));
                        //Debug.Print(packet.ToString() + "\n");
                    }
                    stopwatch.Stop();
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = ArpTable.Instance.Count.ToString() + " device(s) found"));
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = 100));
                    StartBackgroundScan(view, networkAdapter, gatewayIp); //start passive monitoring
                }
                catch (PcapException ex)
                {
                    DebugOutput.Print("PcapException @ GetClientList.StartForegroundScan() @ new Thread(()=>{}) while retrieving packets [" + ex.Message + "]");
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = "Refresh for scan"));
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = 0));
                }
                catch (Exception ex)
                {
                    DebugOutput.Print(ex.Message);
                }

            }));
            #endregion
        }

        /// <summary>
        /// Actively monitor ARP packets for signs of new clients after StartForegroundScan active scan is done
        /// </summary>
        public static void StartBackgroundScan(IView view, WinPcapDevice networkAdapter, IPAddress gatewayIp)
        {
            try
            {
                #region Sending ARP requests to probe for all possible IP addresses on LAN
                ThreadBuffer.Add(new Thread(() =>
                {
                    InitiateArpRequestQueue(view, networkAdapter, gatewayIp);
                }));
                #endregion

                #region Assign OnPacketArrival event handler and start capturing
                networkAdapter.OnPacketArrival += (sender, e) =>
                {
                    ParseArpResponse(view, networkAdapter.ReadCurrentSubnet(), gatewayIp, e);
                };
                #endregion
                networkAdapter.StartCapture();
            }
            catch (Exception ex)
            {
                DebugOutput.Print("Exception at GetClientList.BackgroundScanStart() [" + ex.Message + "]");
            }
        }
        // TODO: Start spoofing for devices regarding online status.
        private static void InitiateArpRequestQueue(IView view, WinPcapDevice networkAdapter, IPAddress gatewayIp)
        {
            try
            {
                // Obtain subnet information
                var subnet = networkAdapter.ReadCurrentSubnet();

                // Obtain current IP address
                var sourceAddress = networkAdapter.ReadCurrentIpV4Address();

                var addressList = new List<IPAddress> {
                                gatewayIp
                            }; // Ensure the ARP request is sent to gateway first, even if it means sending twice
                addressList.AddRange(subnet.ToList());

                // Remove current address from the list and add to ARP table statically
                _ = addressList.Remove(networkAdapter.ReadCurrentIpV4Address());
                ArpTable.Instance.Add(networkAdapter.ReadCurrentIpV4Address(), networkAdapter.MacAddress);

                // Start
                foreach (var targetIpAddress in addressList)
                {
                    ThreadBuffer.Add(new Thread(() =>
                    {
                        SendArpRequest(networkAdapter, targetIpAddress);
                    }));
                }
            }
            catch (PcapException ex)
            {
                DebugOutput.Print("PcapException @ GetClientList.InitiateArpRequestQueue() probably due to capturedevice being closed by refreshing or by exiting application [" + ex.Message + "]");
            }
            catch (OutOfMemoryException ex)
            {
                DebugOutput.Print($"PcapException @ GetClientList.InitiateArpRequestQueue() out of memory. \nTotal number of threads {ThreadBuffer.Count}\nTotal number of alive threads {ThreadBuffer.AliveCount}\n[" + ex.Message + "]");
            }
            catch (Exception ex)
            {
                DebugOutput.Print("Exception at GetClientList.InitiateArpRequestQueue() inside new Thread(()=>{}) while sending packets [" + ex.Message + "]");
            }
        }

        private static void SendArpRequest(WinPcapDevice networkAdapter, IPAddress targetIpAddress)
        {
            var arprequestpacket = new ARPPacket(ARPOperation.Request, "00-00-00-00-00-00".Parse(), targetIpAddress, networkAdapter.MacAddress, networkAdapter.ReadCurrentIpV4Address());
            var ethernetpacket = new EthernetPacket(networkAdapter.MacAddress, "FF-FF-FF-FF-FF-FF".Parse(), EthernetPacketType.Arp);
            ethernetpacket.PayloadPacket = arprequestpacket;
            networkAdapter.SendPacket(ethernetpacket);
            Debug.WriteLine("ARP request is sent to: {0}", targetIpAddress);
        }

        private static void ParseArpResponse(IView view, IPV4Subnet subnet, IPAddress gatewayIp, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            var arppacket = (ARPPacket)packet.Extract(typeof(ARPPacket));
            if (!ArpTable.Instance.ContainsKey(arppacket.SenderProtocolAddress) && arppacket.SenderProtocolAddress.ToString() != "0.0.0.0" && subnet.Contains(arppacket.SenderProtocolAddress))
            {
                var isGateway = false;
                if (arppacket.SenderProtocolAddress == gatewayIp)
                {
                    DebugOutput.Print("Found gateway!");
                    isGateway = true;
                }
                DebugOutput.Print("Added " + arppacket.SenderProtocolAddress.ToString() + " @ " + arppacket.SenderHardwareAddress.ToString("-") + " from background scan!");
                ArpTable.Instance.Add(arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                _ = view.ClientListView.Invoke(new Action(() =>
                {
                    _ = isGateway
                        ? view.ClientListView.Items.Add(new ListViewItem(new string[] { arppacket.SenderProtocolAddress.ToString(), arppacket.SenderHardwareAddress.ToString("-"), "On", "GATEWAY" }))
                        : view.ClientListView.Items.Add(new ListViewItem(new string[] { arppacket.SenderProtocolAddress.ToString(), arppacket.SenderHardwareAddress.ToString("-"), "On", ApplicationSettings.GetSavedClientNameFromMAC(arppacket.SenderHardwareAddress.ToString("-")) }));
                }));
                _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = ArpTable.Instance.Count + " device(s) found"));
            }
        }


    }
}
