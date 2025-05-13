using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    class ServerLauncher
    {
        public static IPEndPoint serverIpPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10082);
        private Server server = new Server();

        public ServerLauncher() {
            
        }
        public void Awake()
        {
            this.server = new Server(serverIpPoint);
        }

        public void Start() {
            // 轮询获取
            server.Start();
        }

        public void DoUpdate() {
            server.DoUpdate();
        }
    }
}
