using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DurakGame
{
    public partial class GameHistoryForm : Form
    {
        private List<string> historyFiles;
        private ListBox listBoxGames;
        private TextBox textBoxDetails;
        private Button btnClose;
        private Button btnLoadHistory;

        public GameHistoryForm(string[] files)
        {
            historyFiles = files.ToList();
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "📋 История игр";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.LightGray;

            var lblTitle = new Label
            {
                Text = "📊 ИСТОРИЯ СЫГРАННЫХ ПАРТИЙ",
                Location = new Point(20, 20),
                Size = new Size(750, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblGames = new Label
            {
                Text = "Список игр:",
                Location = new Point(20, 60),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            listBoxGames = new ListBox
            {
                Location = new Point(20, 85),
                Size = new Size(350, 400),
                Font = new Font("Consolas", 9)
            };
            listBoxGames.SelectedIndexChanged += ListBoxGames_SelectedIndexChanged;

            var lblDetails = new Label
            {
                Text = "Детали игры:",
                Location = new Point(400, 60),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            textBoxDetails = new TextBox
            {
                Location = new Point(400, 85),
                Size = new Size(350, 400),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };

            btnLoadHistory = new Button
            {
                Text = "🔄 Обновить историю",
                Location = new Point(20, 500),
                Size = new Size(150, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            btnLoadHistory.Click += BtnLoadHistory_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(600, 500),
                Size = new Size(150, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblTitle, lblGames, listBoxGames, lblDetails, textBoxDetails,
                btnLoadHistory, btnClose
            });

            LoadGameList();
        }

        private void LoadGameList()
        {
            listBoxGames.Items.Clear();
            foreach (var file in historyFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var displayName = fileName.Replace("GameHistory_", "Игра от ").Replace("_", " ");
                    listBoxGames.Items.Add(displayName);
                }
                catch
                {
                    listBoxGames.Items.Add($"Ошибка: {Path.GetFileName(file)}");
                }
            }
        }

        private void ListBoxGames_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxGames.SelectedIndex >= 0)
            {
                try
                {
                    var selectedFile = historyFiles[listBoxGames.SelectedIndex];
                    var history = LoadGameHistory(selectedFile);
                    DisplayGameDetails(history);
                }
                catch (Exception ex)
                {
                    textBoxDetails.Text = $"Ошибка загрузки истории: {ex.Message}";
                }
            }
        }

        private GameHistory LoadGameHistory(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GameHistory));
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    return (GameHistory)serializer.Deserialize(stream)!;
                }
            }
            catch
            {
                return new GameHistory();
            }
        }

        private void DisplayGameDetails(GameHistory history)
        {
            if (history == null || history.Moves.Count == 0)
            {
                textBoxDetails.Text = "История игры пуста или повреждена.";
                return;
            }

            var details = new System.Text.StringBuilder();
            details.AppendLine($"=== ИСТОРИЯ ИГРЫ ===");
            details.AppendLine($"Начало: {history.GameStart:dd.MM.yyyy HH:mm:ss}");
            details.AppendLine($"Окончание: {history.GameEnd?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Не завершена"}");
            details.AppendLine($"Длительность: {history.GetGameDuration()}");
            details.AppendLine($"Всего ходов: {history.Moves.Count}");
            details.AppendLine();
            details.AppendLine("=== ПОСЛЕДНИЕ ХОДЫ ===");

            var recentMoves = history.GetMoveHistory(15);
            foreach (var move in recentMoves)
            {
                details.AppendLine(move);
            }

            textBoxDetails.Text = details.ToString();
        }

        private void BtnLoadHistory_Click(object? sender, EventArgs e)
        {
            SoundManager.PlayButtonClick();
            historyFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "GameHistory_*.xml").ToList();
            LoadGameList();
            MessageBox.Show($"Загружено {historyFiles.Count} файлов истории.", "Обновление",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}