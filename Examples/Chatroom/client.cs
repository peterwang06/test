using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MergeSharp;
using MergeSharp.TCPConnectionManager;
using Microsoft.Extensions.Logging;

namespace chatroom;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 5)
        {
            Console.Error.WriteLine("Error parsing IP address / ports");
            return 1;
        }

        string username = args[0];
        string clientIP = args[1];
        string clientPort = args[2];
        string targetIP = args[3];
        string targetPort = args[4];

        int clientPortInt;
        int targetPortInt;
        Int32.TryParse(clientPort, out clientPortInt);
        Int32.TryParse(targetPort, out targetPortInt);
        int minPort = Math.Min(clientPortInt, targetPortInt);

        Console.WriteLine("user:" + username + " on " + clientIP + ":" +  clientPort + " connecting to " + targetIP + ":" + targetPort);
        List<string> nodeslist = new List<string>(new string[] { clientIP + ':' + clientPort, targetIP + ':' + targetPort });

        CRDTManager manager = new CRDTManager(clientIP, clientPort, nodeslist);
        manager.rm.RegisterType<GVector<string>>();

        GVector<string> chatroom = null;

        if(clientPortInt == minPort)
        {
            Guid uid = manager.rm.CreateCRDTInstance<GVector<string>>(out chatroom, Guid.Empty);
        }

        Console.WriteLine("Waiting for chatroom");
        while(chatroom == null)
        {
            manager.rm.TryGetCRDT(Guid.Empty, out chatroom);
            Thread.Sleep(50);
        }

        Console.Clear();
        Console.WriteLine("You are connected! Say hello!");

		List<string> output = new List<string>();
		ManualResetEvent waitHandle = new ManualResetEvent(false);
		
        UpdateReporter chatUpdate = new UpdateReporter(Guid.Empty, waitHandle);
        manager.rm.Subscribe(chatUpdate);

		Thread outputThread = new Thread(delegate(){
		    while(true){
    		    waitHandle.WaitOne();
                List<string> output = chatroom.LookupAll();
    			Console.Clear();
    			for(int i = 0; i < output.Count; i++){
    				Console.WriteLine(output[i]);
    			}
    			waitHandle.Reset();
		    }
		});
		outputThread.Start();
		string msg;
		while(true){
			msg =  Console.ReadLine();
            if(msg == "/exit")
            {
                manager.cm.Stop();
                return 0;
            }
			chatroom.Add("[" + DateTime.Now + "] " + username + ": " + msg);
			waitHandle.Set();
		}
    }
}