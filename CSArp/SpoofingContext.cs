using System.Collections.Generic;
using System.Net;
using SharpPcap;

namespace CSArp
{
    public class SpoofingContext
    {
        public CaptureDeviceList NetworkAdapters { get; set; }
        public ICaptureDevice SelectedNetworkAdapter { get; set; }
        public string SelectedNetworkAdapterFriendlyName { get; set; }
        public List<ClientDevice> ClientDevices { get; set; }
        public ClientDevice Gateway { get; set; }
    }
}
