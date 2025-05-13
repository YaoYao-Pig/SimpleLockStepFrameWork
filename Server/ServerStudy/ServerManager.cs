using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy {
    internal class ServerManager {
        private static ServerManager instance;
        public List<int> playerIdList = new List<int>();
        public Dictionary<int, PlayerInput_DTO> playerInputDic = new Dictionary<int, PlayerInput_DTO>();
        public static ServerManager Instance {
            get {
                if (instance == null) { instance = new ServerManager(); } return instance; }
            }

        public ServerManager() { }

        public void AddPlayerId(int id) {
            playerIdList.Add(id);
        }

        public bool IsPlayerEnter(int id) {
            return playerIdList.Contains(id);
        }
    }
}
