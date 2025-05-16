using UnityEngine;
using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lockstep.Math; // Required for List in GameStateSnapshot_Client

// 确保 SimplePollingClient 继承自 MonoBehaviour
public class SimplePollingClient
{
    public string serverAddress = "127.0.0.1";
    public int serverPort = 10082;
    // public string periodicMessage = "Heartbeat from client"; // 此字段当前未被 SendLoopAsync 使用

    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected = false;
    private Task _receiveTask;
    private Task _sendTask;
    
    public void Start() 
    {
        // 启动连接和通信循环
        _ = ConnectAndRunCommunicationAsync();
    }

    public async Task ConnectAndRunCommunicationAsync()
    {
        if (_isConnected)
        {
            Debug.Log("客户端已经连接。");
            return;
        }

        try
        {
            _client = new TcpClient();
            _client.NoDelay = true; // 尝试禁用Nagle算法以减少延迟
            Debug.Log($"尝试连接到 {serverAddress}:{serverPort}...");

            var connectTask = _client.ConnectAsync(serverAddress, serverPort);
            // 对于 Task.WhenAny 的超时处理，可以这样写：
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
            {
                // connectTask 完成了
                if (_client.Connected) // 再次检查连接状态
                {
                    Debug.Log("成功连接到服务器!");
                    NetworkStream stream = _client.GetStream();
                    _reader = new StreamReader(stream);
                    _writer = new StreamWriter(stream) { AutoFlush = true };
                    _isConnected = true;

                    _cancellationTokenSource = new CancellationTokenSource();

                    await _writer.WriteLineAsync("Unity Client Connected. Starting loops.");
                    Debug.Log("已发送初始连接消息。");

                    _receiveTask = Task.Run(() => ReceiveMessagesLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    _sendTask = Task.Run(() => SendLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                    Debug.Log("接收和发送循环已启动。");
                }
                else
                {
                    Debug.LogError("连接任务完成但未连接到服务器。");
                    _client?.Close();
                    _isConnected = false;
                }
            }
            else
            {
                // Task.Delay 完成，表示连接超时
                Debug.LogError("连接服务器超时或服务器未运行。");
                connectTask?.Dispose(); // 如果connectTask仍在运行，尝试释放资源
                _client?.Close();
                _isConnected = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"连接错误: {e.ToString()}");
            _client?.Close();
            _isConnected = false;
        }
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        Debug.Log("发送循环已启动。");
        try
        {
            // int messageCount = 0; // 如果要发送周期性文本消息，则取消注释
            while (!token.IsCancellationRequested && _client != null && _client.Connected)
            {
                if (_writer == null)
                {
                    Debug.LogError("写入器为null，无法发送消息。");
                    break;
                }

                // 假设 GameManager.Instance 和相关属性存在
                if (GameManager.Instance != null && GameManager.Instance.curPlayerInput != null)
                {
                    string jsonInput = JsonUtility.ToJson(GameManager.Instance.curPlayerInput);
                    //Debug.Log("jsonInput"+": "+ jsonInput);
                    await _writer.WriteLineAsync(jsonInput);
                    // Debug.Log($"已发送到服务器: {jsonInput}"); // 此日志可能非常频繁，按需开启
                }
                else
                {
                    Debug.LogWarning("GameManager.Instance 或 curPlayerInput 为空，本次不发送输入。");
                }
                
                // 等待指定的时间间隔
                float interval = (GameManager.Instance != null && GameManager.Instance.sendIntervalSeconds > 0) ? GameManager.Instance.sendIntervalSeconds : 0.1f;
                await Task.Delay(TimeSpan.FromSeconds(interval), token);
            }
        }
        catch (OperationCanceledException) { Debug.Log("发送循环已取消。"); }
        catch (ObjectDisposedException) { Debug.Log("写入器或客户端已释放，发送循环停止。"); }
        catch (IOException ex) { Debug.LogError($"发送循环中发生IO异常 (服务器可能已关闭连接): {ex.Message}"); }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested) { Debug.LogError($"发送循环中发生错误: {e.ToString()}"); }
        }
        finally { Debug.Log("发送循环已结束。"); }
    }

