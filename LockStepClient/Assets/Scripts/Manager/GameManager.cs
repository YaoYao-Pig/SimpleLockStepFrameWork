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

    public List<GameObject> playerPrefabList;
    private List<GameObject> _playerObjectList;
    private List<ManagerBase> managerList = new List<ManagerBase>();
    public List<GameObject> enemyList;
    public Dictionary<int,PlayerInput> playerInputDict = new Dictionary<int, PlayerInput>();
    private PlayerManager _playerManager;
    private bool hasInit = false;
    private void Awake()
    {
        _instance = this;
        hasInit = false;
        playerGuid = System.Guid.NewGuid().GetHashCode();
        simpleClient = new SimpleClient();
        _playerObjectList = null;
        
        _playerManager = GetComponent<PlayerManager>();
        managerList.Add(_playerManager);
        managerList.Add(GetComponent<EnemyManager>());
    }

    public void Initialize()
    {
        if (playerInputDict.Count == 2 && hasInit == false)
        {
            hasInit = true;
            Debug.Log("Create");
            _playerObjectList = new List<GameObject>();
            _playerManager.playerMoveControllerList = new List<MoveController>();
            for (int i = 0; i < playerInputDict.Count; ++i)
            {
                var t = Instantiate(playerPrefabList[0], Vector3.zero, Quaternion.identity);

                var moveController = t.GetComponent<MoveController>();
                
                
                _playerObjectList.Add(t);
                _playerManager.playerMoveControllerList.Add(moveController);
                Debug.Log("PlayerCreate");
            }

            int index = 0;
            foreach (var p in playerInputDict)
            {
                var t = _playerObjectList[index];
                var moveController = t.GetComponent<MoveController>();
                moveController.id = p.Key;
                index++;
            }
            
        }
    }
    
    private void Start()
    {
        simpleClient.Start();
    }

    private void Update()
    {
        Initialize();
        inputMono.DoUpdate();
        foreach (var manager in managerList)
        {
            manager.DoUpdate(sendIntervalSeconds);
        }
    }
}
