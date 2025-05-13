using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour,IController
{
    private Rigidbody _rigidbody;
    

    public void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void DoUpdate(float deltaTime)
    {
        if (GameManager.Instance.playerInputDict.TryGetValue(GameManager.Instance.playerGuid,out var input))
        {
            _rigidbody.AddForce(new Vector3(input.inputUV.x, 0, input.inputUV.y) * 10);
        }
    }
}
