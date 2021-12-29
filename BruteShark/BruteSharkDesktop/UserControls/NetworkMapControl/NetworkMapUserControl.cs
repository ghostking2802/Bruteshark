﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapAnalyzer;
using System.Net;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.IO;

namespace BruteSharkDesktop
{
    public partial class NetworkMapUserControl : UserControl
    {
        private CommonUi.NetworkContext _networkContext;
        private Dictionary<string, HashSet<string>> _dnsMappings;
        private HashSet<NetworkMapEdge> _edges;
        Microsoft.Msagl.GraphViewerGdi.GViewer _viewer;
        Microsoft.Msagl.Drawing.Graph _graph;

        public int NodesCount => _graph.Nodes.Count();

        public NetworkMapUserControl(CommonUi.NetworkContext networkContext)
        {
            InitializeComponent();
            _networkContext = networkContext;

            // Add MSAGL Graph control and register to click events.
            _dnsMappings = new Dictionary<string, HashSet<string>>();
            _edges = new HashSet<NetworkMapEdge>();
            _viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            _viewer.MouseClick += OnGraphMouseClick;
            _graph = new Microsoft.Msagl.Drawing.Graph("graph");
            _viewer.Graph = _graph;
            _viewer.Dock = DockStyle.Fill;
            this.mainSplitContainer.Panel1.Controls.Add(_viewer);

            // There is a bit odd behavior of the controls in the second panel when the msagl is at 
            // the first panel (not drawing the tree view). This force the second panel to refresh.
            this.mainSplitContainer.Panel2.Refresh();
            this.nodeTreeView.Click += (object sender, EventArgs e) => this.mainSplitContainer.Panel2.Refresh();
        }

        private void OnGraphMouseClick(object sender, MouseEventArgs e)
        {
            if (_viewer.SelectedObject is Microsoft.Msagl.Drawing.Node)
            {
                var ipAddress = new StringReader((_viewer.SelectedObject as Microsoft.Msagl.Drawing.Node).LabelText).ReadLine();

                if (IsIpAddress(ipAddress))
                {
                    JsonTreeViewLoader.LoadJsonToTreeView(
                        treeView: this.nodeTreeView, 
                        json: _networkContext.GetNodeDataJson(ipAddress),
                        rootNodeText: "Host Details");
                }
            }

            this.mainSplitContainer.Panel2.Refresh();
        }

        public void AddEdge(string source, string destination, string edgeText = "")
        {
            this.SuspendLayout();

            // We create an edge object and save it in a HashTable to avoid inserting
            // double edges.
            var newEdge = new NetworkMapEdge()
            {
                Source = source,
                Destination = destination,
                Text = edgeText
            };

            if (!_edges.Contains(newEdge))
            {
                _graph.AddEdge(source, edgeText, destination);
                _edges.Add(newEdge);
            }

            var sourceNode = _graph.FindNode(source);
            var destinationNode = _graph.FindNode(destination);
            sourceNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightBlue;
            sourceNode.LabelText = GetNodeText(source);
            destinationNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightBlue;
            destinationNode.LabelText = GetNodeText(destination);

            _viewer.Graph = _graph;
            this.ResumeLayout();
        }

        private string GetNodeText(string ipAddress)
        {
            var res = ipAddress;
            var addressDnsRecords = _networkContext.DnsMappings.Where(d => d.Destination == ipAddress)
                                                               .Select(d => d.Query)
                                                               .ToList();

            if (addressDnsRecords.Count > 0)
            {
                res += Environment.NewLine + "DNS: " + addressDnsRecords.First();

                if (addressDnsRecords.Count > 1)
                {
                    res += $" ({addressDnsRecords.Count} more)";
                }
            }

            return res;
        }

        public void HandleHash(PcapAnalyzer.NetworkHash hash)
        {
            // Usually the hashes username is named "User" or "Username".
            var userName = GetPropertyValue(hash, new string[] { "User", "Username"});

            if (userName.Length > 0)
            {
                var edgeText = $"{hash.HashType} Hash";

                // If it is a domain related hash (e.g Kerberos, NTLM)
                if (hash is PcapAnalyzer.IDomainCredential)
                {
                    var domain = (hash as IDomainCredential).GetDoamin();
                    userName = domain.Length > 0 ? @$"{domain}\{userName}" : userName;
                }

                AddEdge(userName, hash.Destination, edgeText);
                _graph.FindNode(userName).Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightGreen;
            }
        }

        public void HandlePassword(PcapAnalyzer.NetworkPassword password)
        {
            var edgeText = $"{password.Protocol} Password";
            AddEdge(password.Username, password.Destination, edgeText);
            _graph.FindNode(password.Username).Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightGreen;
        }
        
        public void HandleDnsNameMapping(DnsNameMapping dnsNameMapping)
        {
            // Normally DNS mappings arriving before real data, but we can't count on it therfore we are saving
            // the mappings at the network context for future hosts, handled at the AddEdge() function.
            if (_networkContext.HandleDnsNameMapping(dnsNameMapping))
            {
                UpdateNodeLabel(dnsNameMapping.Destination);
            }
        }

        private void UpdateNodeLabel(string ipAddress)
        {
            var node = _graph.FindNode(ipAddress);

            if (node != null)
            {
                node.LabelText = GetNodeText(ipAddress);
            }
        }

        private bool IsIpAddress(string ip)
        {
            return IPAddress.TryParse(ip, out IPAddress ipAddress);
        }

        private static object GetPropertyValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName)?.GetValue(src, null);
        }

        private string GetPropertyValue(object src, IEnumerable<string> propertiesNames)
        {
            var res = string.Empty;

            foreach (var name in propertiesNames)
            {
                var value = GetPropertyValue(src, name);
                var stringValue = value == null ? string.Empty : value.ToString();

                if (stringValue.Length > 0)
                {
                    res = stringValue;
                    break;
                }
            }

            return res;
        }

    }
}
