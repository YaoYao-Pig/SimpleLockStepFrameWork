using System.Collections;
using System.Collections.Generic;
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
        tmpInput.inputUV = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        tmpInput.isJump = Input.GetKeyDown(KeyCode.Space);
        GameManager.Instance.curPlayerInput = tmpInput;
    }
}
