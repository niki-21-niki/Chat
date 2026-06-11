using System;
using System.Drawing;
using System.Windows.Forms;

namespace DurakGame
{
    public partial class GameModeForm : Form
    {
        private Player humanPlayer;
        private Player opponent;
        private bool isLocalMultiplayer;

        public GameModeForm(Player humanPlayer, Player opponent, bool isLocalMultiplayer = false)
        {
            InitializeComponent(); // ДОБАВИТЬ эту строку
            this.humanPlayer = humanPlayer;
            this.opponent = opponent;
            this.isLocalMultiplayer = isLocalMultiplayer;
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "🎮 Выбор режима игры - Дурак";
            this.Size = new Size(500, 300); // Уменьшен размер формы
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightBlue;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            var lblTitle = new Label
            {
                Text = "🎯 ВЫБЕРИТЕ РЕЖИМ ИГРЫ",
                Location = new Point(50, 30),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var playerInfo = new Label
            {
                Text = $"Игрок: {humanPlayer.Name} (Рейтинг: {humanPlayer.WinRate:F1}%)",
                Location = new Point(50, 80),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Только кнопка подкидного дурака
            var btnThrowIn = new Button
            {
                Text = "🎯 ПОДКИДНОЙ ДУРАК\n(Классические правила)",
                Location = new Point(100, 140),
                Size = new Size(300, 60),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = GameMode.ThrowIn
            };
            btnThrowIn.FlatAppearance.BorderSize = 0;
            btnThrowIn.Click += GameModeSelected;

            var btnBack = new Button
            {
                Text = "← Назад в меню",
                Location = new Point(175, 210),
                Size = new Size(150, 40),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) =>
            {
                SoundManager.PlayButtonClick();
                this.Close();
                new RegistrationForm().Show();
            };

            this.Controls.AddRange(new Control[] { lblTitle, playerInfo, btnThrowIn, btnBack });
        }

        private void GameModeSelected(object sender, EventArgs e)
        {
            SoundManager.PlayButtonClick();
            var button = (Button)sender;
            var mode = (GameMode)button.Tag;

            var gameForm = new MainGameForm(humanPlayer, opponent, mode, isLocalMultiplayer);
            gameForm.Show();
            this.Hide();
            gameForm.FormClosed += (s, e) => this.Show();
        }

        private void GameModeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                SoundManager.PlayButtonClick();
                new RegistrationForm().Show();
            }
        }
    }
}