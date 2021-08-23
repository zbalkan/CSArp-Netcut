using System;
using System.Collections.Generic;
using System.Linq;
using SharpPcap;
using SharpPcap.WinPcap;
using SharpPcap.AirPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.IO;

namespace CSArp
{
    public class ArpController : IController
    {
        private SpoofingContext spoofingContext;

        public ArpController()
        {
            spoofingContext = new SpoofingContext();
        }

        public CaptureDeviceList GetAllNetworkAdapters()
        {
            if (spoofingContext.NetworkAdapters == null)
            {
                spoofingContext.NetworkAdapters = CaptureDeviceList.Instance;
            }

            return spoofingContext.NetworkAdapters;
        }
        public ICaptureDevice GetNetworkAdapter(string name)
        {
            var adapters = GetAllNetworkAdapters().ToList();
            ICaptureDevice networkAdapter = null;
            foreach (var adapter in adapters)
            {
                if (adapter is WinPcapDevice winPcapDevice)
                {
                    if (winPcapDevice.Interface.FriendlyName == name)
                    {
                        networkAdapter = winPcapDevice;
                    }
                }
                else if (adapter is AirPcapDevice airPcapDevice)
                {
                    if (airPcapDevice.Interface.FriendlyName == name)
                    {
                        networkAdapter = airPcapDevice;
                    }
                }
            }
            return networkAdapter;
        }
        public IEnumerable<ClientDevice> GetAllClients()
        {
            if (spoofingContext.ClientDevices != null && spoofingContext.ClientDevices.Any())
            {
                return spoofingContext.ClientDevices;
            }
            SpoofingController.Reconnect();
            spoofingContext.ClientDevices = new List<ClientDevice>();
            return ArpScanner.GetAllClients(spoofingContext.Gateway.IPAddress, spoofingContext.ClientDevices);
        }
        public ClientDevice GetClient(IPAddress address)
        {
            return GetAllClients().FirstOrDefault(client => client.IPAddress.Equals(address));
        }
        public void Disconnect(ClientDevice clientDevice)
        {
            throw new NotImplementedException();
        }
        public void ReconnectAll()
        {
            throw new NotImplementedException();
        }
        public void ExportLog(string path)
        {
            throw new NotImplementedException();
        }

    }
}
