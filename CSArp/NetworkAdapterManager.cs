using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpPcap;
using SharpPcap.WinPcap;
using SharpPcap.AirPcap;

namespace CSArp
{
    public static class NetworkAdapterManager
    {
        public static CaptureDeviceList NetworkAdapters
        {
            get
            {
                if (_networkAdapters == null)
                {
                    _networkAdapters = CaptureDeviceList.Instance;
                }

                return _networkAdapters;
            }
        }

        private static CaptureDeviceList _networkAdapters;

        public static IReadOnlyList<WinPcapDevice> WinPcapDevices
        {
            get
            {
                return NetworkAdapters
                    .Where(adapter => adapter is WinPcapDevice)
                    .Select(adapter => adapter as WinPcapDevice)
                    .ToList()
                    .AsReadOnly();
            }
        }

        [Obsolete("Since AirPcap is obsolete, it will not be used at first.")]
        public static IReadOnlyList<AirPcapDevice> AirPcapDevices
        {
            get
            {
                return NetworkAdapters
                    .Where(adapter => adapter is AirPcapDevice)
                    .Select(adapter => adapter as AirPcapDevice)
                    .ToList()
                    .AsReadOnly();
            }
        }
    }
}
