﻿using System;
using System.Collections.Generic;
using System.Net;

namespace CSArp
{
    public class IPV4Subnet
    {
        public IPAddress First { get; private set; }
        public IPAddress Last { get; private set; }
        public IPAddress NetworkAddress { get; private set; }
        public IPAddress BroadcastAddress { get; private set; }

        private readonly uint firstAddressAsUint;
        private readonly uint lastAddressAsUint;

        public IPV4Subnet(IPAddress currentAddress, IPAddress subnetMask)
        {
            // Convert the IP address to bytes.
            byte[] ipBytes = currentAddress.GetAddressBytes();

            // Get bytes from subnet mask
            var maskBytes = subnetMask.GetAddressBytes();

            byte[] firstIpAddressAsByte = new byte[ipBytes.Length];
            byte[] lastIpAddressAsByte = new byte[ipBytes.Length];

            // Calculate the bytes of the start and end IP addresses.
            for (int i = 0; i < ipBytes.Length; i++)
            {
                firstIpAddressAsByte[i] = (byte)(ipBytes[i] & maskBytes[i]);
                lastIpAddressAsByte[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }

            // Set network address and broadcast address
            NetworkAddress = new IPAddress(firstIpAddressAsByte);
            BroadcastAddress = new IPAddress(lastIpAddressAsByte);

            // Exclude network address and broadcast address
            firstIpAddressAsByte[3] += 1;
            lastIpAddressAsByte[3] -= 1;

            // Convert the bytes to IP addresses.
            First = new IPAddress(firstIpAddressAsByte);
            Last = new IPAddress(lastIpAddressAsByte);

            // Convert addresses to uint for future use
            firstAddressAsUint = ConvertToUint(First);
            lastAddressAsUint = ConvertToUint(Last);
        }

        public int Count
        {
            get
            {
                return (int)(lastAddressAsUint - firstAddressAsUint + 1);
            }
        }

        public bool Contains(IPAddress ipaddress)
        {
            var address = ConvertToUint(ipaddress);
            return firstAddressAsUint <= address && address <= lastAddressAsUint;
        }

        public List<IPAddress> ToList()
        {
            var list = new List<IPAddress>();

            while (GetEnumerator().MoveNext())
            {
                list.Add(GetEnumerator().Current);
            }

            return list;
        }
        private IEnumerator<IPAddress> GetEnumerator()
        {
            for (uint adr = firstAddressAsUint; adr <= lastAddressAsUint; adr++)
            {
                yield return ConvertToIPv4Address(adr);
            }
        }

        private uint ConvertToUint(IPAddress ipAddress)
        {
            var addressBytes = ipAddress.GetAddressBytes();
            Array.Reverse(addressBytes);
            return BitConverter.ToUInt32(addressBytes, 0);
        }

        private IPAddress ConvertToIPv4Address(uint value)
        {
            var addressBytes = BitConverter.GetBytes(value);
            Array.Reverse(addressBytes);
            return new IPAddress(addressBytes);
        }

        private static int SubnetToCIDR(IPAddress subnetMask)
        {
            byte[] ipParts = subnetMask.GetAddressBytes();
            uint subnet = 16777216 * Convert.ToUInt32(ipParts[0]) + 65536 * Convert.ToUInt32(ipParts[1]) + 256 * Convert.ToUInt32(ipParts[2]) + Convert.ToUInt32(ipParts[3]);
            uint mask = 0x80000000;
            uint subnetConsecutiveOnes = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!(mask & subnet).Equals(mask)) break;

                subnetConsecutiveOnes++;
                mask >>= 1;
            }
            return (int)subnetConsecutiveOnes;
        }
    }
}