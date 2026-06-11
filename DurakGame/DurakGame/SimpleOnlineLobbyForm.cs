using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace DurakGame
{
    public partial class SimpleOnlineLobbyForm : Form
    {
        private Player currentPlayer;
        private SimpleNetworkManager networkManager;
        private RegistrationForm parentForm;

        private TextBox txtChat, txtMessage, txtIP, txtPort;
        private Button btnCreateGame, btnJoinGame, btnDisconnect, btnSendMessage;
        private Label lblConnectionStatus;

        public SimpleOnlineLobbyForm(Player player, RegistrationForm parent = null)
        {
            InitializeComponent();
            currentPlayer = player;
            parentForm = parent;
            networkManager = new SimpleNetworkManager();
            SetupNetworkEvents();
            SetupUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 600);
            this.Name = "SimpleOnlineLobbyForm";
            this.Text = "Простое сетевое подключение";
            this.ResumeLayout(false);
        }

        private void SetupUI()
        {
            this.Text = $"🌐 Сетевая игра - {currentPlayer.Name}";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightBlue;

            // Заголовок
            var lblTitle = new Label
            {
                Text = $"🌐 ПРОСТОЕ СЕТЕВОЕ ПОДКЛЮЧЕНИЕ - {currentPlayer.Name}",
                Location = new Point(50, 20),
                Size = new Size(800, 40),
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Панель подключения
            var connectionPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(800, 150),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            lblConnectionStatus = new Label
            {
                Text = "🔴 Не подключено",
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red
            };

            var lblYourIP = new Label
            {
                Text = $"Ваш IP: {networkManager.GetLocalIPAddress()}",
                Location = new Point(10, 35),
                Size = new Size(400, 20),
                Font = new Font("Arial", 9)
            };

            // Создание игры
            var lblCreateGame = new Label
            {
                Text = "Создать игру:",
                Location = new Point(10, 60),
                Size = new Size(100, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            var lblPort = new Label
            {
                Text = "Порт:",
                Location = new Point(120, 60),
                Size = new Size(40, 20)
            };

            txtPort = new TextBox
            {
                Location = new Point(160, 58),
                Size = new Size(60, 20),
                Text = "12345"
            };

            btnCreateGame = new Button
            {
                Text = "🏠 Создать игру",
                Location = new Point(230, 55),
                Size = new Size(150, 30),
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnCreateGame.Click += async (s, e) => await CreateGame();

            // Присоединение к игре
            var lblJoinGame = new Label
            {
                Text = "Присоединиться:",
                Location = new Point(10, 100),
                Size = new Size(120, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            var lblIP = new Label
            {
                Text = "IP:",
                Location = new Point(140, 100),
                Size = new Size(30, 20)
            };

            txtIP = new TextBox
            {
                Location = new Point(170, 98),
                Size = new Size(150, 20),
                Text = "localhost"
            };

            var lblJoinPort = new Label
            {
                Text = "Порт:",
                Location = new Point(330, 100),
                Size = new Size(40, 20)
            };

            var txtJoinPort = new TextBox
            {
                Location = new Point(370, 98),
                Size = new Size(60, 20),
                Text = "12345"
            };

            btnJoinGame = new Button
            {
                Text = "🔗 Присоединиться",
                Location = new Point(440, 95),
                Size = new Size(150, 30),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnJoinGame.Click += async (s, e) =>
            {
                if (int.TryParse(txtJoinPort.Text, out int port))
                {
                    await JoinGame(txtIP.Text, port);
                }
            };

            btnDisconnect = new Button
            {
                Text = "❌ Отключиться",
                Location = new Point(600, 95),
                Size = new Size(150, 30),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = false
            };
            btnDisconnect.Click += (s, e) => Disconnect();

            connectionPanel.Controls.AddRange(new Control[] {
                lblConnectionStatus, lblYourIP, lblCreateGame, lblPort, txtPort, btnCreateGame,
                lblJoinGame, lblIP, txtIP, lblJoinPort, txtJoinPort, btnJoinGame, btnDisconnect
            });

            // Чат
            var chatPanel = new Panel
            {
                Location = new Point(50, 230),
                Size = new Size(800, 250),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var lblChat = new Label
            {
                Text = "💬 Чат:",
                Location = new Point(10, 10),
                Size = new Size(780, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            txtChat = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(780, 170),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            txtMessage = new TextBox
            {
                Location = new Point(10, 210),
                Size = new Size(650, 25),
                Font = new Font("Arial", 9)
            };
            txtMessage.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    SendChatMessage();
                    e.Handled = true;
                }
            };

            btnSendMessage = new Button
            {
                Text = "Отправить",
                Location = new Point(670, 210),
                Size = new Size(120, 25),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnSendMessage.Click += (s, e) => SendChatMessage();

            chatPanel.Controls.AddRange(new Control[] { lblChat, txtChat, txtMessage, btnSendMessage });

            // Управление
            var controlPanel = new Panel
            {
                Location = new Point(50, 490),
                Size = new Size(800, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightYellow
            };

            var lblHelp = new Label
            {
                Text = "💡 Подсказка: 1. Создайте игру и сообщите IP и порт другу. 2. Присоединяйтесь по IP и порту друга.",
                Location = new Point(10, 10),
                Size = new Size(780, 20),
                Font = new Font("Arial", 9)
            };

            var btnBack = new Button
            {
                Text = "← Назад в меню",
                Location = new Point(10, 35),
                Size = new Size(780, 20),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnBack.Click += (s, e) =>
            {
                SoundManager.PlayButtonClick();
                networkManager?.Disconnect();
                this.Close();
                parentForm?.Show();
            };

            controlPanel.Controls.AddRange(new Control[] { lblHelp, btnBack });

            this.Controls.AddRange(new Control[] {
                lblTitle, connectionPanel, chatPanel, controlPanel
            });
        }

        private void SetupNetworkEvents()
        {
            networkManager.OnMessageReceived += OnNetworkMessageReceived;
            networkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
            networkManager.OnChatMessageReceived += OnChatMessageReceived;
            networkManager.OnOpponentConnected += OnOpponentConnected;
            networkManager.OnGameStartReceived += OnGameStartReceived; // Добавьте это!
            networkManager.OnGameStateReceived += OnGameStateReceived; // И это!
        }

        private void OnGameStartReceived()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnGameStartReceived));
                return;
            }

            AddChatMessage("Система", "🎮 Хост запустил игру!");

            // Запускаем игру для клиента
            this.Hide();
            var gameForm = new MainGameForm(
                currentPlayer,
                new Player(networkManager.OpponentName, "", true),
                networkManager,
                GameMode.ThrowIn,
                false);
            gameForm.Show();
            gameForm.FormClosed += (s, e) => this.Close();
        }

        private void OnGameStateReceived(GameState state)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<GameState>(OnGameStateReceived), state);
                return;
            }

            if (state != null)
            {
                AddChatMessage("Система", "🔄 Получено состояние игры от хоста");
                // Можно сохранить состояние для передачи в игровую форму
            }
        }

        private async Task CreateGame()
        {
            SoundManager.PlayButtonClick();

            if (networkManager.IsConnected)
            {
                MessageBox.Show("Уже подключены к игре", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Введите корректный порт", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddChatMessage("Система", $"🔄 Создание игры на порту {port}...");

            var success = await networkManager.CreateGame(currentPlayer, port);
            if (success)
            {
                UpdateConnectionStatus();
                btnCreateGame.Enabled = false;
                btnJoinGame.Enabled = false;
                btnDisconnect.Enabled = true;

                AddChatMessage("Система", "✅ Игра создана! Ожидаем подключения друга...");
                AddChatMessage("Система", $"📢 Сообщите другу: IP: {networkManager.GetLocalIPAddress()}, Порт: {port}");
            }
            else
            {
                AddChatMessage("Система", "❌ Не удалось создать игру");
            }
        }

        private async Task JoinGame(string ipAddress, int port)
        {
            SoundManager.PlayButtonClick();

            if (networkManager.IsConnected)
            {
                MessageBox.Show("Уже подключены к игре", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddChatMessage("Система", $"🔄 Подключение к {ipAddress}:{port}...");

            var success = await networkManager.JoinGame(ipAddress, port, currentPlayer);
            if (success)
            {
                UpdateConnectionStatus();
                btnCreateGame.Enabled = false;
                btnJoinGame.Enabled = false;
                btnDisconnect.Enabled = true;

                AddChatMessage("Система", "✅ Подключено! Ожидаем начала игры...");
            }
            else
            {
                AddChatMessage("Система", "❌ Не удалось подключиться");
            }
        }

        private void Disconnect()
        {
            SoundManager.PlayButtonClick();
            networkManager?.Disconnect();
            UpdateConnectionStatus();

            btnCreateGame.Enabled = true;
            btnJoinGame.Enabled = true;
            btnDisconnect.Enabled = false;

            AddChatMessage("Система", "📴 Отключено от игры");
        }

        private void SendChatMessage()
        {
            if (!string.IsNullOrEmpty(txtMessage.Text) && networkManager.IsConnected)
            {
                networkManager.SendChatMessage(txtMessage.Text);
                AddChatMessage(currentPlayer.Name, txtMessage.Text);
                txtMessage.Text = "";
            }
        }

        // Обработчики событий сети
        private void OnNetworkMessageReceived(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(OnNetworkMessageReceived), message);
                return;
            }
            AddChatMessage("Сеть", message);
        }

        private void OnChatMessageReceived(string playerName, string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string>(OnChatMessageReceived), playerName, message);
                return;
            }
            AddChatMessage(playerName, message);
        }

        private async void OnOpponentConnected(string opponentName)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(OnOpponentConnected), opponentName);
                return;
            }

            AddChatMessage("Система", $"🤝 Оппонент подключен: {opponentName}");

            // Если мы хост, запускаем игру
            if (networkManager.IsHost)
            {
                // Ждем 1 секунду для стабилизации
                await Task.Delay(1000);

                if (networkManager.IsConnected && !this.IsDisposed)
                {
                    try
                    {
                        // Отправляем клиенту сообщение о начале игры
                        await networkManager.SendGameStart();

                        // Ждем еще немного
                        await Task.Delay(500);

                        this.Hide();
                        var gameForm = new MainGameForm(
                            currentPlayer,
                            new Player(opponentName, "", true),
                            networkManager,
                            GameMode.ThrowIn,
                            true);
                        gameForm.Show();
                        gameForm.FormClosed += (s, e) =>
                        {
                            if (!this.IsDisposed)
                                this.Close();
                        };
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка создания игры: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnConnectionStatusChanged), isConnected);
                return;
            }

            UpdateConnectionStatus();

            if (isConnected)
            {
                AddChatMessage("Система", "✅ Подключение установлено");
            }
            else
            {
                AddChatMessage("Система", "❌ Соединение потеряно");
            }
        }

        private void UpdateConnectionStatus()
        {
            if (lblConnectionStatus != null)
            {
                if (networkManager.IsConnected)
                {
                    if (networkManager.IsHost)
                    {
                        lblConnectionStatus.Text = $"🟢 Хост ({networkManager.Port})";
                        lblConnectionStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblConnectionStatus.Text = $"🟢 Подключен к игре";
                        lblConnectionStatus.ForeColor = Color.Green;
                    }
                }
                else
                {
                    lblConnectionStatus.Text = "🔴 Не подключено";
                    lblConnectionStatus.ForeColor = Color.Red;
                }
            }
        }

        private void AddChatMessage(string sender, string message)
        {
            if (txtChat != null)
            {
                txtChat.AppendText($"[{DateTime.Now:HH:mm:ss}] {sender}: {message}\r\n");
                txtChat.SelectionStart = txtChat.Text.Length;
                txtChat.ScrollToCaret();
            }
        }
    }
}