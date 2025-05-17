using System;
using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

public class PlayerEntity : MonoBehaviour,IController
{
    public ColliderProxy colliderProxy;
    public LVector2 prePos;
    public PlayerView playerView;
    public int id;
    
    public void DoUpdate(float deltaTime)
    {
        prePos = colliderProxy.transform.position;
        if (GameManager.Instance.playerInputDict.TryGetValue(id,out var input))
        {
            if (colliderProxy.isStatic)
            {
                colliderProxy.isStatic = false;
                playerView.SetCollider();
                //return;
            }
            else
            {
                playerView.SetUnCollider();
            }
            colliderProxy.transform.position +=  new LVector2(input.inputUV.x/1000,input.inputUV.y/1000);
        }
    }
}
