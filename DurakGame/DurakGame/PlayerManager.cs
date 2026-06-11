using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DurakGame
{
    public class PlayerManager
    {
        private static readonly string PlayersFilePath = "players.json";
        private List<Player> players;
        private static PlayerManager instance;

        public static PlayerManager Instance => instance ??= new PlayerManager();

        private PlayerManager()
        {
            players = LoadPlayers();
            AddDemoPlayers(); // Теперь вызывается внутри конструктора
        }

        public List<Player> GetPlayers() => players;

        public Player RegisterPlayer(string name, string password)
        {
            // Проверяем существование игрока (без учета регистра)
            var existingPlayer = players.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingPlayer != null)
            {
                // Если игрок уже существует, проверяем можно ли обновить
                if (!string.IsNullOrEmpty(existingPlayer.Password))
                {
                    return null; // Игрок уже зарегистрирован с паролем
                }
                else
                {
                    // Игрок существует но без пароля - обновляем пароль
                    existingPlayer.Password = password;
                    SavePlayers();
                    return existingPlayer;
                }
            }

            // Создаем нового игрока
            var player = new Player(name, password);
            players.Add(player);
            SavePlayers();
            return player;
        }

        public Player LoginPlayer(string name, string password)
        {
            // Ищем игрока (без учета регистра)
            var player = players.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (player != null)
            {
                // Проверяем пароль
                if (player.CheckPassword(password))
                {
                    player.IsOnline = true;
                    SavePlayers();
                    return player;
                }
            }

            return null;
        }

        public void LogoutPlayer(string name)
        {
            var player = players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                player.IsOnline = false;
                SavePlayers();
            }
        }

        public void UpdatePlayerStats(Player player)
        {
            var existingPlayer = players.FirstOrDefault(p =>
                p.Name.Equals(player.Name, StringComparison.OrdinalIgnoreCase));

            if (existingPlayer != null)
            {
                existingPlayer.Wins = player.Wins;
                existingPlayer.GamesPlayed = player.GamesPlayed;
                SavePlayers();
            }
        }


        private List<Player> LoadPlayers()
        {
            try
            {
                if (File.Exists(PlayersFilePath))
                {
                    var json = File.ReadAllText(PlayersFilePath);
                    var loadedPlayers = JsonSerializer.Deserialize<List<Player>>(json) ?? new List<Player>();

                    // Восстанавливаем Hand для всех игроков
                    foreach (var player in loadedPlayers)
                    {
                        if (player.Hand == null)
                            player.Hand = new List<Card>();
                    }

                    return loadedPlayers;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки игроков: {ex.Message}");
                // Создаем новый файл при ошибке
                try { File.Delete(PlayersFilePath); } catch { }
            }
            return new List<Player>();
        }

        private void SavePlayers()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(players, options);

                // Создаем директорию если не существует
                var directory = Path.GetDirectoryName(PlayersFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(PlayersFilePath, json);

                Console.WriteLine($"✅ Игроки сохранены: {players.Count} записей");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения игроков: {ex.Message}");
            }
        }

        // Сделаем метод приватным, так как он вызывается только из конструктора
        private void AddDemoPlayers()
        {
            // Добавляем компьютерного игрока только если его нет
            if (!players.Any(p => p.Name == "Компьютер"))
            {
                players.Add(new Player("Компьютер", "", false) { Wins = 15, GamesPlayed = 30 });
                SavePlayers();
            }

            // Добавляем тестовых игроков для отладки
            AddTestPlayers();
        }

        private void AddTestPlayers()
        {
            // Тестовые игроки для отладки входа
            var testPlayers = new[]
            {
                new { Name = "test", Password = "test" },
                new { Name = "user", Password = "123" },
                new { Name = "admin", Password = "admin" }
            };

            foreach (var test in testPlayers)
            {
                if (!players.Any(p => p.Name.Equals(test.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    players.Add(new Player(test.Name, test.Password));
                    Console.WriteLine($"✅ Добавлен тестовый игрок: {test.Name}");
                }
            }

            if (testPlayers.Any(t => !players.Any(p => p.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase))))
            {
                SavePlayers();
            }
        }

        public List<Player> GetRegisteredPlayers()
        {
            return players.Where(p => p.Name != "Компьютер").ToList();
        }

        // Отладочный метод для просмотра всех игроков
        public void DebugPrintPlayers()
        {
            Console.WriteLine("=== ДЕБАГ: Список игроков ===");
            foreach (var player in players)
            {
                Console.WriteLine($"Имя: {player.Name}, Пароль: '{player.Password}', Игр: {player.GamesPlayed}");
            }
            Console.WriteLine("=============================");
        }

        // Публичный метод для принудительного добавления демо-игроков (если нужно)
        public void ForceAddDemoPlayers()
        {
            AddDemoPlayers();
        }
    }
}