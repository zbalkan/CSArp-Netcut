using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SharpPcap;

namespace CSArp
{
    public interface IController
    {
        CaptureDeviceList GetAllNetworkAdapters();
        ICaptureDevice GetNetworkAdapter(string name);
        IEnumerable<ClientDevice> GetAllClients();
        ClientDevice GetClient(IPAddress address);
        void Disconnect(ClientDevice clientDevice);
        void ReconnectAll();
        void ExportLog(string path);
    }
}
