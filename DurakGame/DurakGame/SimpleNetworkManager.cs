using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DurakGame
{
    public class SimpleNetworkManager
    {
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private bool isHost = false;
        private int port = 12345;
        private bool isConnected = false;
        private string opponentName = "";
        private string playerName = "";

        // События
        public event Action<string> OnMessageReceived;
        public event Action<bool> OnConnectionStatusChanged;
        public event Action<string> OnOpponentConnected;
        public event Action<GameState> OnGameStateReceived;
        public event Action<GameMove> OnGameMoveReceived;
        public event Action<string, string> OnChatMessageReceived;
        public event Action<Suit> OnTrumpCardReceived;
        public event Action OnGameStartReceived;

        private readonly string MessageSeparator = "|||END|||";
        private StringBuilder messageBuffer = new StringBuilder();

        public SimpleNetworkManager()
        {
        }

        // Создание игры (хост)
        public async Task<bool> CreateGame(Player hostPlayer, int customPort = 12345)
        {
            try
            {
                port = customPort;
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                isHost = true;
                playerName = hostPlayer.Name;

                OnMessageReceived?.Invoke($"✅ Сервер создан на порту {port}");
                OnMessageReceived?.Invoke($"Ваш IP: {GetLocalIPAddress()}\nПорт: {port}");

                // Ожидаем подключения
                _ = Task.Run(async () =>
                {
                    try
                    {
                        client = await server.AcceptTcpClientAsync();
                        stream = client.GetStream();
                        isConnected = true;

                        // Отправляем свое имя
                        await SendMessage($"PLAYER_INFO|{playerName}");

                        // Запускаем прослушивание
                        _ = Task.Run(ListenForMessages);

                        OnConnectionStatusChanged?.Invoke(true);
                        OnMessageReceived?.Invoke("✅ Игрок подключился!");
                    }
                    catch (Exception ex)
                    {
                        OnMessageReceived?.Invoke($"❌ Ошибка: {ex.Message}");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                OnMessageReceived?.Invoke($"❌ Ошибка создания сервера: {ex.Message}");
                return false;
            }
        }

        // Подключение к игре
        public async Task<bool> JoinGame(string ipAddress, int port, Player player)
        {
            try
            {
                this.port = port;
                playerName = player.Name;
                client = new TcpClient();

                await client.ConnectAsync(ipAddress, port);

                if (client.Connected)
                {
                    stream = client.GetStream();
                    isHost = false;
                    isConnected = true;

                    // Отправляем свое имя
                    await SendMessage($"PLAYER_INFO|{playerName}");

                    // Запускаем прослушивание
                    _ = Task.Run(ListenForMessages);

                    OnConnectionStatusChanged?.Invoke(true);
                    OnMessageReceived?.Invoke($"✅ Подключено к игре!");

                    return true;
                }
            }
            catch (Exception ex)
            {
                OnMessageReceived?.Invoke($"❌ Ошибка подключения: {ex.Message}");
            }
            return false;
        }

        // Отправка сообщения
        public async Task SendMessage(string message)
        {
            if (!isConnected || stream == null) return;

            try
            {
                string formattedMessage = message + MessageSeparator;
                byte[] bytes = Encoding.UTF8.GetBytes(formattedMessage);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                OnMessageReceived?.Invoke($"❌ Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        // Отправка хода
        public async Task SendGameMove(GameMove move)
        {
            if (isConnected)
            {
                var serializedMove = JsonSerializer.Serialize(move);
                await SendMessage($"GAME_MOVE|{serializedMove}");
            }
        }

        // В класс SimpleNetworkManager добавьте этот метод:
        public async Task SendGameState(GameState state)
        {
            if (isConnected)
            {
                try
                {
                    var serializedState = JsonSerializer.Serialize(state);
                    await SendMessage($"GAME_STATE|{serializedState}");
                }
                catch (Exception ex)
                {
                    OnMessageReceived?.Invoke($"❌ Ошибка отправки состояния: {ex.Message}");
                }
            }
        }

        // Отправка состояния игры
        public async Task SendGameStart()
        {
            if (isConnected)
            {
                await SendMessage($"GAME_START|{playerName}");
            }
        }

        // Отправка козыря
        public async Task SendTrumpCard(Suit trumpSuit)
        {
            if (isConnected)
            {
                await SendMessage($"TRUMP_CARD|{(int)trumpSuit}");
            }
        }

        // Отправка сообщения в чат
        public async Task SendChatMessage(string message)
        {
            if (isConnected)
            {
                await SendMessage($"CHAT|{playerName}|{message}");
            }
        }

        // Прослушивание сообщений
        private async Task ListenForMessages()
        {
            var buffer = new byte[4096];

            while (client?.Connected == true && isConnected)
            {
                try
                {
                    if (stream == null || !stream.CanRead) break;

                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessReceivedData(receivedData);
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }

            Disconnect();
        }

        private void ProcessReceivedData(string data)
        {
            messageBuffer.Append(data);
            string bufferContent = messageBuffer.ToString();
            var messages = bufferContent.Split(new[] { MessageSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (messages.Length > 0)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(messages[i]))
                    {
                        ProcessSingleMessage(messages[i].Trim());
                    }
                }
                messageBuffer.Clear();
            }
        }

        private void AddDebugMessage(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[NETWORK] {DateTime.Now:HH:mm:ss} - {message}");
        }

        private void ProcessSingleMessage(string message)
        {
            var parts = message.Split('|');
            if (parts.Length < 1) return;

            string type = parts[0];

            switch (type)
            {
                case "PLAYER_INFO":
                    if (parts.Length >= 2)
                    {
                        opponentName = parts[1];
                        OnOpponentConnected?.Invoke(opponentName);
                    }
                    break;

                case "GAME_MOVE":
                    if (parts.Length >= 2)
                    {
                        try
                        {
                            var move = JsonSerializer.Deserialize<GameMove>(parts[1]);
                            if (move != null)
                            {
                                OnGameMoveReceived?.Invoke(move);
                            }
                        }
                        catch { }
                    }
                    break;

                case "GAME_STATE":
                    if (parts.Length >= 2)
                    {
                        try
                        {
                            var state = JsonSerializer.Deserialize<GameState>(parts[1]);
                            if (state != null)
                            {
                                OnGameStateReceived?.Invoke(state);
                            }
                        }
                        catch { }
                    }
                    break;

                case "GAME_START":
                    OnGameStartReceived?.Invoke();
                    AddDebugMessage($"🎮 Получено сообщение о начале игры");
                    break;

                case "TRUMP_CARD":
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int trumpSuitValue))
                    {
                        OnTrumpCardReceived?.Invoke((Suit)trumpSuitValue);
                    }
                    break;

                case "CHAT":
                    if (parts.Length >= 3)
                    {
                        var sender = parts[1];
                        var chatMessage = string.Join("|", parts.Skip(2));
                        OnChatMessageReceived?.Invoke(sender, chatMessage);
                    }
                    break;

                default:
                    OnMessageReceived?.Invoke(message);
                    break;
            }
        }

        public void Disconnect()
        {
            try
            {
                server?.Stop();
                client?.Close();
                stream?.Close();
                isConnected = false;
                OnConnectionStatusChanged?.Invoke(false);
            }
            catch { }
        }

        // Вспомогательные методы
        public string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public static int FindAvailablePort(int startPort = 12345, int maxAttempts = 50)
        {
            for (int port = startPort; port < startPort + maxAttempts; port++)
            {
                try
                {
                    using (var tester = new TcpListener(IPAddress.Loopback, port))
                    {
                        tester.Start();
                        tester.Stop();
                        return port;
                    }
                }
                catch { }
            }
            return startPort;
        }

        // Свойства
        public bool IsConnected => isConnected;
        public bool IsHost => isHost;
        public int Port => port;
        public string OpponentName => opponentName;
        public string PlayerName => playerName;
    }
}