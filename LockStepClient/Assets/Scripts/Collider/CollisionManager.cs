using System.Collections.Generic;
using UnityEngine;

public class CollisionManager:ManagerBase {
    public Dictionary<int,ColliderProxy> allColliderProxyList = new Dictionary<int, ColliderProxy>();

    public List<ColliderProxy> preMoveColliderObject = new List<ColliderProxy>();
    public List<ColliderProxy> curMoveColliderObject = new List<ColliderProxy>();


    
    public void Register(int id,ColliderProxy colliderProxy)
    {
        allColliderProxyList.Add(id, colliderProxy);
        preMoveColliderObject.Add(colliderProxy);
    }
    
    public override void DoUpdate(float deltaTime)
    {
        //检测移动物体
        foreach(var pair in GameManager.Instance.playerInputDict)
        {
            int id = pair.Key;
            var input = pair.Value;
            if (input.isMove())
            {
                //Debug.Log("player: "+id+" is move");
                allColliderProxyList.TryGetValue(id,out var colliderProxy);
                if (colliderProxy == null)
                {
                    Debug.LogError("collider is null");
                }
                curMoveColliderObject.Add(colliderProxy);
            }
        }
        //TODO:重构四叉树

        foreach (var colliderProxy in curMoveColliderObject)
        {
            if (preMoveColliderObject.Contains(colliderProxy))
            {
                preMoveColliderObject.Remove(colliderProxy);
            }
        }

        List<ColliderProxy> tmp = new List<ColliderProxy>();
        //碰撞检测
        foreach (var colliderProxy in curMoveColliderObject)
        {
            //检测碰撞
            //Debug.Log("Check");
            foreach (var otherColliderProxy in allColliderProxyList.Values)
            {
                if (colliderProxy == otherColliderProxy)
                {
                    //Debug.Log("Check Continue");
                    continue;
                }
                //Debug.Log("Check in");
                if (CheckCollision(colliderProxy,otherColliderProxy))
                {
                    //TODO:处理碰撞
                    //Debug.Log("Collision success!");
                    tmp.Add(colliderProxy);
                    colliderProxy.isStatic = true;
                    otherColliderProxy.isStatic = true;
                }
            }
            //Debug.Log("Check End");
        }

        foreach (var colliderProxy in preMoveColliderObject)
        {
            foreach (var otherColliderProxy in allColliderProxyList.Values)
            {
                if (colliderProxy == otherColliderProxy)
                {
                    continue;
                }
                if (CheckCollision(colliderProxy,otherColliderProxy))
                {
                    //TODO:处理碰撞
                    tmp.Add(colliderProxy);
                    colliderProxy.isStatic = true;
                    otherColliderProxy.isStatic = true;
                }
            }
        }
        preMoveColliderObject = tmp;
        curMoveColliderObject.Clear();
    }
    
    public bool CheckCollision(ColliderProxy colliderProxy1,ColliderProxy colliderProxy2)
    {
        Debug.Log("colliderProxy1:"+colliderProxy1.GetRect().ToString());
        Debug.Log("colliderProxy2:"+colliderProxy2.GetRect().ToString());
        if (colliderProxy1.GetRect().Overlaps(colliderProxy2.GetRect()))
        {
            return true;
        }
        return false;
    }
}