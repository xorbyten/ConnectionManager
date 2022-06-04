using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionManager.Handlers
{
    internal class Users
    {
        //public Socket UserSocket { get; private set; }
        public string Hostname { get; set; }

        public string IP { get; private set; }
        public Users(/*Socket socket*/ string hostname, string ip)
        {
            /*this.UserSocket = socket;
            IPEndPoint ipEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            IP = ipEndPoint.Address.ToString();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(IP);
            Hostname = ipHostEntry.HostName;*/
            this.Hostname = hostname;
            this.IP = ip;
        }
    }
}
