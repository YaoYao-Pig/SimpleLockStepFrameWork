using System;
using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

public class PlayerEntity : MonoBehaviour,IController
{
    public ColliderProxy colliderProxy;

    public int id;
    
    public void DoUpdate(float deltaTime)
    {
        if (GameManager.Instance.playerInputDict.TryGetValue(id,out var input))
        {
            colliderProxy.transform.position +=  new LVector2(input.inputUV.x,input.inputUV.y);
        }
    }
}
