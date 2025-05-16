using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : IView
{
    public int id;
    private PlayerEntity playerEntity;
    // Update is called once per frame
    void Update()
    {
        if (playerEntity == null)
        {
            playerEntity = GameManager.Instance.playerManager.PlayerId2EntitiesDic[id];
            
        }

        var t = GameManager.Instance.playerInputDict[id];
        Debug.Log("pos: " + playerEntity.colliderProxy.transform.position);
        transform.position = new Vector3(playerEntity.colliderProxy.transform.position.x/1000, 0 , playerEntity.colliderProxy.transform.position.y/1000)*Time.deltaTime;

    }
}
