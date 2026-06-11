using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace DurakGame
{
    public partial class MainGameForm : Form
    {
        private Game game;
        private PictureBox[] playerCards;
        private PictureBox[] computerCards;
        private PictureBox[] tableCards;
        private PictureBox trumpCard;
        private Button btnTakeCards, btnPass, btnBackToMenu, btnSurrender, btnNothingToThrow, btnTransfer;
        private Label lblStatus, lblDeckCount, lblTrumpSuit, lblPlayerInfo, lblOpponentInfo;
        private Label lblCurrentTurn;
        private Label lblPlayerStats, lblComputerStats;
        private TextBox txtChat; // ДОБАВЛЕНО: Поле для чата
        private Player currentPlayer;
        private bool isHost = false; // Значение по умолчанию для локальной игры
        private SimpleNetworkManager networkManager = null; // Только для онлайн игр
        public MainGameForm(Player humanPlayer, Player opponent, GameMode mode, bool isLocalMultiplayer = false)
        {
            InitializeComponent();
            currentPlayer = humanPlayer;

            System.Diagnostics.Debug.WriteLine($"Создание MainGameForm: {humanPlayer.Name} vs {opponent.Name}");

            game = new Game(humanPlayer, opponent, mode, isLocalMultiplayer);
            game.GameStateChanged += OnGameStateChanged;
            game.GameMessage += OnGameMessage;
            game.GameEnded += ShowGameResult;

            SetupUI();
            UpdateUI();

            System.Diagnostics.Debug.WriteLine($"Игра создана: IsGameActive={game.IsGameActive}, " +
                                             $"CurrentAttacker={game.CurrentAttacker?.Name}");
        }

        public MainGameForm(Player humanPlayer, Player opponent, SimpleNetworkManager networkManager, GameMode mode, bool isHost = false)
        {
            InitializeComponent();
            currentPlayer = humanPlayer;
            this.isHost = isHost;
            this.networkManager = networkManager;
            game = new Game(humanPlayer, opponent, mode, false, true); // true для онлайн режима
            game.GameStateChanged += OnGameStateChanged;
            game.GameMessage += OnGameMessage;
            game.GameEnded += ShowGameResult;
            SetupUI();
            UpdateUI();
        }

        // В MainGameForm добавить этот метод:
        public async Task SendFullGameState()
        {
            if (networkManager != null)
            {
                var gameState = CreateCompleteGameState();
                await networkManager.SendGameState(gameState);
            }
        }

        // ДОБАВЬТЕ ЭТОТ МЕТОД:
        private GameState CreateCompleteGameState()
        {
            return new GameState
            {
                Deck = new List<Card>(game.Deck.Select(c => new Card(c.Suit, c.Rank, c.Suit == game.TrumpSuit))),
                Table = new List<Card>(game.Table.Select(c => new Card(c.Suit, c.Rank, c.Suit == game.TrumpSuit))),
                TrumpSuit = game.TrumpSuit,
                IsGameActive = game.IsGameActive,
                Mode = game.Mode,
                CurrentAttackerIndex = game.CurrentAttacker == game.HumanPlayer ? 0 : 1,
                CurrentDefenderIndex = game.CurrentDefender == game.HumanPlayer ? 0 : 1,
                Player1Hand = new List<Card>(game.HumanPlayer.Hand.Select(c => new Card(c.Suit, c.Rank, c.Suit == game.TrumpSuit))),
                Player2Hand = new List<Card>(game.ComputerPlayer.Hand.Select(c => new Card(c.Suit, c.Rank, c.Suit == game.TrumpSuit))),
                Player1Name = game.HumanPlayer.Name,
                Player2Name = game.ComputerPlayer.Name,
                SaveTime = DateTime.Now
            };
        }

        private void SetupUI()
        {
            this.Text = "🎮 Дурак - Карточная игра";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(34, 139, 34);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            CreateTurnIndicator();
            CreatePlayerCards();
            CreateComputerCards();
            CreateTableArea();
            CreateControlButtons();
            CreateInfoLabels();
            CreatePlayerStats();
            CreateComputerStats();
            CreateChatArea(); // ДОБАВЛЕНО: Создание области чата
        }

        private void CreateTurnIndicator()
        {
            lblCurrentTurn = new Label
            {
                Location = new Point(450, 20),
                Size = new Size(300, 40),
                BackColor = Color.Transparent,
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "ВАШ ХОД (АТАКА)"
            };
            this.Controls.Add(lblCurrentTurn);
        }

        private void CreatePlayerCards()
        {
            playerCards = new PictureBox[12];

            for (int i = 0; i < playerCards.Length; i++)
            {
                playerCards[i] = new PictureBox
                {
                    Size = new Size(80, 120),
                    Location = new Point(300 + i * 40, 600),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                    Cursor = Cursors.Hand,
                    Tag = i,
                    Visible = false
                };
                playerCards[i].Click += PlayerCard_Click;
                playerCards[i].MouseEnter += (s, e) =>
                {
                    var pb = (PictureBox)s;
                    if (pb.Enabled && pb.Visible)
                    {
                        pb.BringToFront();
                        pb.Location = new Point(pb.Location.X, 580);
                    }
                };
                playerCards[i].MouseLeave += (s, e) =>
                {
                    var pb = (PictureBox)s;
                    if (pb.Enabled && pb.Visible)
                    {
                        int index = (int)pb.Tag;
                        pb.Location = new Point(300 + index * 40, 600);
                    }
                };
                this.Controls.Add(playerCards[i]);
            }
        }

        private void CreateComputerCards()
        {
            computerCards = new PictureBox[12];
            for (int i = 0; i < computerCards.Length; i++)
            {
                computerCards[i] = new PictureBox
                {
                    Size = new Size(80, 120),
                    Location = new Point(300 + i * 40, 80),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.DarkRed,
                    Image = CardImageManager.GetCardBackImage(),
                    Visible = false
                };
                this.Controls.Add(computerCards[i]);
            }
        }

        private void CreateTableArea()
        {
            tableCards = new PictureBox[12];
            for (int i = 0; i < 12; i++)
            {
                tableCards[i] = new PictureBox
                {
                    Size = new Size(80, 120),
                    Visible = false,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(tableCards[i]);
            }
        }

        private void CreateControlButtons()
        {
            // Панель для кнопок - ПОДНИМАЕМ ВЫШЕ
            var controlPanel = new Panel
            {
                Location = new Point(50, 450), // ПОДНЯЛИ: было 500, стало 450
                Size = new Size(200, 250),
                BackColor = Color.FromArgb(200, 255, 255, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Остальной код создания кнопок остается без изменений...
            btnSurrender = new Button
            {
                Text = "Сдаться",
                Location = new Point(20, 20),
                Size = new Size(160, 35),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnSurrender.Click += (s, e) =>
            {
                var result = MessageBox.Show("Вы уверены, что хотите сдаться?", "Сдача",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    game.Surrender();
                    UpdateUI();
                }
            };

            var btnSync = new Button
            {
                Text = "🔄 Синхронизировать",
                Location = new Point(20, 270),
                Size = new Size(160, 35),
                BackColor = Color.Purple,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = isHost
            };
            btnSync.Click += (s, e) =>
            {
                if (isHost)
                {
                    SendFullGameState();
                    AddChatMessage("Система", "🔄 Запрос синхронизации отправлен");
                }
            };

            controlPanel.Controls.Add(btnSync);

            btnTakeCards = new Button
            {
                Text = "Взять карты",
                Location = new Point(20, 70),
                Size = new Size(160, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnTakeCards.Click += (s, e) =>
            {
                if (game.CurrentDefender == game.HumanPlayer && game.Table.Count > 0)
                {
                    game.HumanTakeCards();
                    UpdateUI();
                }
            };

            btnPass = new Button
            {
                Text = "Бито",
                Location = new Point(20, 120),
                Size = new Size(160, 35),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnPass.Click += (s, e) =>
            {
                if (game.CurrentAttacker == game.HumanPlayer && game.Table.Count > 0)
                {
                    if (game.Table.Count % 2 == 0 && game.AllCardsBeaten())
                    {
                        game.HumanPass();
                        UpdateUI();
                    }
                    else
                    {
                        MessageBox.Show("Не все карты побиты! Нельзя завершить ход.",
                            "Невозможно завершить ход", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            

            btnNothingToThrow = new Button
            {
                Text = "Нечего подкидывать",
                Location = new Point(20, 220),
                Size = new Size(160, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnNothingToThrow.Click += (s, e) =>
            {
                if (game.CurrentAttacker == game.HumanPlayer &&
                    game.Table.Count > 0 &&
                    !game.CanAttackMore())
                {
                    game.HumanPass();
                    UpdateUI();
                }
            };

            controlPanel.Controls.AddRange(new Control[] {
        btnSurrender, btnTakeCards, btnPass, btnNothingToThrow
    });
            this.Controls.Add(controlPanel);

            // Кнопка возврата в меню - также поднимаем
            btnBackToMenu = new Button
            {
                Text = "В меню",
                Location = new Point(1050, 650), // ПОДНЯЛИ: было 720, стало 650
                Size = new Size(120, 35),
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnBackToMenu.Click += (s, e) =>
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти в меню? Текущая игра будет потеряна.",
                    "Выход в меню", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    this.Close();
                    // ВОССТАНАВЛИВАЕМ уже зарегистрированный аккаунт
                    ReturnToRegistrationForm();
                }
            };
            this.Controls.Add(btnBackToMenu);
        }

        private void ReturnToRegistrationForm()
        {
            var registrationForm = new RegistrationForm();

            // Если у нас есть текущий игрок, пытаемся восстановить его сессию
            if (currentPlayer != null)
            {
                // Здесь можно передать данные текущего игрока в форму регистрации
                // Например, через конструктор или публичное свойство
                registrationForm.Show();
            }
            else
            {
                registrationForm.Show();
            }
        }

        private void CreateInfoLabels()
        {
            var infoPanel = new Panel
            {
                Location = new Point(50, 50),
                Size = new Size(250, 200), // Увеличили высоту для полного отображения карты
                BackColor = Color.FromArgb(200, 255, 255, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblDeckCount = new Label
            {
                Text = "Карт в колоде: 36",
                Location = new Point(10, 10),
                Size = new Size(230, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Black
            };

            lblTrumpSuit = new Label
            {
                Text = "Козырь: ♥",
                Location = new Point(10, 35),
                Size = new Size(230, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Black
            };

            trumpCard = new PictureBox
            {
                Size = new Size(100, 150), // Увеличили размер для полного отображения
                Location = new Point(75, 60), // Скорректировали позицию
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            infoPanel.Controls.AddRange(new Control[] { lblDeckCount, lblTrumpSuit, trumpCard });
            this.Controls.Add(infoPanel);
        }


        private void CreatePlayerStats()
        {
            lblPlayerStats = new Label
            {
                Location = new Point(950, 600),
                Size = new Size(200, 120),
                BackColor = Color.FromArgb(200, 255, 255, 255),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.FixedSingle,
                Text = $"{game.HumanPlayer.Name}\n\nКарт: 6\nПобед: {game.HumanPlayer.Wins}\nРейтинг: {game.HumanPlayer.WinRate:F1}%"
            };
            this.Controls.Add(lblPlayerStats);
        }

        private void CreateComputerStats()
        {
            lblComputerStats = new Label
            {
                Location = new Point(950, 80),
                Size = new Size(200, 120),
                BackColor = Color.FromArgb(200, 255, 255, 255),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.FixedSingle,
                Text = $"Компьютер\n\nКарт: 6\nПобед: {game.ComputerPlayer.Wins}\nРейтинг: {game.ComputerPlayer.WinRate:F1}%"
            };
            this.Controls.Add(lblComputerStats);
        }

        // ДОБАВЛЕНО: Создание области чата
        private void CreateChatArea()
        {
            var chatPanel = new Panel
            {
                Location = new Point(800, 300), // ПОДНЯЛИ ВЫШЕ: было 400, стало 300
                Size = new Size(350, 200), // Немного уменьшили высоту
                BackColor = Color.FromArgb(200, 255, 255, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblChat = new Label
            {
                Text = "💬 Игровой журнал:",
                Location = new Point(10, 10),
                Size = new Size(330, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            txtChat = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(330, 150), // Уменьшили высоту текстового поля
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Arial", 9)
            };

            chatPanel.Controls.AddRange(new Control[] { lblChat, txtChat });
            this.Controls.Add(chatPanel);
        }

        // ДОБАВЛЕНО: Метод для добавления сообщений в чат
        private void AddChatMessage(string sender, string message)
        {
            if (txtChat != null)
            {
                if (txtChat.InvokeRequired)
                {
                    txtChat.Invoke(new Action<string, string>(AddChatMessage), sender, message);
                    return;
                }

                txtChat.AppendText($"[{DateTime.Now:HH:mm:ss}] {sender}: {message}\r\n");
                txtChat.SelectionStart = txtChat.Text.Length;
                txtChat.ScrollToCaret();
            }
        }

        // ДОБАВЛЕНО: Метод для установки состояния игры
        public void SetGameState(Game loadedGame)
        {
            if (loadedGame != null)
            {
                // Останавливаем текущую игру если есть
                if (game != null && game.IsGameActive)
                {
                    game.IsGameActive = false;
                }

                // Заменяем игру на загруженную
                game = loadedGame;
                game.GameStateChanged += OnGameStateChanged;
                game.GameMessage += OnGameMessage;
                game.GameEnded += ShowGameResult;

                // Обновляем UI
                UpdateUI();

                AddChatMessage("Система", "✅ Игра успешно загружена!");
                AddChatMessage("Система", $"🃏 Восстановлено состояние: {game.HumanPlayer.Hand.Count} карт у вас, " +
                                         $"{game.ComputerPlayer.Hand.Count} у противника, " +
                                         $"{game.Deck.Count} в колоде");
            }
        }

        private void PlayerCard_Click(object sender, EventArgs e)
        {
            if (!(sender is PictureBox cardPb) || cardPb.Tag == null)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка: sender не PictureBox или Tag null");
                return;
            }

            int cardIndex = (int)cardPb.Tag;

            // Проверяем, что карта существует в руке
            if (cardIndex < 0 || cardIndex >= game.HumanPlayer.Hand.Count)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: неверный индекс карты {cardIndex}");
                return;
            }

            var card = game.HumanPlayer.Hand[cardIndex];

            System.Diagnostics.Debug.WriteLine($"Карта нажата: {card}, IsGameActive={game.IsGameActive}, " +
                                             $"CurrentAttacker={game.CurrentAttacker?.Name}");

            if (!game.IsGameActive)
            {
                MessageBox.Show("Игра не активна!", "Ошибка");
                return;
            }

            if (game.CurrentAttacker == game.HumanPlayer)
            {
                // Атака
                if (game.Table.Count == 0 || game.CanAttackWithCard(card))
                {
                    System.Diagnostics.Debug.WriteLine($"Атака картой {card}");
                    game.HumanAttack(cardIndex);
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Можно подкидывать только карты того же достоинства, что уже лежат на столе",
                        "Невозможно подкинуть", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (game.CurrentDefender == game.HumanPlayer && game.Table.Count > 0)
            {
                // Защита
                var attackingCard = game.Table[game.Table.Count - 1];
                if (card.CanBeat(attackingCard, game.TrumpSuit))
                {
                    System.Diagnostics.Debug.WriteLine($"Защита картой {card} против {attackingCard}");
                    game.HumanDefend(cardIndex);
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Эта карта не может побить атакующую карту!",
                        "Невозможно побить", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Не ваш ход: CurrentAttacker={game.CurrentAttacker?.Name}, " +
                                                 $"CurrentDefender={game.CurrentDefender?.Name}");
            }
        }

        private void UpdateUI()
        {
            UpdatePlayerCards();
            UpdateComputerCards();
            UpdateTableCards();
            UpdateGameInfo();
            UpdateControlButtons();
            UpdateStats();
            UpdateTurnIndicator();
        }

        private void UpdateTurnIndicator()
        {
            if (lblCurrentTurn != null)
            {
                if (!game.IsGameActive)
                {
                    lblCurrentTurn.Text = "🏁 Игра завершена";
                }
                else if (game.CurrentAttacker == game.HumanPlayer)
                {
                    lblCurrentTurn.Text = "🎯 ВАШ ХОД (АТАКА)";
                    System.Diagnostics.Debug.WriteLine("Ход игрока (атака)");
                }
                else if (game.CurrentDefender == game.HumanPlayer)
                {
                    lblCurrentTurn.Text = "🛡 ВАШ ХОД (ЗАЩИТА)";
                    System.Diagnostics.Debug.WriteLine("Ход игрока (защита)");
                }
                else
                {
                    lblCurrentTurn.Text = "⏳ ХОД КОМПЬЮТЕРА";
                    System.Diagnostics.Debug.WriteLine("Ход компьютера");
                }
            }
        }

        private void UpdatePlayerCards()
        {
            if (playerCards == null || game == null || game.HumanPlayer == null) return;

            // Сначала скрываем все карты
            for (int i = 0; i < playerCards.Length; i++)
            {
                playerCards[i].Visible = false;
                playerCards[i].Enabled = false;
                playerCards[i].BackColor = Color.White;
            }

            // Показываем карты игрока
            for (int i = 0; i < Math.Min(game.HumanPlayer.Hand.Count, playerCards.Length); i++)
            {
                var card = game.HumanPlayer.Hand[i];
                playerCards[i].Image = CardImageManager.GetCardImage(card.Suit, card.Rank);
                playerCards[i].Visible = true;
                playerCards[i].Tag = i;

                bool isEnabled = false;

                if (game.IsGameActive && game.CurrentAttacker == game.HumanPlayer)
                {
                    if (game.Table.Count == 0)
                    {
                        // Первый ход - можно ходить любой картой, кроме козырной если она не последняя
                        if (game.Deck.Count > 1 && card.IsTrump)
                        {
                            // Козырные карты лучше не ходить первыми
                            isEnabled = false;
                        }
                        else
                        {
                            isEnabled = true;
                        }
                    }
                    else if (game.CanAttackWithCard(card))
                    {
                        isEnabled = true;
                    }
                }
                else if (game.IsGameActive && game.CurrentDefender == game.HumanPlayer && game.Table.Count > 0)
                {
                    var attackingCard = game.Table[game.Table.Count - 1];
                    if (card.CanBeat(attackingCard, game.TrumpSuit))
                    {
                        isEnabled = true;
                    }
                }

                playerCards[i].Enabled = isEnabled;

                // Подсветка козырных карт
                if (card.IsTrump)
                {
                    playerCards[i].BackColor = isEnabled ? Color.LightYellow : Color.Gold;
                    // Добавляем рамку для козырных карт
                    playerCards[i].BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    playerCards[i].BackColor = isEnabled ? Color.LightGreen : Color.White;
                    playerCards[i].BorderStyle = BorderStyle.FixedSingle;
                }

                playerCards[i].Cursor = isEnabled ? Cursors.Hand : Cursors.Default;
                playerCards[i].Location = new Point(300 + i * 40, 600);
                playerCards[i].BringToFront();
            }
        }

        private void ForceUIUpdate()
        {
            UpdatePlayerCards();
            UpdateComputerCards();
            UpdateTableCards();
            UpdateGameInfo();
            UpdateControlButtons();
            UpdateStats();
            UpdateTurnIndicator();

            // Принудительное обновление формы
            this.Refresh();
            Application.DoEvents();
        }

        private void UpdateComputerCards()
        {
            foreach (var cardPb in computerCards)
            {
                cardPb.Visible = false;
            }

            for (int i = 0; i < game.ComputerPlayer.Hand.Count; i++)
            {
                if (i < computerCards.Length)
                {
                    computerCards[i].Visible = true;
                    computerCards[i].Image = CardImageManager.GetCardBackImage();

                    if (game.CurrentAttacker == game.ComputerPlayer && game.IsGameActive)
                    {
                        computerCards[i].BackColor = Color.Yellow;
                    }
                    else
                    {
                        computerCards[i].BackColor = Color.DarkRed;
                    }

                    computerCards[i].Location = new Point(300 + i * 40, 80);
                }
            }
        }

        private void ShowGameResult(Player? winner)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Player?>(ShowGameResult), winner);
                return;
            }

            string title = "";
            string message = "";
            MessageBoxIcon icon = MessageBoxIcon.Information;

            if (winner == null)
            {
                title = "🤝 НИЧЬЯ";
                message = "Оба игрока остались без карт!";
                icon = MessageBoxIcon.Information;
            }
            else if (winner == game.HumanPlayer)
            {
                title = "🎉 ПОБЕДА!";
                message = $"Поздравляем, {game.HumanPlayer.Name}! Вы выиграли эту партию!\n\n" +
                         $"Ваш рейтинг: {game.HumanPlayer.WinRate:F1}%";
                icon = MessageBoxIcon.Information;

                // Воспроизводим музыку победы
                SoundManager.PlayWin();
            }
            else
            {
                title = "😔 ПОРАЖЕНИЕ";
                message = $"К сожалению, {game.ComputerPlayer.Name} победил.\n\n" +
                         $"Ваш рейтинг: {game.HumanPlayer.WinRate:F1}%";
                icon = MessageBoxIcon.Exclamation;

                // Воспроизводим музыку поражения
                SoundManager.PlayLose();
            }

            // Показываем красивое сообщение
            var result = MessageBox.Show(message, title,
                MessageBoxButtons.OK, icon);

            // После закрытия сообщения предлагаем новую игру
            if (result == DialogResult.OK)
            {
                var playAgain = MessageBox.Show("Хотите сыграть еще раз?", "Новая игра",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (playAgain == DialogResult.Yes)
                {
                    // Запускаем новую игру
                    this.Close();
                    var newGameForm = new MainGameForm(game.HumanPlayer,
                        new Player("Компьютер", "", false), game.Mode);
                    newGameForm.Show();
                }
                else
                {
                    // Возвращаемся в меню
                    this.Close();
                    new RegistrationForm().Show();
                }
            }
        }

        private void UpdateTableCards()
        {
            // Сначала скрываем все карты
            for (int i = 0; i < 12; i++)
            {
                tableCards[i].Visible = false;
                tableCards[i].BorderStyle = BorderStyle.None;
            }

            // Отображаем карты на столе
            for (int i = 0; i < game.Table.Count; i++)
            {
                var card = game.Table[i];
                var cardImage = CardImageManager.GetCardImage(card.Suit, card.Rank);
                tableCards[i].Image = cardImage;

                int pairIndex = i / 2;
                int cardInPair = i % 2;

                int baseX = 450 + pairIndex * 90; // Увеличили расстояние между парами
                int baseY = 300;

                if (cardInPair == 0) // Атакующая карта
                {
                    tableCards[i].Location = new Point(baseX, baseY);
                    tableCards[i].BackColor = Color.LightCoral;
                    tableCards[i].BorderStyle = BorderStyle.FixedSingle;
                }
                else // Защитная карта - располагаем ПОД атакующей
                {
                    tableCards[i].Location = new Point(baseX, baseY + 40); // Смещаем вниз
                    tableCards[i].BackColor = Color.LightBlue;
                    tableCards[i].BorderStyle = BorderStyle.FixedSingle;
                }

                tableCards[i].Visible = true;
                tableCards[i].BringToFront();
            }

            // Управление Z-Order: защитные карты поверх атакующих
            for (int i = 0; i < game.Table.Count; i++)
            {
                if (i % 2 == 1) // Защитные карты
                {
                    tableCards[i].BringToFront();
                }
                else // Атакующие карты
                {
                    tableCards[i].SendToBack();
                }
            }

            // Принудительное обновление отображения
            this.Refresh();
        }

        private void UpdateGameInfo()
        {
            lblDeckCount.Text = $"Карт в колоде: {game.Deck.Count}";
            lblTrumpSuit.Text = $"Козырь: {GetSuitName(game.TrumpSuit)}";

            if (game.Deck.Count > 0)
            {
                var trumpCardFromDeck = new Card(game.TrumpSuit, Rank.Six);
                trumpCard.Image = CardImageManager.GetCardImage(trumpCardFromDeck.Suit, trumpCardFromDeck.Rank);
            }
            else
            {
                trumpCard.Image = CreateTrumpCardImage();
            }
        }

        private void UpdateStats()
        {
            if (lblPlayerStats != null)
            {
                lblPlayerStats.Text = $"{game.HumanPlayer.Name}\n\nКарт: {game.HumanPlayer.Hand.Count}\nПобед: {game.HumanPlayer.Wins}\nРейтинг: {game.HumanPlayer.WinRate:F1}%";
            }

            if (lblComputerStats != null)
            {
                lblComputerStats.Text = $"Компьютер\n\nКарт: {game.ComputerPlayer.Hand.Count}\nПобед: {game.ComputerPlayer.Wins}\nРейтинг: {game.ComputerPlayer.WinRate:F1}%";
            }
        }

        private void UpdateControlButtons()
        {
            // Проверяем, что элементы существуют
            if (btnTakeCards == null || btnPass == null || btnNothingToThrow == null ||
                btnSurrender == null || game == null)
                return;

            // Проверяем, что игра активна и элементы доступны
            try
            {
                // Кнопка "Взять карты" - доступна только защищающемуся когда есть непобитые карты
                btnTakeCards.Enabled = game.IsGameActive &&
                    game.CurrentDefender == game.HumanPlayer &&
                    game.Table.Count > 0 &&
                    game.Table.Count % 2 == 1;

                // Кнопка "Бито" - доступна только атакующему когда все карты побиты И есть хотя бы одна пара
                btnPass.Enabled = game.IsGameActive &&
                    game.CurrentAttacker == game.HumanPlayer &&
                    game.Table.Count > 0 &&
                    game.AllCardsBeaten();

                // Кнопка "Нечего подкидывать" - доступна когда атакующий не может больше подкинуть
                btnNothingToThrow.Enabled = game.IsGameActive &&
                    game.CurrentAttacker == game.HumanPlayer &&
                    game.Table.Count > 0 &&
                    game.AllCardsBeaten() &&
                    !game.CanAttackMore();

                // Проверяем, существует ли кнопка Transfer
                if (btnTransfer != null)
                {
                    btnTransfer.Enabled = false; // Переводной режим не используется
                    btnTransfer.BackColor = btnTransfer.Enabled ? Color.Orange : Color.Gray;
                }

                // Кнопка "Сдаться" - всегда доступна во время активной игры
                btnSurrender.Enabled = game.IsGameActive;

                // Визуальная обратная связь
                btnTakeCards.BackColor = btnTakeCards.Enabled ? Color.SteelBlue : Color.Gray;
                btnPass.BackColor = btnPass.Enabled ? Color.SeaGreen : Color.Gray;
                btnNothingToThrow.BackColor = btnNothingToThrow.Enabled ? Color.Goldenrod : Color.Gray;
                btnSurrender.BackColor = btnSurrender.Enabled ? Color.IndianRed : Color.Gray;
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateControlButtons: {ex.Message}");
            }
        }

        private void OnGameStateChanged()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(ForceUIUpdate));
                }
                else
                {
                    ForceUIUpdate();
                }
            }
        }

        private void OnGameMessage(string message)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action<string>(OnGameMessage), message);
                }
                else
                {
                    AddChatMessage("Игра", message);

                    if (lblCurrentTurn != null)
                    {
                        lblCurrentTurn.Text = message;
                        // Используем Timer для отображения временного сообщения
                        Timer messageTimer = new Timer();
                        messageTimer.Interval = 2000;
                        messageTimer.Tick += (s, e) =>
                        {
                            UpdateTurnIndicator();
                            messageTimer.Stop();
                            messageTimer.Dispose();
                        };
                        messageTimer.Start();
                    }
                }
            }
        }

        private Image CreateTrumpCardImage()
        {
            Bitmap bmp = new Bitmap(80, 120);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Black, 0, 0, 79, 119);

                string trumpSymbol = GetSuitSymbol(game.TrumpSuit);
                Color trumpColor = (game.TrumpSuit == Suit.Hearts || game.TrumpSuit == Suit.Diamonds) ?
                    Color.Red : Color.Black;

                using (Font symbolFont = new Font("Arial", 24, FontStyle.Bold))
                using (Brush brush = new SolidBrush(trumpColor))
                {
                    SizeF symbolSize = g.MeasureString(trumpSymbol, symbolFont);
                    g.DrawString(trumpSymbol, symbolFont, brush, 40 - symbolSize.Width / 2, 50 - symbolSize.Height / 2);
                }
            }
            return bmp;
        }

        private string GetSuitName(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "♥ Черви",
                Suit.Diamonds => "♦ Бубны",
                Suit.Clubs => "♣ Трефы",
                Suit.Spades => "♠ Пики",
                _ => "Неизвестно"
            };
        }

        private string GetSuitSymbol(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Очистка ресурсов
            if (game != null && game.IsGameActive)
            {
                game.IsGameActive = false;
            }
            base.OnFormClosed(e);
        }
    }
}