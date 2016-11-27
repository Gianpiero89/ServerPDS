using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace ServerTCP
{
    class Server
    {
        private TcpListener server;


        public Server(IPAddress ip, int port)
        {
            this.server = new TcpListener(ip, port);
        }

        public void StartServer()
        {
            this.server.Start();
        }

        public void stopServer()
        {
            this.server.Stop();
        }

        public TcpClient waitForConnection()
        {
            try
            {
                lock (this)
                {
                     return this.server.AcceptTcpClient();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Interrupt connnection\n");
                Console.WriteLine(e.Message);
                throw new Exception("ServerDown");
            }

        }
    }
}
