using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : ManagerBase
{


    public List<PlayerEntity> playerMoveControllerList;
    public Dictionary<int, PlayerEntity> PlayerId2EntitiesDic;
    public List<PlayerView> playerViewList;

    // Update is called once per frame
    public override void DoUpdate(float deltaTime)
    {
        if (playerMoveControllerList != null)
        {
            foreach (var player in playerMoveControllerList)
            {
                player.DoUpdate(deltaTime);
            }
        }
    }
}
