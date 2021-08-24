using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using SharpPcap;
using SharpPcap.WinPcap;
using PacketDotNet;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

/*
 Reference:
 http://stackoverflow.com/questions/14114971/sending-my-own-arp-packet-using-sharppcap-and-packet-net
 https://www.codeproject.com/Articles/12458/SharpPcap-A-Packet-Capture-Framework-for-NET
*/

namespace CSArp
{
    // TODO: Add a scanning bool, to set the state for cancellation.
    // TODO: Remove GUI related code out of the class.
    public static class NetworkScanner
    {
        /// <summary>
        /// Populates listview with machines connected to the LAN
        /// </summary>
        /// <param name="view"></param>
        /// <param name="selectedDevice"></param>
        public static void StartForegroundScan(IView view, WinPcapDevice selectedDevice, IPAddress sourceAddress, IPAddress gatewayIp, IPV4Subnet subnet)
        {
            DebugOutput.Print(view, "Refresh client list");
            #region initialization
            _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = "Please wait..."));
            _ = view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = 0));
            view.ClientListView.Items.Clear();
            #endregion

            // Clear ARP table
            ArpTable.Instance.Clear();

            // TODO: Send and capture ICMP packages for both MAC address and alive status.
            #region Sending ARP requests to probe for all possible IP addresses on LAN
            ThreadBuffer.Add(new Thread(() =>
            {
                InitiateArpRequestQueue(view, selectedDevice, sourceAddress, gatewayIp, subnet);
            }));
            #endregion

            #region Retrieving ARP packets floating around and finding out the senders' IP and MACs
            selectedDevice.Filter = "arp";
            RawCapture rawcapture = null;
            long scanduration = 5000;
            ThreadBuffer.Add(new Thread(() =>
            {
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while ((rawcapture = selectedDevice.GetNextPacket()) != null && stopwatch.ElapsedMilliseconds <= scanduration)
                    {
                        var packet = Packet.ParsePacket(rawcapture.LinkLayerType, rawcapture.Data);
                        var arppacket = (ARPPacket)packet.Extract(typeof(ARPPacket));
                        if (!ArpTable.Instance.ContainsKey(arppacket.SenderProtocolAddress) && arppacket.SenderProtocolAddress.ToString() != "0.0.0.0" && subnet.Contains(arppacket.SenderProtocolAddress))
                        {
                            DebugOutput.Print(view, "Added " + arppacket.SenderProtocolAddress.ToString() + " @ " + GetMACString(arppacket.SenderHardwareAddress));
                            ArpTable.Instance.Add(arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                            _ = view.ClientListView.Invoke(new Action(() =>
                            {
                                _ = view.ClientListView.Items.Add(new ListViewItem(new string[] { arppacket.SenderProtocolAddress.ToString(), GetMACString(arppacket.SenderHardwareAddress), "On", ApplicationSettings.GetSavedClientNameFromMAC(GetMACString(arppacket.SenderHardwareAddress)) }));
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
                    StartBackgroundScan(view, selectedDevice, sourceAddress, gatewayIp, subnet); //start passive monitoring
                }
                catch (PcapException ex)
                {
                    DebugOutput.Print(view, "PcapException @ GetClientList.StartForegroundScan() @ new Thread(()=>{}) while retrieving packets [" + ex.Message + "]");
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = "Refresh for scan"));
                    _ = view.MainForm.Invoke(new Action(() => view.ToolStripProgressBarScan.Value = 0));
                }
                catch (Exception ex)
                {
                    DebugOutput.Print(view, ex.Message);
                }

            }));
            #endregion
        }

        /// <summary>
        /// Actively monitor ARP packets for signs of new clients after StartForegroundScan active scan is done
        /// </summary>
        public static void StartBackgroundScan(IView view, WinPcapDevice selectedDevice, IPAddress sourceAddress, IPAddress gatewayIp, IPV4Subnet subnet)
        {
            try
            {
                #region Sending ARP requests to probe for all possible IP addresses on LAN
                ThreadBuffer.Add(new Thread(() =>
                {
                    InitiateArpRequestQueue(view, selectedDevice, sourceAddress, gatewayIp, subnet);
                }));
                #endregion

                #region Assign OnPacketArrival event handler and start capturing
                selectedDevice.OnPacketArrival += (object sender, CaptureEventArgs e) =>
                {
                    ParseArpResponse(view, subnet, e);
                };
                #endregion
                selectedDevice.StartCapture();
            }
            catch (Exception ex)
            {
                DebugOutput.Print(view, "Exception at GetClientList.BackgroundScanStart() [" + ex.Message + "]");
            }
        }
        // TODO: Start spoofing for devices regarding online status.
        private static void InitiateArpRequestQueue(IView view, WinPcapDevice device, IPAddress sourceAddress ,IPAddress gatewayIp, IPV4Subnet subnet)
        {
            try
            {
                var addressList = new List<IPAddress> {
                                gatewayIp
                            }; // Ensure the ARP request is sent to gateway first
                addressList.AddRange(subnet.ToList());

                foreach (var targetIpAddress in addressList)
                {
                    ThreadBuffer.Add(new Thread(() =>
                    {
                        SendArpRequest(device, sourceAddress, targetIpAddress);
                    }));
                }
            }
            catch (PcapException ex)
            {
                DebugOutput.Print(view, "PcapException @ GetClientList.InitiateArpRequestQueue() probably due to capturedevice being closed by refreshing or by exiting application [" + ex.Message + "]");
            }
            catch (OutOfMemoryException ex)
            {
                DebugOutput.Print(view, $"PcapException @ GetClientList.InitiateArpRequestQueue() out of memory. \nTotal number of threads {ThreadBuffer.Count}\nTotal number of alive threads {ThreadBuffer.AliveCount}\n[" + ex.Message + "]");
            }
            catch (Exception ex)
            {
                DebugOutput.Print(view, "Exception at GetClientList.InitiateArpRequestQueue() inside new Thread(()=>{}) while sending packets [" + ex.Message + "]");
            }
        }

        private static void SendArpRequest(WinPcapDevice device, IPAddress sourceIpAddress, IPAddress targetIpAddress)
        {
            var arprequestpacket = new ARPPacket(ARPOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), targetIpAddress, device.MacAddress, sourceIpAddress);
            var ethernetpacket = new EthernetPacket(device.MacAddress, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), EthernetPacketType.Arp);
            ethernetpacket.PayloadPacket = arprequestpacket;
            device.SendPacket(ethernetpacket);
            Debug.WriteLine("ARP request is sent to: {0}", targetIpAddress);
        }

        private static void ParseArpResponse(IView view, IPV4Subnet subnet, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            var arppacket = (ARPPacket)packet.Extract(typeof(ARPPacket));
            if (!ArpTable.Instance.ContainsKey(arppacket.SenderProtocolAddress) && arppacket.SenderProtocolAddress.ToString() != "0.0.0.0" && subnet.Contains(arppacket.SenderProtocolAddress))
            {
                DebugOutput.Print(view, "Added " + arppacket.SenderProtocolAddress.ToString() + " @ " + GetMACString(arppacket.SenderHardwareAddress) + " from background scan!");
                ArpTable.Instance.Add(arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                _ = view.ClientListView.Invoke(new Action(() =>
                {
                    _ = view.ClientListView.Items.Add(new ListViewItem(new string[] { arppacket.SenderProtocolAddress.ToString(), GetMACString(arppacket.SenderHardwareAddress), "On", ApplicationSettings.GetSavedClientNameFromMAC(GetMACString(arppacket.SenderHardwareAddress)) }));
                }));
                _ = view.MainForm.Invoke(new Action(() => view.ToolStripStatusScan.Text = ArpTable.Instance.Count + " device(s) found"));
            }
        }

        /// <summary>
        /// Converts a PhysicalAddress to colon delimited string like FF:FF:FF:FF:FF:FF
        /// </summary>
        /// <param name="physicaladdress"></param>
        /// <returns></returns>
        private static string GetMACString(PhysicalAddress physicaladdress)
        {
            try
            {
                var retval = "";
                for (var i = 0; i <= 5; i++)
                {
                    retval += physicaladdress.GetAddressBytes()[i].ToString("X2") + ":";
                }

                return retval.Substring(0, retval.Length - 1);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
