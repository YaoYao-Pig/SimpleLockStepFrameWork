using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput
{
    public int id;
    public Vector2 inputUV;
    public bool isJump;
    

    public PlayerInput()
    {
        id = GameManager.Instance.playerGuid;
        inputUV = Vector2.zero;
        isJump = false;
    }
}
