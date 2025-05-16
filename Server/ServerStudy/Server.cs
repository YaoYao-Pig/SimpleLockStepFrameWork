using System;
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic;    // For List
using System.Diagnostics;
using System.IO;
using System.Linq; // For ToList()
using System.Net;
using System.Net.Sockets;
using System.Threading; // For CancellationTokenSource & CancellationToken
using System.Threading.Tasks;
using Lockstep.Math;
using Newtonsoft.Json; // 确保已通过NuGet添加 Newtonsoft.Json

namespace ServerStudy {
    public class Server {
        private TcpListener _acceptor;
        // private TcpClient client; // 这个共享的client成员变量问题较多，且主要逻辑已不使用它

        // 使用 ConcurrentDictionary 来线程安全地存储客户端的 StreamWriter
        // 键可以是客户端的唯一标识（例如 RemoteEndPoint.ToString()），值是 StreamWriter
        private readonly ConcurrentDictionary<string, StreamWriter> _clientWriters = new ConcurrentDictionary<string, StreamWriter>();
        private readonly CancellationTokenSource _serverShutdownTokenSource = new CancellationTokenSource(); // 用于控制服务器所有循环的关闭

        public float BroadcastIntervalSeconds { get; set; } = 0.1f; // 默认100ms广播一次 (10Hz)

        public Server() { }
        public Server(IPEndPoint iPEndPoint) {
            _acceptor = new TcpListener(iPEndPoint);
            _acceptor.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _acceptor.Server.NoDelay = true; // 通常为游戏服务器启用TCP_NODELAY以降低延迟
            Console.WriteLine("服务器开始监听...");
            _acceptor.Start();
            Console.WriteLine($"服务器正在监听端口: {iPEndPoint}");
        }

        public void Start() {
            // 启动接受客户端连接的循环
            _ = AcceptClientsLoopAsync(_serverShutdownTokenSource.Token);

            // 启动定时广播游戏状态的循环
            _ = BroadcastGameStateLoopAsync(_serverShutdownTokenSource.Token);

            Console.WriteLine("服务器已启动：正在接受客户端连接并定时广播游戏状态。");
        }

        // 停止服务器，释放资源
        public void Stop() {
            Console.WriteLine("正在停止服务器...");
            _serverShutdownTokenSource.Cancel(); // 通知所有循环停止
            _acceptor?.Stop(); // 停止接受新连接

            // 关闭所有现有客户端连接的写入器
            foreach (var writerEntry in _clientWriters) {
                try {
                    writerEntry.Value.Close(); // 这也会关闭底层流和客户端连接
                }
                catch (Exception ex) {
                    Console.WriteLine($"关闭客户端写入器时出错 ({writerEntry.Key}): {ex.Message}");
                }
            }
            _clientWriters.Clear();
            Console.WriteLine("服务器已停止。");
        }


