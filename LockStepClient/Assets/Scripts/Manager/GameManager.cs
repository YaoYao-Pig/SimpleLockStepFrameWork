using System;
using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

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
    public PlayerManager playerManager;
    private bool hasInit = false;

    public LFloat detaTime = LMath.ToLFloat(0.02f);
    private void Awake()
    {
        _instance = this;
        hasInit = false;
        playerGuid = System.Guid.NewGuid().GetHashCode();
        simpleClient = new SimpleClient();
        _playerObjectList = null;
        
        playerManager = GetComponent<PlayerManager>();
        managerList.Add(playerManager);
        managerList.Add(GetComponent<EnemyManager>());
    }

    public void Initialize()
    {
        if (playerInputDict.Count == 2 && hasInit == false)
        {
            hasInit = true;
            Debug.Log("Create");
            _playerObjectList = new List<GameObject>();
            playerManager.playerMoveControllerList = new List<PlayerEntity>();
            playerManager.PlayerId2EntitiesDic = new Dictionary<int, PlayerEntity>();
            for (int i = 0; i < playerInputDict.Count; ++i)
            {
                var t = Instantiate(playerPrefabList[0], Vector3.zero, Quaternion.identity);

                var moveController = t.GetComponent<PlayerEntity>();
                
                
                _playerObjectList.Add(t);
                playerManager.playerMoveControllerList.Add(moveController);

                Debug.Log("PlayerCreate");
            }

            int index = 0;
            foreach (var p in playerInputDict)
            {
                var t = _playerObjectList[index];
                var moveController = t.GetComponent<PlayerEntity>();
                moveController.id = p.Key;
                moveController.colliderProxy = new ColliderProxy();
                moveController.colliderProxy.transform = new CTransform(LVector2.zero) ;
                var playerView = t.GetComponent<PlayerView>();
                playerView.id = p.Key;
                playerManager.PlayerId2EntitiesDic.Add(moveController.id, moveController);
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
