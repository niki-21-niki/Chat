using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static DurakGame.GameRepository;

namespace DurakGame
{
    public partial class GameLoaderForm : Form
    {
        private Player currentPlayer;
        private GameRepository gameRepository;
        private List<SavedGameData> savedGames;

        // ОБЪЯВЛЯЕМ ЭЛЕМЕНТЫ УПРАВЛЕНИЯ КАК ПОЛЯ КЛАССА
        private ListBox listSavedGames;
        private TextBox txtGameInfo;
        private Button btnLoad, btnDelete, btnBack;

        public GameLoaderForm(Player player)
        {
            InitializeComponent();
            currentPlayer = player;
            gameRepository = new GameRepository();

            try
            {
                SetupUI();
                LoadSavedGames();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации загрузчика: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Name = "GameLoaderForm";
            this.Text = "Загрузка сохраненных игр";
            this.ResumeLayout(false);
        }

        private void SetupUI()
        {
            this.Text = "💾 Загрузка сохраненных игр";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.LightBlue;

            var lblTitle = new Label
            {
                Text = "💾 ЗАГРУЗКА СОХРАНЕННЫХ ИГР",
                Location = new Point(20, 20),
                Size = new Size(650, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblGames = new Label
            {
                Text = "Сохраненные игры:",
                Location = new Point(20, 60),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // ИНИЦИАЛИЗИРУЕМ listSavedGames ПРАВИЛЬНО
            listSavedGames = new ListBox
            {
                Location = new Point(20, 85),
                Size = new Size(300, 300),
                Font = new Font("Arial", 9),
                Name = "listSavedGames" // ← добавляем имя
            };
            listSavedGames.SelectedIndexChanged += ListSavedGames_SelectedIndexChanged;

            var lblInfo = new Label
            {
                Text = "Информация об игре:",
                Location = new Point(350, 60),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            txtGameInfo = new TextBox
            {
                Location = new Point(350, 85),
                Size = new Size(300, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Arial", 9)
            };

            btnLoad = new Button
            {
                Text = "🎮 Загрузить игру",
                Location = new Point(350, 300),
                Size = new Size(150, 35),
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = false
            };
            btnLoad.Click += BtnLoad_Click;

            btnDelete = new Button
            {
                Text = "🗑️ Удалить игру",
                Location = new Point(520, 300),
                Size = new Size(130, 35),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnBack = new Button
            {
                Text = "← Назад",
                Location = new Point(20, 400),
                Size = new Size(150, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnBack.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
        lblTitle, lblGames, listSavedGames, lblInfo, txtGameInfo,
        btnLoad, btnDelete, btnBack
    });
        }

        private void LoadSavedGames()
        {
            // Используем правильное имя элемента управления - listSavedGames
            if (listSavedGames == null) return;

            listSavedGames.Items.Clear();

            try
            {
                savedGames = gameRepository.GetSavedGames(currentPlayer.Name, 20);

                if (!savedGames.Any())
                {
                    listSavedGames.Items.Add("Нет сохраненных игр");
                    return;
                }

                foreach (var game in savedGames)
                {
                    string status = game.IsActive ?
                        (game.EndTime.HasValue ? "🏁 Завершена" : "🎮 Активна") :
                        "⏸️ Приостановлена";

                    string displayText = $"{status} - {game.Player1Name} vs {game.Player2Name} - {game.StartTime:dd.MM.yy HH:mm}";

                    listSavedGames.Items.Add(new GameListItem
                    {
                        DisplayText = displayText,
                        GameData = game
                    });
                }
            }
            catch (Exception ex)
            {
                listSavedGames.Items.Add($"Ошибка загрузки: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки списка игр: {ex.Message}");
            }
        }


        private void ListSavedGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listSavedGames.SelectedItem is GameListItem item) //  используем listSavedGames
            {
                try
                {
                    var game = item.GameData;
                    txtGameInfo.Text = GetDetailedGameInfo(game);
                    btnLoad.Enabled = game.IsActive && !game.EndTime.HasValue;
                    btnDelete.Enabled = true;
                }
                catch (Exception ex)
                {
                    txtGameInfo.Text = $"Ошибка загрузки информации: {ex.Message}";
                    btnLoad.Enabled = false;
                    btnDelete.Enabled = false;
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (listSavedGames.SelectedItem is GameListItem item)
            {
                try
                {
                    var savedGame = item.GameData;

                    // Проверяем валидность данных
                    if (savedGame == null)
                    {
                        MessageBox.Show("Ошибка: выбранная игра не содержит данных.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Определяем оппонента
                    var isPlayer1 = savedGame.Player1Name == currentPlayer.Name;
                    var opponentName = isPlayer1 ? savedGame.Player2Name : savedGame.Player1Name;
                    var opponent = new Player(opponentName, "", opponentName == "Компьютер");

                    // Загружаем игру
                    var game = Game.LoadSavedGame(savedGame, currentPlayer, opponent);

                    if (game != null)
                    {
                        this.Hide();
                        var gameForm = new MainGameForm(currentPlayer, opponent, game.Mode);
                        gameForm.SetGameState(game);
                        gameForm.Show();
                        gameForm.FormClosed += (s, e) =>
                        {
                            this.Show();
                            LoadSavedGames(); // Обновляем список после возврата
                        };
                    }
                    else
                    {
                        MessageBox.Show("Не удалось загрузить игру. Файл может быть поврежден.",
                            "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки игры: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки: {ex.Message}");
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listSavedGames.SelectedItem is GameListItem item)
            {
                var result = MessageBox.Show("Удалить выбранную игру?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        if (gameRepository.DeleteGame(item.GameData.GameId))
                        {
                            LoadSavedGames();
                            txtGameInfo.Clear();
                            btnLoad.Enabled = false;
                            btnDelete.Enabled = false;

                            MessageBox.Show("Игра удалена", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string GetDetailedGameInfo(SavedGameData game)
        {
            return $@"🎮 Игра: {game.Player1Name} vs {game.Player2Name}
🎯 Режим: {game.GameMode}
📅 Начало: {game.StartTime:dd.MM.yyyy HH:mm}
⏱️ Последнее сохранение: {game.LastSaveTime:dd.MM.yyyy HH:mm}
🃏 Ходов: {game.History.Moves.Count}
📊 Статус: {game.GetStatusDescription()}

{(game.GameState != null ?
$@"Карты в игре:
• {game.Player1Name}: {game.GameState.Player1Hand.Count} карт
• {game.Player2Name}: {game.GameState.Player2Hand.Count} карт  
• Колода: {game.GameState.Deck.Count} карт
• На столе: {game.GameState.Table.Count} карт
• Козырь: {GetSuitName(game.GameState.TrumpSuit)}" :
"Состояние игры недоступно")}";
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

        private class GameListItem
        {
            public string DisplayText { get; set; }
            public SavedGameData GameData { get; set; }
            public override string ToString() => DisplayText;
        }
    }
}