        private async Task AcceptClientsLoopAsync(CancellationToken cancellationToken) {
            Console.WriteLine("开始接受客户端连接...");
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    TcpClient connectedClient = await _acceptor.AcceptTcpClientAsync().ConfigureAwait(false);
                    connectedClient.NoDelay = true; // 为每个连接的客户端也设置NoDelay
                    Console.WriteLine($"客户端已连接: {connectedClient.Client.RemoteEndPoint}");
                    // 为每个客户端启动一个独立的处理任务
                    _ = HandleClientAsync(connectedClient, cancellationToken);
                }
            }
            catch (ObjectDisposedException) {
                Console.WriteLine("TcpListener 已关闭，停止接受新连接。");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) // 忽略 OperationCanceledException 因为它是预期的关闭信号
            {
                Console.WriteLine($"接受客户端连接时发生错误: {ex.Message}");
            }
            finally {
                Console.WriteLine("接受客户端连接的循环已结束。");
            }
        }

        private async Task HandleClientAsync(TcpClient individualClient, CancellationToken serverToken) {
            string clientEndPointString = "未知客户端";
            StreamWriter writer = null;

            try {
                clientEndPointString = individualClient.Client.RemoteEndPoint.ToString();

                using (var stream = individualClient.GetStream())
                using (var reader = new StreamReader(stream)) // 用于从此客户端读取
                {
                    writer = new StreamWriter(stream) { AutoFlush = true }; // 用于向此客户端写入

                    // 将此客户端的 writer 添加到广播列表
                    if (!_clientWriters.TryAdd(clientEndPointString, writer)) {
                        Console.WriteLine($"警告: 无法为客户端 {clientEndPointString} 添加写入器到广播列表。可能已存在或并发问题。");
                        // 根据策略，可能需要关闭此连接
                        return;
                    }
                    Console.WriteLine($"客户端 {clientEndPointString} 的写入器已添加。");

                    // 循环处理来自此客户端的消息
                    while (individualClient.Connected && !serverToken.IsCancellationRequested) {
                        string line = null;
                        try {
                            // 使 ReadLineAsync 可被服务器关闭信号取消
                            var readLineTask = reader.ReadLineAsync();
                            var completedTask = await Task.WhenAny(readLineTask, Task.Delay(Timeout.Infinite, serverToken)).ConfigureAwait(false);

                            if (completedTask == readLineTask) {
                                line = await readLineTask.ConfigureAwait(false);
                            }
                            else // Task.Delay 完成，意味着 serverToken 被取消
                            {
                                Console.WriteLine($"服务器关闭，停止读取客户端 {clientEndPointString}。");
                                break;
                            }
                        }
                        catch (IOException) { Console.WriteLine($"客户端 {clientEndPointString} 可能已断开 (IO异常)。"); break; }
                        catch (ObjectDisposedException) { Console.WriteLine($"客户端 {clientEndPointString} 的流已释放。"); break; }
                        catch (OperationCanceledException) { Console.WriteLine($"读取客户端 {clientEndPointString} 被取消。"); break; }


                        if (line == null) { Console.WriteLine($"客户端 {clientEndPointString} 已正常断开连接。"); break; }

                        // Console.WriteLine($"[{clientEndPointString}] 原始消息: {line}"); // 调试时开启
                        try {
                            PlayerInput_DTO receivedInput = JsonConvert.DeserializeObject<PlayerInput_DTO>(line);
                            //Console.WriteLine("PlayerDTO:" + receivedInput.ToString());
                            JsonExtractor.TryExtractInputUVAsInt(line, out int inputX, out int inputY);
                            receivedInput.inputUV.x = inputX;
                            receivedInput.inputUV.y = inputY;
                             //Console.WriteLine("line: " + line);
                            
                            //Console.WriteLine(receivedInput.inputUV.x+"   "+ receivedInput.inputUV.y);
                            if (receivedInput != null) {
                                // 假设 ServerManager 和其 playerInputDic 存在且可访问
                                if (ServerManager.Instance != null) {
                                    // 检查玩家是否已记录，如果未记录则添加
                                    if (!ServerManager.Instance.IsPlayerEnter(receivedInput.id)) {
                                        ServerManager.Instance.AddPlayerId(receivedInput.id);
                                        Console.WriteLine($"玩家 {receivedInput.id} (来自 {clientEndPointString}) 已添加。");
                                    }
                                    // 更新该玩家的输入数据 (注意 playerInputDic 的线程安全)
                                    // ServerManager.Instance.playerInputDic.[receivedInput.id] 语法错误，应为 [key]
                                    ServerManager.Instance.playerInputDic[receivedInput.id] = receivedInput;
                                }
                                else {
                                    Console.WriteLine("警告: ServerManager.Instance 为 null，无法存储玩家输入。");
                                }
                                Console.WriteLine($"[{clientEndPointString}] 已处理输入: {receivedInput}");

                                // 可选: 向该客户端发送确认消息
                                // var ackMsg = new { status = "InputReceived", inputId = receivedInput.id };
                                // await writer.WriteLineAsync(JsonConvert.SerializeObject(ackMsg)).ConfigureAwait(false);
                            }
                            else {
                                Console.WriteLine($"[{clientEndPointString}] 反序列化消息为 null: {line}");
                            }
                        }
                        catch (JsonException jsonEx) {
                            Console.WriteLine($"[{clientEndPointString}] JSON反序列化错误: {jsonEx.Message} (消息: \"{line}\")");
                        }
                        catch (KeyNotFoundException knfEx) // 如果 ServerManager.Instance.playerInputDic[receivedInput.id] 的id不存在且未处理
                        {
                            //Console.WriteLine($"[{clientEndPointString}] 处理输入时键未找到错误 (ID: {receivedInput.id}): {knfEx.Message}");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"[{clientEndPointString}] 处理消息时发生错误: {ex.Message} (消息: \"{line}\")");
                        }
                    }
                } // using reader 和 stream 会在这里释放
            }
            catch (Exception ex) when (!(ex is OperationCanceledException || ex is ObjectDisposedException)) {
                Console.WriteLine($"处理客户端 {clientEndPointString} 时发生未预料的错误: {ex.ToString()}");
            }
            finally {
                // 从广播列表中移除此客户端的 writer
                if (writer != null && !string.IsNullOrEmpty(clientEndPointString)) {
                    if (_clientWriters.TryRemove(clientEndPointString, out _)) {
                        Console.WriteLine($"客户端 {clientEndPointString} 的写入器已移除。");
                    }
                }
                try {
                    writer?.Close(); // 尝试关闭写入器（如果还未关闭）
                    individualClient?.Close(); // 关闭客户端连接
                }
                catch (Exception ex) {
                    Console.WriteLine($"关闭客户端 {clientEndPointString} 资源时出错: {ex.Message}");
                }
                Console.WriteLine($"与客户端 {clientEndPointString} 的连接处理已结束。");
            }
        }

        private async Task BroadcastGameStateLoopAsync(CancellationToken cancellationToken) {
            Console.WriteLine("游戏状态广播循环已启动。");
            var broadcastInterval = TimeSpan.FromSeconds(BroadcastIntervalSeconds);

            while (!cancellationToken.IsCancellationRequested) {
                try {
                    
                    await Task.Delay(broadcastInterval, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested) break;

                    if (ServerManager.Instance == null || ServerManager.Instance.playerInputDic == null) {
                        Console.WriteLine("广播循环：ServerManager 或 playerInputDic 不可用。"); // 可能过于频繁
                        continue;
                    }

                    // 创建游戏状态快照
                    var gameState = new GameStateSnapshot {
                        // 重要: 如果 playerInputDic 不是线程安全的，或者在迭代时可能被修改，
                        // 需要先将其转换为一个列表或进行锁定操作。
                        // ToList() 创建一个浅拷贝，用于本次广播。
                        AllPlayerInputs = ServerManager.Instance.playerInputDic.Values.ToList(),
                        ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    if (!gameState.AllPlayerInputs.Any()) // 如果没有输入数据，则不广播
                    {
                        continue;
                    }
                  
                    string gameStateJson = JsonConvert.SerializeObject(gameState);

                    // 遍历所有连接的客户端并发送数据
                    // 使用 ToList() 创建一个副本以避免在迭代时修改集合（尽管 ConcurrentDictionary 通常支持并发读取）
                    List<KeyValuePair<string, StreamWriter>> writersSnapshot = _clientWriters.ToList();

                    foreach (var entry in writersSnapshot) {
                        StreamWriter writer = entry.Value;
                        string clientKey = entry.Key;
                        try {
                            if (writer.BaseStream.CanWrite) // 检查流是否仍然可写
                            {
                                await writer.WriteLineAsync(gameStateJson).ConfigureAwait(false);
                            }
                            else // 流不可写，说明连接可能已关闭
                            {
                                Console.WriteLine($"广播：客户端 {clientKey} 的流不可写，准备移除。");
                                _clientWriters.TryRemove(clientKey, out _); // 尝试移除
                            }
                        }
                        catch (IOException ex) // 通常表示客户端已断开
                        {
                            Console.WriteLine($"广播给客户端 {clientKey} 时发生IO错误 (可能已断开): {ex.Message}");
                            _clientWriters.TryRemove(clientKey, out _); // 移除断开的客户端
                        }
                        catch (ObjectDisposedException ex) // 流或写入器已被释放
                        {
                            Console.WriteLine($"广播给客户端 {clientKey} 时对象已释放: {ex.Message}");
                            _clientWriters.TryRemove(clientKey, out _); // 移除
                        }
                        catch (Exception ex) // 其他发送错误
                        {
                            Console.WriteLine($"广播给客户端 {clientKey} 时发生未知错误: {ex.Message}");
                            // 根据错误类型决定是否移除客户端
                        }
                    }
                    // Console.WriteLine($"已广播游戏状态给 {_clientWriters.Count} 个客户端。"); // 调试时开启
                }
                catch (OperationCanceledException) {
                    Console.WriteLine("游戏状态广播循环已取消。");
                    break;
                }
                catch (Exception ex) {
                    Console.WriteLine($"游戏状态广播循环发生错误: {ex.ToString()}");
                    // 为防止错误导致循环频繁失败，可以考虑短暂延迟
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
            }
            Console.WriteLine("游戏状态广播循环已结束。");
        }


        // ------ 以下是原始代码中与共享 this.client 相关的部分，问题较多，建议不使用 ------
        // 如果确实需要，应彻底重构以处理多客户端或确保其仅用于特定单客户端场景

        private TcpClient client; // 这个共享的 client 成员问题很多

        public async Task StartRecv() {
            Console.WriteLine("警告: StartRecv() 使用了有问题的共享 client 模式。");
            while (true) {
                await StartConnect();
            }
        }

        public async Task StartConnect() {
            Console.WriteLine("警告: StartConnect() 使用了有问题的共享 client 模式。");
            this.client = await _acceptor.AcceptTcpClientAsync(); // 覆盖共享 client
            Console.WriteLine($"客户端通过 StartConnect 连接并分配给共享成员: {this.client.Client.RemoteEndPoint}");
            S(); // S() 使用共享 client
        }

        public async void S() { // async void 通常只用于事件处理程序顶层
            Console.WriteLine("警告: S() 使用了有问题的共享 client 模式且是 async void。");
            if (this.client == null || !this.client.Connected) {
                Console.WriteLine("S(): 共享 client 为 null 或未连接。");
                return;
            }
            Console.WriteLine($"S(): 正在为共享 client 处理消息: {this.client.Client.RemoteEndPoint}");
            try {
                using (var stream = this.client.GetStream())
                using (var reader = new StreamReader(stream)) {
                    while (this.client.Connected) {
                        string line = null;
                        try { line = await reader.ReadLineAsync(); }
                        catch (IOException) { Console.WriteLine($"S(): 客户端 {this.client.Client.RemoteEndPoint} 断开 (IO异常)。"); break; }

                        if (line == null) { Console.WriteLine($"S(): 客户端 {this.client.Client.RemoteEndPoint} 断开 (null 行)。"); break; }
                        Console.WriteLine($"S(): [{this.client.Client.RemoteEndPoint}] 原始消息: {line}");
                        try {
                            PlayerInput_DTO receivedInput = JsonConvert.DeserializeObject<PlayerInput_DTO>(line);
                            if (receivedInput != null) {
                                Console.WriteLine($"S(): [{this.client.Client.RemoteEndPoint}] 反序列化输入: {receivedInput}");
                            }
                            else {
                                Console.WriteLine($"S(): [{this.client.Client.RemoteEndPoint}] 反序列化为 null: {line}");
                            }
                        }
                        catch (JsonException jsonEx) {
                            Console.WriteLine($"S(): [{this.client.Client.RemoteEndPoint}] JSON反序列化错误: {jsonEx.Message}");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"S(): [{this.client.Client.RemoteEndPoint}] 处理消息错误: {ex.Message}");
                        }
                    }
                }
            }
            catch (ObjectDisposedException) {
                Console.WriteLine($"S(): 客户端 {this.client?.Client?.RemoteEndPoint} 的流可能已释放。");
            }
            catch (Exception ex) {
                Console.WriteLine($"S(): 未处理异常 (客户端 {this.client?.Client?.RemoteEndPoint}): {ex.Message}");
            }
            finally {
                Console.WriteLine($"S(): 退出 (客户端 {this.client?.Client?.RemoteEndPoint})。");
            }
        }

        public void DoUpdate() {
            // 此方法当前为空，可用于服务器范围内的周期性非网络任务（如果由外部循环驱动）
        }
    }
}

public class Vec2_DTO {
    public int x { get; set; }
    public int y { get; set; }

    public override string ToString() {
        return $"({x}, {y})";
    }
}

// DTO for receiving PlayerInput JSON from the client
public class PlayerInput_DTO {
    public int id;
    public Vec2_DTO inputUV { get; set; }
    public bool isJump { get; set; }

    public override string ToString() {
        return $"Id:{id}, InputUV: {inputUV}, IsJump: {isJump}";
    }
}

public class GameStateSnapshot {
    public List<PlayerInput_DTO> AllPlayerInputs { get; set; }
    public long ServerTimestamp { get; set; } // 服务器时间戳 (UTC毫秒)

    public GameStateSnapshot() {
        AllPlayerInputs = new List<PlayerInput_DTO>();
        // ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 在填充时设置
    }
}