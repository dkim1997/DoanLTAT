using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace Tuan_77
{
    class Program
    {
        static Socket sock;
        static void revc()
        {
            EndPoint tamp = null;
            List<EndPoint> listip = new List<EndPoint>();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9050);
            EndPoint ep = (EndPoint)iep;
            sock.Bind(iep);
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                    new MulticastOption(IPAddress.Parse("224.100.0.1")));
            Console.WriteLine("Ready to receive…");
            while (true)
            {
                        
                byte[] data = new byte[1024];
                int recv = sock.ReceiveFrom(data, ref ep);
                tamp = ep;
                string stringData = Encoding.ASCII.GetString(data, 0, recv);
                Console.WriteLine("received: {0} from: {1}", stringData,
                                                ep.ToString());
           }
        }
        static bool check(List<EndPoint> list, EndPoint ep)
        {
            foreach (EndPoint i in list)
            {
                if (i.ToString() == ep.ToString()) return true;
            }
            return false;
        }
        static void send()
        {
                Thread.Sleep(100);
                Socket server = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint iep = new IPEndPoint(IPAddress.Parse("224.100.0.1"), 9050);
                byte[] data = Encoding.ASCII.GetBytes("This is a test message");
                server.SendTo(data, iep);
                server.Close();
        }
        static void Main(string[] args)
        {
            Thread t1 = new Thread(revc);
            t1.Start();
            Thread t2 = new Thread(send);
            t2.Start();
            while (true)
            {
                Thread.Sleep(5000);
                t2.Abort();
                t1.Abort();
                sock.Close();
                t1 = new Thread(revc);
                t2 = new Thread(send);
                t1.Start();
                t2.Start();
            }
        }
    }
}
