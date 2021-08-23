using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace CSArp
{
    public class ArpTable
    {
        public static ArpTable Instance
        {
            get
            {
                return _instance ?? new ArpTable();
            }
        }

        public int Count { get { return _dictionary.Count; } }

        private static readonly ArpTable _instance;
        private readonly ConcurrentDictionary<IPAddress, PhysicalAddress> _dictionary;

        private ArpTable()
        {
            _dictionary = new ConcurrentDictionary<IPAddress, PhysicalAddress>();
        }

        public void Add(IPAddress ipAddress, PhysicalAddress physicalAddress)
        {
            _ = _dictionary.TryAdd(ipAddress, physicalAddress);
        }

        public bool ContainsKey(IPAddress ipAddress)
        {
            return _dictionary.ContainsKey(ipAddress);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }
    }
}
