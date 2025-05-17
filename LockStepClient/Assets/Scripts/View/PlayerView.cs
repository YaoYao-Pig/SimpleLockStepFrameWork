using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : IView
{
    public int id;
    private PlayerEntity playerEntity;

    public Renderer renderer;
    public Material colliderMaterial;

    public Material defualtMaterial;
    // Update is called once per frame
    public void SetCollider()
    {
        renderer.material = colliderMaterial;
    }

    public void SetUnCollider()
    {
        renderer.material = defualtMaterial;
    }
    void Update()
    {
        if (playerEntity == null)
        {
            playerEntity = GameManager.Instance.playerManager.PlayerId2EntitiesDic[id];
            
        }

        var t = GameManager.Instance.playerInputDict[id];
        transform.position = new Vector3(playerEntity.colliderProxy.transform.position.x, 0 , playerEntity.colliderProxy.transform.position.y)*Time.deltaTime;

    }
}
