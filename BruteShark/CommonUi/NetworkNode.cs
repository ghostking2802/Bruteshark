﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace CommonUi
{
    public class NetworkNode
    {
        [JsonProperty("IP Address")]
        public string IpAddress { get; set; }

        [JsonProperty("Open Ports")]
        public HashSet<int> OpenPorts { get; set; }

        [JsonProperty("DNS Mappings")]
        public HashSet<string> DnsMappings { get; set; }

        [JsonProperty("TCP Sessions Count")]
        public int TcpSessionsCount { get; set; }

        [JsonProperty("UDP Streams Count")]
        public int UdpStreamsCount { get; set; }

        [JsonProperty("Sent Data")]
        public int SentData { get; set; }

        [JsonProperty("Received Data")]
        public int ReceiveData { get; set; }

        [JsonProperty("Domains and Users")]
        public HashSet<string> Domains { get; set; }

        [JsonProperty("Domain Users")]
        public HashSet<string> DomainUsers { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is NetworkNode))
            {
                return false;
            }

            var networkNode = obj as NetworkNode;

            return base.Equals(networkNode) &&
                   this.IpAddress == networkNode.IpAddress;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^
                   this.IpAddress.GetHashCode();
        }
    }
}
