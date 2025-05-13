using UnityEngine;
using System;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

public class SimpleClient
{
    public string serverAddress = "127.0.0.1";
    public int serverPort = 10082;
    public string testMessage = "Hello Server from Unity!";
    public SimplePollingClient simplePollingClient = new SimplePollingClient();

    
    // 在游戏开始时自动尝试连接和发送消息
    public void Start()
    {
        simplePollingClient.Start();
         //ConnectAndSendTestMessage();
    }

    // 如果你想通过按钮或其他方式触发，可以取消注释下面的方法
    // public void TriggerSend()
    // {
    //     _ = ConnectAndSendTestMessage(); // 使用丢弃来调用异步方法，不等待其完成
    // }

    public void DoUpdate()
    {
        simplePollingClient.OnDestroy();
    }
}