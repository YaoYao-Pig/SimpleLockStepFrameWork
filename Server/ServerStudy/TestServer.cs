using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy{
    class TestServer {
        private static ServerLauncher serverLauncher;
        public static void Main(string[] args) {
            DoAwake();
            DoStart();
            while (true) {
                serverLauncher.DoUpdate();
            }
            
        }


        static void DoAwake() {
            serverLauncher = new ServerLauncher();
            serverLauncher.Awake();
        }

        static void DoStart() {
            serverLauncher.Start();
        }
    }
}
