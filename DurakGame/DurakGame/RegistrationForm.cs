using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DurakGame
{
    public partial class RegistrationForm : Form
    {
        private PlayerManager playerManager;
        private Player currentPlayer;
        private Player restoredPlayer;

        public RegistrationForm(Player playerToRestore = null)
        {
            InitializeComponent();
            playerManager = PlayerManager.Instance;
            restoredPlayer = playerToRestore;

            if (restoredPlayer != null)
            {
                currentPlayer = restoredPlayer;
                UpdateCurrentPlayerLabel();
            }

            playerManager.DebugPrintPlayers();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "🎮 Дурак - Регистрация";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightBlue;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            CreateTitle();
            CreateRegistrationPanel();
            CreateLoginPanel();
            CreateGameModesPanel();

            if (restoredPlayer != null)
            {
                // Автоматически обновляем интерфейс для восстановленного игрока
                UpdateCurrentPlayerLabel();
            }
        }

        private void CreateTitle()
        {
            var lblTitle = new Label
            {
                Text = "🎮 КАРТОЧНАЯ ИГРА \"ДУРАК\"",
                Location = new Point(150, 20),
                Size = new Size(600, 40),
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);
        }



        private void CreateRegistrationPanel()
        {
            var panel = new Panel
            {
                Location = new Point(50, 80),
                Size = new Size(250, 180),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var lblRegister = new Label
            {
                Text = "📝 РЕГИСТРАЦИЯ",
                Location = new Point(10, 10),
                Size = new Size(230, 25),
                Font = new Font("Arial", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblName = new Label { Text = "Имя:", Location = new Point(10, 50), Font = new Font("Arial", 9, FontStyle.Bold) };
            var txtName = new TextBox { Location = new Point(110, 48), Size = new Size(120, 20), Name = "txtRegName" };

            var lblPassword = new Label { Text = "Пароль:", Location = new Point(10, 80), Font = new Font("Arial", 9, FontStyle.Bold) };
            var txtPassword = new TextBox { Location = new Point(110, 78), Size = new Size(120, 20), PasswordChar = '*', Name = "txtRegPassword" };

            var btnRegister = new Button
            {
                Text = "Зарегистрироваться",
                Location = new Point(25, 120),
                Size = new Size(200, 30),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Name = "btnRegister"
            };
            btnRegister.Click += BtnRegister_Click;

            panel.Controls.AddRange(new Control[] { lblRegister, lblName, txtName, lblPassword, txtPassword, btnRegister });
            this.Controls.Add(panel);
        }

        private void CreateLoginPanel()
        {
            var panel = new Panel
            {
                Location = new Point(50, 280),
                Size = new Size(250, 150),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var lblLogin = new Label
            {
                Text = "🔐 ВХОД",
                Location = new Point(10, 10),
                Size = new Size(230, 25),
                Font = new Font("Arial", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblLoginName = new Label { Text = "Имя:", Location = new Point(10, 50), Font = new Font("Arial", 9, FontStyle.Bold) };
            var txtLoginName = new TextBox { Location = new Point(110, 48), Size = new Size(120, 20), Name = "txtLoginName" };

            var lblLoginPassword = new Label { Text = "Пароль:", Location = new Point(10, 80), Font = new Font("Arial", 9, FontStyle.Bold) };
            var txtLoginPassword = new TextBox { Location = new Point(110, 78), Size = new Size(120, 20), PasswordChar = '*', Name = "txtLoginPassword" };

            var btnLogin = new Button
            {
                Text = "Войти",
                Location = new Point(25, 110),
                Size = new Size(200, 30),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Name = "btnLogin"
            };
            btnLogin.Click += BtnLogin_Click;

            var btnDebug = new Button
            {
                Text = "Debug",
                Location = new Point(25, 155),
                Size = new Size(200, 20),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Arial", 7, FontStyle.Bold)
            };
            btnDebug.Click += (s, e) => playerManager.DebugPrintPlayers();

            panel.Controls.AddRange(new Control[] { lblLogin, lblLoginName, txtLoginName, lblLoginPassword, txtLoginPassword, btnLogin, btnDebug });
            this.Controls.Add(panel);
        }

        private void CreateGameModesPanel()
        {
            var panel = new Panel
            {
                Location = new Point(320, 80),
                Size = new Size(500, 550),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var lblModes = new Label
            {
                Text = "🎯 ВЫБОР РЕЖИМА ИГРЫ",
                Location = new Point(10, 10),
                Size = new Size(480, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Информация о текущем игроке
            var lblCurrentPlayer = new Label
            {
                Name = "lblCurrentPlayer",
                Text = "❌ Не авторизован",
                Location = new Point(50, 50),
                Size = new Size(400, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Кнопки режимов игры - ТОЛЬКО КОМПЬЮТЕР И ONLINE
            var btnVsComputer = new Button
            {
                Text = "💻 ИГРА С КОМПЬЮТЕРОМ",
                Location = new Point(50, 90),
                Size = new Size(400, 60),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Name = "btnVsComputer"
            };
            btnVsComputer.Click += BtnVsComputer_Click;

            var btnOnlineGame = new Button
            {
                Text = "🌐 Простая сетевая игра",
                Location = new Point(200, 340),
                Size = new Size(400, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.Purple,
                ForeColor = Color.White
            };
            btnOnlineGame.Click += (s, e) =>
            {
                SoundManager.PlayButtonClick();
                this.Hide();
                var onlineForm = new SimpleOnlineLobbyForm(currentPlayer, this);
                onlineForm.Show();
            };

            var btnLoadGame = new Button
            {
                Text = "💾 ЗАГРУЗИТЬ ИГРУ",
                Location = new Point(50, 500),
                Size = new Size(400, 50),
                BackColor = Color.Purple,
                ForeColor = Color.White,
                Font = new Font("Arial", 11, FontStyle.Bold),
                Name = "btnLoadGame"
            };
            btnLoadGame.Click += BtnLoadGame_Click;

            // Рейтинг игроков
            var btnRating = new Button
            {
                Text = "🏆 РЕЙТИНГ ИГРОКОВ",
                Location = new Point(50, 230),
                Size = new Size(400, 50),
                BackColor = Color.Gold,
                ForeColor = Color.Black,
                Font = new Font("Arial", 11, FontStyle.Bold),
                Name = "btnRating"
            };
            btnRating.Click += BtnRating_Click;

            var listRating = new ListBox
            {
                Location = new Point(50, 290),
                Size = new Size(400, 220),
                Font = new Font("Arial", 9),
                Name = "listRating"
            };

            panel.Controls.AddRange(new Control[] {
        lblModes, lblCurrentPlayer, btnVsComputer,  btnOnlineGame,
        btnRating, listRating, btnLoadGame
    });
            this.Controls.Add(panel);
        }

        private void BtnVsComputer_Click(object sender, EventArgs e)
        {
            if (!CheckAuthorization()) return;

            // Сразу запускаем игру с компьютером в подкидном режиме
            var gameForm = new MainGameForm(currentPlayer, new Player("Компьютер", "", false), GameMode.ThrowIn, false);
            gameForm.Show();
            this.Hide();
            gameForm.FormClosed += (s, e) => this.Show();
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            var txtName = this.Controls.Find("txtRegName", true).FirstOrDefault() as TextBox;
            var txtPassword = this.Controls.Find("txtRegPassword", true).FirstOrDefault() as TextBox;

            if (txtName == null || txtPassword == null) return;

            string name = txtName.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите имя и пароль", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (name.Length < 2)
            {
                MessageBox.Show("Имя должно содержать至少 2 символа", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var player = playerManager.RegisterPlayer(name, password);
            if (player == null)
            {
                MessageBox.Show("Игрок с таким именем уже существует", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // АВТОМАТИЧЕСКИЙ ВХОД ПОСЛЕ РЕГИСТРАЦИИ
            currentPlayer = playerManager.LoginPlayer(name, password);
            if (currentPlayer != null)
            {
                UpdateCurrentPlayerLabel();
                MessageBox.Show($"🎉 Регистрация успешна! Добро пожаловать, {player.Name}!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Очистка полей
                txtName.Text = "";
                txtPassword.Text = "";
            }
            else
            {
                MessageBox.Show("Ошибка автоматического входа после регистрации", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadGame_Click(object sender, EventArgs e)
        {
            if (!CheckAuthorization()) return;

            var loaderForm = new GameLoaderForm(currentPlayer);
            loaderForm.ShowDialog();
        }


        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var txtName = this.Controls.Find("txtLoginName", true).FirstOrDefault() as TextBox;
            var txtPassword = this.Controls.Find("txtLoginPassword", true).FirstOrDefault() as TextBox;

            if (txtName == null || txtPassword == null) return;

            string name = txtName.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите имя и пароль", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Отладочная информация
            Console.WriteLine($"Попытка входа: Имя='{name}', Пароль='{password}'");

            var player = playerManager.LoginPlayer(name, password);
            if (player == null)
            {
                MessageBox.Show("Неверное имя или пароль", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Показываем отладочную информацию
                playerManager.DebugPrintPlayers();
                return;
            }

            currentPlayer = player;
            UpdateCurrentPlayerLabel();
            MessageBox.Show($"🎉 Добро пожаловать, {player.Name}!", "Успешный вход",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Очистка полей
            txtName.Text = "";
            txtPassword.Text = "";
        }

        private void BtnOnlinePvP_Click(object sender, EventArgs e)
        {
            if (!CheckAuthorization()) return;

            // Простое сетевое лобби
            var lobbyForm = new SimpleOnlineLobbyForm(currentPlayer, this);
            lobbyForm.Show();
            this.Hide();
            lobbyForm.FormClosed += (s, e) => this.Show();
        }

        private void BtnRating_Click(object sender, EventArgs e)
        {
            var listRating = this.Controls.Find("listRating", true).FirstOrDefault() as ListBox;
            if (listRating != null)
            {
                UpdateRatingList(listRating);
            }
        }

        private bool CheckAuthorization()
        {
            if (currentPlayer == null)
            {
                MessageBox.Show("Сначала войдите в аккаунт", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void UpdateCurrentPlayerLabel()
        {
            var label = this.Controls.Find("lblCurrentPlayer", true).FirstOrDefault() as Label;
            if (label != null && currentPlayer != null)
            {
                label.Text = $"✅ Авторизован: {currentPlayer.Name} (Рейтинг: {currentPlayer.WinRate:F1}%)";
                label.ForeColor = Color.Green;
            }
        }

        private void UpdateRatingList(ListBox listBox)
        {
            listBox.Items.Clear();

            // Только зарегистрированные игроки (исключаем компьютер)
            var registeredPlayers = playerManager.GetRegisteredPlayers()
                .Where(p => p.GamesPlayed > 0) // Только те, кто играл
                .OrderByDescending(p => p.WinRate)
                .ThenByDescending(p => p.Wins)
                .ThenBy(p => p.GamesPlayed)
                .ToList();

            listBox.Items.Add("🏆 РЕЙТИНГ ЗАРЕГИСТРИРОВАННЫХ ИГРОКОВ 🏆");
            listBox.Items.Add("==========================================");

            if (registeredPlayers.Count == 0)
            {
                listBox.Items.Add("Пока нет статистики игроков");
                listBox.Items.Add("Сыграйте несколько игр для появления рейтинга");
                return;
            }

            int position = 1;
            foreach (var player in registeredPlayers)
            {
                string status = player.IsOnline ? "🟢 ONLINE" : "⚫ OFFLINE";
                listBox.Items.Add($"{position}. {player.Name} {status}");
                listBox.Items.Add($"   Побед: {player.Wins} | Игр: {player.GamesPlayed} | Рейтинг: {player.WinRate:F1}%");
                listBox.Items.Add("");
                position++;
            }
        }
    }
}