using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

public class ColliderProxy
{

    public CTransform transform;
    public LRect bound = new LRect(0,0,60,60);
    public bool isStatic = false;
    public LRect GetRect()
    {
        return new LRect(bound.x +transform.position.x, bound.y + transform.position.y, bound.width, bound.height);
    }

}
