using System;
using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

[Serializable]
public class PlayerInput
{
    public int id;
    public LVector2 inputUV;
    public bool isJump;
    

    public PlayerInput()
    {
        id = GameManager.Instance.playerGuid;
        inputUV = LVector2.zero;
        isJump = false;
    }
}
