using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

public class InputMono : MonoBase
{
    public override void _Awake()
    {
        
    }

    public override void _Start()
    {
        
    }

    public void DoUpdate()
    {
        this._Update();
    }
    public override void _Update()
    {
        var tmpInput = new PlayerInput();
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        //Debug.Log(h.ToLFloat()+"  "+v.ToLFloat());
        tmpInput.inputUV = new LVector2(h.ToLFloat(), v.ToLFloat());
        tmpInput.isJump = Input.GetKeyDown(KeyCode.Space);
        GameManager.Instance.curPlayerInput = tmpInput;
    }
}