    private async Task ReceiveMessagesLoop(CancellationToken token)
    {
        Debug.Log("接收循环已启动。");
        try
        {
            while (true)
            {
                if (_reader == null)
                {
                    Debug.LogError("读取器为null，无法接收消息。");
                    break;
                }
                string message = null;
                try
                {
                    var readTask = _reader.ReadLineAsync();
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, token));

                    if (completedTask == readTask)
                    {
                        message = await readTask; // 获取已完成读取任务的结果
                        Debug.Log($"接收到原始消息: {message}"); // 修正 Debug.Log 位置
                    }
                    else // Task.Delay 完成，意味着 token 被取消
                    {
                        Debug.Log("因服务器关闭或取消令牌，接收循环中断。");
                        break;
                    }
                }
                catch (IOException ex) { if (token.IsCancellationRequested) Debug.Log("因取消导致流关闭 (接收)。"); else Debug.LogWarning($"流关闭，可能由于服务器或网络问题 (接收): {ex.Message}"); break; }
                catch (ObjectDisposedException) { Debug.Log("读取器或客户端已释放，接收循环停止。"); break; }
                catch (OperationCanceledException) { Debug.Log("接收循环已通过令牌取消。"); break; }


                if (token.IsCancellationRequested) break;

                if (message != null)
                {
                    // 尝试将消息反序列化为 GameStateSnapshot_Client
                        GameStateSnapshot_Client gameState = JsonUtility.FromJson<GameStateSnapshot_Client>(message);
                        if (gameState != null && gameState.AllPlayerInputs != null)
                        {
                            foreach(var p in gameState.AllPlayerInputs)
                            {
                                // Debug.Log("----------------");
                                // Debug.Log(p.id);
                                // Debug.Log(p.inputUV.x.ToString()+" "+p.inputUV.y.ToString());
                                // Debug.Log("----------------");
                                var pi = new PlayerInput();
                                pi.inputUV = new LVector2(p.inputUV.x, p.inputUV.y);
                                pi.isJump = p.isJump;
                                pi.id = p.id;
                                GameManager.Instance.playerInputDict[p.id] = pi;
                            }
                        }
                }
                else // message is null
                {
                    Debug.Log("与服务器断开连接 (接收到null消息)。");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested) { Debug.LogError($"接收循环中发生错误: {e.ToString()}"); }
        }
        finally
        {
            Debug.Log("接收循环已结束。");
            if (_isConnected) // 如果循环意外结束但仍标记为已连接
            {
            }
        }
    }

    public void Disconnect()
    {
        if (!_isConnected && _client == null) return;
        
        Debug.Log("正在断开客户端连接...");
        _isConnected = false;

        if (_cancellationTokenSource != null)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            // Dispose CancellationTokenSource 应该在所有使用它的任务完成后，或者如果不再需要它
            // 但由于任务可能仍在运行中因Cancel而退出，立即Dispose可能导致ObjectDisposedException
            // 更好的做法是等待任务结束，或在确认任务已观察到取消后再Dispose。
            // 为简单起见，我们先Cancel，然后在关闭流和客户端后再Dispose。
        }

        try { _writer?.Close(); _writer = null; } catch (Exception ex) { Debug.LogWarning($"关闭写入器时出错: {ex.Message}"); }
        try { _reader?.Close(); _reader = null; } catch (Exception ex) { Debug.LogWarning($"关闭读取器时出错: {ex.Message}"); }
        try { _client?.Close(); _client = null; } catch (Exception ex) { Debug.LogWarning($"关闭客户端时出错: {ex.Message}"); }

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _receiveTask = null;
        _sendTask = null;
        Debug.Log("客户端已断开连接。");
    }

    void OnApplicationQuit() // MonoBehaviour 生命周期函数
    {
        Debug.Log("应用程序退出，断开客户端连接。");
        Disconnect();
    }

    public void OnDestroy() // MonoBehaviour 生命周期函数 (public 或 protected/private 都可以)
    {
        Debug.Log("SimplePollingClient 对象销毁，断开客户端连接。");
        Disconnect();
    }
}

// 确保您的项目中包含 UnityMainThreadDispatcher.cs 脚本。
// 例如:
// public class UnityMainThreadDispatcher : MonoBehaviour
// {
//     private static readonly System.Collections.Generic.Queue<Action> _executionQueue =
//         new System.Collections.Generic.Queue<Action>();
//     private static UnityMainThreadDispatcher _instance = null;

//     public static UnityMainThreadDispatcher Instance() { /* ... 实现 ... */ }
//     void Update() { /* ... 实现 ... */ }
//     public void Enqueue(Action action) { /* ... 实现 ... */ }
// }

[System.Serializable]
public class Vec2_DTO_Client
{
    public int x;
    public int y;

    public override string ToString()
    {
        return $"({x}, {y})";
    }
}

[System.Serializable]
public class PlayerInput_DTO_Client
{
    public int id; // 玩家ID
    public Vec2_DTO_Client inputUV;
    public bool isJump;

    public override string ToString()
    {
        return $"ID:{id}, UV:{inputUV}, Jump:{isJump}";
    }
}

[System.Serializable]
public class GameStateSnapshot_Client
{
    public List<PlayerInput_DTO_Client> AllPlayerInputs;
    public long ServerTimestamp; // 服务器发送时的时间戳 (UTC毫秒)

    public override string ToString()
    {
        return $"Timestamp: {ServerTimestamp}, PlayerCount: {AllPlayerInputs?.Count ?? 0}";
    }
}