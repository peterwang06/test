using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using MergeSharp;
using MergeSharp.TCPConnectionManager;
using Microsoft.Extensions.Logging;

namespace chatroom;

public class CRDTManager
{
    public ConnectionManager cm;
    public ReplicationManager rm;

    public CRDTManager(string clientIP, string clientPort, List<string> nodeslist)
    {
        this.cm = new ConnectionManager(clientIP, clientPort, nodeslist);
        this.rm = new ReplicationManager(this.cm);
    }
}