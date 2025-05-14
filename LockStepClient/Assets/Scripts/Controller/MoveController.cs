using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour,IController
{
    private Rigidbody _rigidbody;

    public int id;

    public void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void DoUpdate(float deltaTime)
    {
        if (GameManager.Instance.playerInputDict.TryGetValue(id,out var input))
        {
            transform.position = new Vector3(transform.position.x +input.inputUV.x, transform.position.y+input.inputUV.y, transform.position.z);
            
        }
    }
}
