using System.Net;
using System.Net.NetworkInformation;

namespace CSArp
{
    public class ClientDevice
    {
        public IPAddress IPAddress { get; set; }
        public PhysicalAddress PhysicalAddress { get; set; }
    }
}
