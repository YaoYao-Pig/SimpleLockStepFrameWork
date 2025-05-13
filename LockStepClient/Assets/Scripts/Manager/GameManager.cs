using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : ManagerBase
{
    private static GameManager _instance;
    public float sendIntervalSeconds = 0.1f;
    public int playerGuid;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }
    public PlayerInput curPlayerInput;
    public SimpleClient simpleClient;
    public InputMono inputMono;

    public List<GameObject> playerList;
    private GameObject _playerObject;
    private List<ManagerBase> managerList = new List<ManagerBase>();
    public List<GameObject> enemyList;
    public Dictionary<int,PlayerInput> playerInputDict = new Dictionary<int, PlayerInput>();
    private void Awake()
    {
        _instance = this;
        playerGuid = System.Guid.NewGuid().GetHashCode();
        simpleClient = new SimpleClient();
        _playerObject = Instantiate(playerList[0],Vector3.zero,Quaternion.identity);
        managerList.Add(GetComponent<PlayerManager>());
        managerList.Add(GetComponent<EnemyManager>());
    }
    
    private void Start()
    {
        simpleClient.Start();
    }

    private void Update()
    {
        inputMono.DoUpdate();
        foreach (var manager in managerList)
        {
            manager.DoUpdate(sendIntervalSeconds);
        }
    }
}
