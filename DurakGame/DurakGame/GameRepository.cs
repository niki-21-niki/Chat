using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace DurakGame
{
    public class GameRepository
    {
        private string connectionString;
        private string databasePath = "durak_games.db";

        public GameRepository()
        {
            connectionString = $"Data Source={databasePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool isNewDatabase = !File.Exists(databasePath);

            if (isNewDatabase)
            {
                SQLiteConnection.CreateFile(databasePath);
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Улучшенная таблица игр
                string createGamesTable = @"
                CREATE TABLE IF NOT EXISTS Games (
                    Id TEXT PRIMARY KEY,
                    Player1Name TEXT NOT NULL,
                    Player2Name TEXT NOT NULL,
                    GameMode INTEGER NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME NULL,
                    Winner TEXT NULL,
                    GameState TEXT NULL,
                    LastSaveTime DATETIME NOT NULL,
                    IsActive BOOLEAN NOT NULL DEFAULT 1,
                    IsOnline BOOLEAN NOT NULL DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

                // Таблица ходов
                string createMovesTable = @"
                CREATE TABLE IF NOT EXISTS GameMoves (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameId TEXT NOT NULL,
                    MoveNumber INTEGER NOT NULL,
                    PlayerName TEXT NOT NULL,
                    MoveType INTEGER NOT NULL,
                    CardSuit INTEGER NULL,
                    CardRank INTEGER NULL,
                    Timestamp DATETIME NOT NULL,
                    Description TEXT NULL,
                    FOREIGN KEY (GameId) REFERENCES Games (Id)
                )";

                // Таблица статистики игроков
                string createPlayerStatsTable = @"
                CREATE TABLE IF NOT EXISTS PlayerStats (
                    PlayerName TEXT PRIMARY KEY,
                    TotalGames INTEGER DEFAULT 0,
                    Wins INTEGER DEFAULT 0,
                    Losses INTEGER DEFAULT 0,
                    Draws INTEGER DEFAULT 0,
                    LastPlayed DATETIME NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

                ExecuteNonQuery(connection, createGamesTable);
                ExecuteNonQuery(connection, createMovesTable);
                ExecuteNonQuery(connection, createPlayerStatsTable);

                // Для существующей базы добавляем недостающие колонки
                if (!isNewDatabase)
                {
                    AddMissingColumns(connection);
                }

                // Создаем индекс для быстрого поиска активных игр (только если колонка существует)
                try
                {
                    string createIndex = @"
                    CREATE INDEX IF NOT EXISTS idx_games_active 
                    ON Games(IsActive, LastSaveTime DESC)";
                    ExecuteNonQuery(connection, createIndex);
                }
                catch (System.Data.SQLite.SQLiteException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Не удалось создать индекс: {ex.Message}");
                    // Игнорируем ошибку индекса, так как колонка может отсутствовать
                }
            }
        }


        private void AddMissingColumns(SQLiteConnection connection)
        {
            try
            {
                // Проверяем существование колонок и добавляем их если нужно
                string[] columnsToAdd = {
                    "IsActive", "IsOnline", "LastSaveTime"
                };

                foreach (var column in columnsToAdd)
                {
                    if (!ColumnExists(connection, "Games", column))
                    {
                        string alterTable = $"ALTER TABLE Games ADD COLUMN {column} ";

                        if (column == "IsActive" || column == "IsOnline")
                        {
                            alterTable += "BOOLEAN NOT NULL DEFAULT 1";
                        }
                        else if (column == "LastSaveTime")
                        {
                            alterTable += "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP";
                        }

                        ExecuteNonQuery(connection, alterTable);
                        System.Diagnostics.Debug.WriteLine($"✅ Добавлена колонка {column} в таблицу Games");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка добавления колонок: {ex.Message}");
            }
        }

        private bool ColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            try
            {
                string checkColumnSql = @"
                    SELECT COUNT(*) FROM pragma_table_info(@TableName) 
                    WHERE name = @ColumnName";

                using (var command = new SQLiteCommand(checkColumnSql, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);
                    command.Parameters.AddWithValue("@ColumnName", columnName);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            try
            {
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (System.Data.SQLite.SQLiteException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SQLite ошибка в {sql}: {ex.Message}");

                // Если ошибка связана с отсутствием таблицы или колонки, пересоздаем БД
                if (ex.Message.Contains("no such table") || ex.Message.Contains("no such column"))
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Попытка пересоздания базы данных из ExecuteNonQuery...");

                    // Закрываем текущее соединение перед пересозданием
                    connection?.Close();

                    RecreateDatabase();

                    // Открываем новое соединение и повторяем запрос
                    using (var newConnection = new SQLiteConnection(connectionString))
                    {
                        newConnection.Open();
                        using (var retryCommand = new SQLiteCommand(sql, newConnection))
                        {
                            retryCommand.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Общая ошибка в {sql}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка при очистке пула соединений: {ex.Message}");
            }
        }

        private void RecreateDatabase()
        {
            int maxRetries = 3;
            int retryDelay = 1000; // 1 секунда

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 Попытка {attempt} пересоздания базы данных...");

                    // Закрываем все соединения
                    System.Data.SQLite.SQLiteConnection.ClearAllPools();

                    // Даем время на освобождение файла
                    System.Threading.Thread.Sleep(retryDelay);

                    if (File.Exists(databasePath))
                    {
                        // Пробуем удалить файл с несколькими попытками
                        for (int fileAttempt = 1; fileAttempt <= 3; fileAttempt++)
                        {
                            try
                            {
                                File.Delete(databasePath);
                                System.Diagnostics.Debug.WriteLine("🗑️ Старая база данных удалена");
                                break;
                            }
                            catch (IOException ioEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Попытка {fileAttempt}: Файл занят, ждем... {ioEx.Message}");
                                if (fileAttempt == 3) throw;
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                    }

                    // Создаем новую базу
                    SQLiteConnection.CreateFile(databasePath);
                    System.Diagnostics.Debug.WriteLine("✅ Новая база данных создана");

                    // Переинициализируем структуру
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        string createGamesTable = @"
                CREATE TABLE Games (
                    Id TEXT PRIMARY KEY,
                    Player1Name TEXT NOT NULL,
                    Player2Name TEXT NOT NULL,
                    GameMode INTEGER NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME NULL,
                    Winner TEXT NULL,
                    GameState TEXT NULL,
                    LastSaveTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    IsActive BOOLEAN NOT NULL DEFAULT 1,
                    IsOnline BOOLEAN NOT NULL DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

                        string createMovesTable = @"
                CREATE TABLE GameMoves (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GameId TEXT NOT NULL,
                    MoveNumber INTEGER NOT NULL,
                    PlayerName TEXT NOT NULL,
                    MoveType INTEGER NOT NULL,
                    CardSuit INTEGER NULL,
                    CardRank INTEGER NULL,
                    Timestamp DATETIME NOT NULL,
                    Description TEXT NULL,
                    FOREIGN KEY (GameId) REFERENCES Games (Id)
                )";

                        string createPlayerStatsTable = @"
                CREATE TABLE PlayerStats (
                    PlayerName TEXT PRIMARY KEY,
                    TotalGames INTEGER DEFAULT 0,
                    Wins INTEGER DEFAULT 0,
                    Losses INTEGER DEFAULT 0,
                    Draws INTEGER DEFAULT 0,
                    LastPlayed DATETIME NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

                        using (var command = new SQLiteCommand(createGamesTable, connection))
                            command.ExecuteNonQuery();

                        using (var command = new SQLiteCommand(createMovesTable, connection))
                            command.ExecuteNonQuery();

                        using (var command = new SQLiteCommand(createPlayerStatsTable, connection))
                            command.ExecuteNonQuery();

                        // Создаем индекс
                        string createIndex = @"
                CREATE INDEX idx_games_active 
                ON Games(IsActive, LastSaveTime DESC)";

                        using (var command = new SQLiteCommand(createIndex, connection))
                            command.ExecuteNonQuery();

                        System.Diagnostics.Debug.WriteLine("✅ Структура базы данных пересоздана");
                    }

                    // Успешно завершили пересоздание
                    return;
                }
                catch (IOException ioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Попытка {attempt}: Файл все еще занят - {ioEx.Message}");

                    if (attempt == maxRetries)
                    {
                        // Последняя попытка не удалась - используем альтернативный подход
                        HandleDatabaseLockAlternative();
                        return;
                    }

                    // Увеличиваем задержку перед следующей попыткой
                    System.Threading.Thread.Sleep(retryDelay * attempt);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Попытка {attempt}: Ошибка пересоздания БД - {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        HandleDatabaseLockAlternative();
                        return;
                    }
                }
            }
        }

        private void HandleDatabaseLockAlternative()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Используем альтернативный подход...");

                // Создаем временную базу данных
                string tempDatabasePath = Path.Combine(
                    Path.GetDirectoryName(databasePath),
                    "temp_" + DateTime.Now.Ticks + "_" + Path.GetFileName(databasePath)
                );

                // Обновляем путь к базе данных
                databasePath = tempDatabasePath;
                connectionString = $"Data Source={databasePath};Version=3;";

                System.Diagnostics.Debug.WriteLine($"✅ Создана временная база: {databasePath}");

                // Инициализируем новую базу
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Критическая ошибка создания временной БД: {ex.Message}");
                throw new InvalidOperationException("Не удалось создать базу данных. Закройте все экземпляры приложения и попробуйте снова.", ex);
            }
        }

        public void SaveGame(GameHistory history, GameState gameState = null)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Используем более безопасный подход с проверкой колонок
                    string saveGame = @"
                        INSERT OR REPLACE INTO Games 
                        (Id, Player1Name, Player2Name, GameMode, StartTime, EndTime, Winner, GameState, LastSaveTime, IsActive, IsOnline)
                        VALUES (@Id, @Player1, @Player2, @Mode, @Start, @End, @Winner, @GameState, @LastSaveTime, @IsActive, @IsOnline)";

                    using (var command = new SQLiteCommand(saveGame, connection))
                    {
                        command.Parameters.AddWithValue("@Id", history.GameId);
                        command.Parameters.AddWithValue("@Player1", history.Player1Name);
                        command.Parameters.AddWithValue("@Player2", history.Player2Name);
                        command.Parameters.AddWithValue("@Mode", (int)history.GameMode);
                        command.Parameters.AddWithValue("@Start", history.GameStart);
                        command.Parameters.AddWithValue("@End", history.GameEnd ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Winner", history.Winner ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GameState", gameState?.Serialize() ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LastSaveTime", DateTime.Now);
                        command.Parameters.AddWithValue("@IsActive", gameState?.IsGameActive ?? !history.GameEnd.HasValue);
                        command.Parameters.AddWithValue("@IsOnline", false);

                        command.ExecuteNonQuery();
                    }

                    // Сохраняем ходы
                    SaveGameMoves(connection, history);

                    // Обновляем статистику игроков
                    UpdatePlayerStats(history);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Игра сохранена: {history.GameId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка сохранения игры: {ex.Message}");

                // При ошибке пробуем пересоздать БД и сохранить заново
                if (ex.Message.Contains("no such column"))
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Попытка восстановления после ошибки колонки...");
                    RecreateDatabase();
                    SaveGame(history, gameState); // Рекурсивный вызов
                }
                else
                {
                    throw;
                }
            }
        }

        // Остальные методы остаются без измене

        private void UpdatePlayerStats(GameHistory history)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    foreach (var playerName in new[] { history.Player1Name, history.Player2Name })
                    {
                        if (string.IsNullOrEmpty(playerName)) continue;

                        string getStats = "SELECT * FROM PlayerStats WHERE PlayerName = @Player";
                        int totalGames = 0, wins = 0, losses = 0, draws = 0;

                        using (var command = new SQLiteCommand(getStats, connection))
                        {
                            command.Parameters.AddWithValue("@Player", playerName);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    totalGames = reader.GetInt32(1);
                                    wins = reader.GetInt32(2);
                                    losses = reader.GetInt32(3);
                                    draws = reader.GetInt32(4);
                                }
                            }
                        }

                        totalGames++;

                        if (history.Winner == playerName)
                            wins++;
                        else if (!string.IsNullOrEmpty(history.Winner) && history.Winner != "Ничья")
                            losses++;
                        else if (history.Winner == "Ничья")
                            draws++;

                        string updateStats = @"
                            INSERT OR REPLACE INTO PlayerStats 
                            (PlayerName, TotalGames, Wins, Losses, Draws, LastPlayed)
                            VALUES (@Player, @Total, @Wins, @Losses, @Draws, @LastPlayed)";

                        using (var command = new SQLiteCommand(updateStats, connection))
                        {
                            command.Parameters.AddWithValue("@Player", playerName);
                            command.Parameters.AddWithValue("@Total", totalGames);
                            command.Parameters.AddWithValue("@Wins", wins);
                            command.Parameters.AddWithValue("@Losses", losses);
                            command.Parameters.AddWithValue("@Draws", draws);
                            command.Parameters.AddWithValue("@LastPlayed", DateTime.Now);

                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        private void SaveGameMoves(SQLiteConnection connection, GameHistory history)
        {
            // Очищаем старые ходы
            string clearMoves = "DELETE FROM GameMoves WHERE GameId = @GameId";
            using (var command = new SQLiteCommand(clearMoves, connection))
            {
                command.Parameters.AddWithValue("@GameId", history.GameId);
                command.ExecuteNonQuery();
            }

            // Сохраняем новые ходы
            string saveMove = @"
                INSERT INTO GameMoves 
                (GameId, MoveNumber, PlayerName, MoveType, CardSuit, CardRank, Timestamp, Description)
                VALUES (@GameId, @MoveNumber, @Player, @MoveType, @Suit, @Rank, @Timestamp, @Description)";

            foreach (var move in history.Moves)
            {
                using (var command = new SQLiteCommand(saveMove, connection))
                {
                    command.Parameters.AddWithValue("@GameId", history.GameId);
                    command.Parameters.AddWithValue("@MoveNumber", move.MoveNumber);
                    command.Parameters.AddWithValue("@Player", move.PlayerName);
                    command.Parameters.AddWithValue("@MoveType", (int)move.MoveType);
                    command.Parameters.AddWithValue("@Suit", move.Card?.Suit ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Rank", move.Card?.Rank ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Timestamp", move.Timestamp);
                    command.Parameters.AddWithValue("@Description", move.Description ?? "");

                    command.ExecuteNonQuery();
                }
            }
        }

        public SavedGameData LoadFullGame(string gameId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Загружаем основную информацию об игре
                    var gameData = LoadGameData(connection, gameId);
                    if (gameData == null) return null;

                    // Загружаем ходы
                    gameData.History.Moves = LoadGameMoves(connection, gameId);

                    return gameData;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки игры {gameId}: {ex.Message}");
                return null;
            }
        }

        private SavedGameData LoadGameData(SQLiteConnection connection, string gameId)
        {
            string gameQuery = @"
                SELECT Id, Player1Name, Player2Name, GameMode, StartTime, EndTime, Winner, GameState, IsActive
                FROM Games WHERE Id = @GameId";

            using (var command = new SQLiteCommand(gameQuery, connection))
            {
                command.Parameters.AddWithValue("@GameId", gameId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var gameStateData = reader.IsDBNull(7) ? null : reader.GetString(7);
                        var gameState = string.IsNullOrEmpty(gameStateData) ?
                            null : GameState.Deserialize(gameStateData);

                        return new SavedGameData
                        {
                            GameId = reader.GetString(0),
                            Player1Name = reader.GetString(1),
                            Player2Name = reader.GetString(2),
                            GameMode = (GameMode)reader.GetInt32(3),
                            StartTime = reader.GetDateTime(4),
                            EndTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                            Winner = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            GameState = gameState,
                            IsActive = reader.GetBoolean(8)
                        };
                    }
                }
            }
            return null;
        }

        private List<MoveRecord> LoadGameMoves(SQLiteConnection connection, string gameId)
        {
            var moves = new List<MoveRecord>();

            string movesQuery = @"
                SELECT MoveNumber, PlayerName, MoveType, CardSuit, CardRank, Timestamp, Description
                FROM GameMoves WHERE GameId = @GameId ORDER BY MoveNumber";

            using (var command = new SQLiteCommand(movesQuery, connection))
            {
                command.Parameters.AddWithValue("@GameId", gameId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var move = new MoveRecord
                        {
                            MoveNumber = reader.GetInt32(0),
                            PlayerName = reader.GetString(1),
                            MoveType = (MoveType)reader.GetInt32(2),
                            Timestamp = reader.GetDateTime(5),
                            Description = reader.GetString(6)
                        };

                        // Восстанавливаем карту если есть
                        if (!reader.IsDBNull(3) && !reader.IsDBNull(4))
                        {
                            move.Card = new Card(
                                (Suit)reader.GetInt32(3),
                                (Rank)reader.GetInt32(4)
                            );
                        }

                        moves.Add(move);
                    }
                }
            }
            return moves;
        }

        public List<SavedGameData> GetSavedGames(string playerName = null, int limit = 50)
        {
            var games = new List<SavedGameData>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT Id, Player1Name, Player2Name, GameMode, StartTime, EndTime, Winner, GameState, IsActive, LastSaveTime
                        FROM Games 
                        WHERE (@PlayerName IS NULL OR Player1Name = @PlayerName OR Player2Name = @PlayerName)
                        ORDER BY LastSaveTime DESC 
                        LIMIT @Limit";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlayerName",
                            string.IsNullOrEmpty(playerName) ? (object)DBNull.Value : playerName);
                        command.Parameters.AddWithValue("@Limit", limit);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var gameStateData = reader.IsDBNull(7) ? null : reader.GetString(7);
                                var gameState = string.IsNullOrEmpty(gameStateData) ?
                                    null : GameState.Deserialize(gameStateData);

                                games.Add(new SavedGameData
                                {
                                    GameId = reader.GetString(0),
                                    Player1Name = reader.GetString(1),
                                    Player2Name = reader.GetString(2),
                                    GameMode = (GameMode)reader.GetInt32(3),
                                    StartTime = reader.GetDateTime(4),
                                    EndTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                    Winner = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    GameState = gameState,
                                    IsActive = reader.GetBoolean(8),
                                    LastSaveTime = reader.GetDateTime(9)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки списка игр: {ex.Message}");
            }

            return games;
        }

        public bool DeleteGame(string gameId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем ходы
                            string deleteMoves = "DELETE FROM GameMoves WHERE GameId = @GameId";
                            using (var command = new SQLiteCommand(deleteMoves, connection))
                            {
                                command.Parameters.AddWithValue("@GameId", gameId);
                                command.ExecuteNonQuery();
                            }

                            // Удаляем игру
                            string deleteGame = "DELETE FROM Games WHERE Id = @GameId";
                            using (var command = new SQLiteCommand(deleteGame, connection))
                            {
                                command.Parameters.AddWithValue("@GameId", gameId);
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка удаления игры {gameId}: {ex.Message}");
                return false;
            }
        }

        public class SavedGameData
        {
            public string GameId { get; set; }
            public string Player1Name { get; set; }
            public string Player2Name { get; set; }
            public GameMode GameMode { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string Winner { get; set; }
            public GameState GameState { get; set; }
            public bool IsActive { get; set; }
            public DateTime LastSaveTime { get; set; }
            public GameHistory History { get; set; } = new GameHistory();

            public string GetStatusDescription()
            {
                if (EndTime.HasValue)
                    return $"🏁 Завершена - {Winner}";
                else if (IsActive)
                    return $"🎮 Активна - можно продолжить";
                else
                    return $"⏸️ Приостановлена";
            }

            public string GetGameInfo()
            {
                return $"{Player1Name} vs {Player2Name} - {GameMode}";
            }
        }


        public List<GameHistory> GetPlayerGames(string playerName, int limit = 50)
        {
            var games = new List<GameHistory>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT * FROM Games 
                        WHERE Player1Name = @Player OR Player2Name = @Player
                        ORDER BY StartTime DESC 
                        LIMIT @Limit";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Player", playerName);
                        command.Parameters.AddWithValue("@Limit", limit);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var history = new GameHistory
                                {
                                    GameId = reader.GetString(0),
                                    Player1Name = reader.GetString(1),
                                    Player2Name = reader.GetString(2),
                                    GameMode = (GameMode)reader.GetInt32(3),
                                    GameStart = reader.GetDateTime(4),
                                    GameEnd = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                    Winner = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                };

                                games.Add(history);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки игр: {ex.Message}");
            }

            return games;
        }

        public GameHistory LoadGame(string gameId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Загружаем основную информацию об игре
                    string gameQuery = "SELECT * FROM Games WHERE Id = @GameId";
                    using (var gameCommand = new SQLiteCommand(gameQuery, connection))
                    {
                        gameCommand.Parameters.AddWithValue("@GameId", gameId);

                        using (var gameReader = gameCommand.ExecuteReader())
                        {
                            if (gameReader.Read())
                            {
                                var history = new GameHistory
                                {
                                    GameId = gameReader.GetString(0),
                                    Player1Name = gameReader.GetString(1),
                                    Player2Name = gameReader.GetString(2),
                                    GameMode = (GameMode)gameReader.GetInt32(3),
                                    GameStart = gameReader.GetDateTime(4),
                                    GameEnd = gameReader.IsDBNull(5) ? null : gameReader.GetDateTime(5),
                                    Winner = gameReader.IsDBNull(6) ? "" : gameReader.GetString(6)
                                };

                                // Загружаем ходы
                                string movesQuery = "SELECT * FROM GameMoves WHERE GameId = @GameId ORDER BY MoveNumber";
                                using (var movesCommand = new SQLiteCommand(movesQuery, connection))
                                {
                                    movesCommand.Parameters.AddWithValue("@GameId", gameId);

                                    using (var movesReader = movesCommand.ExecuteReader())
                                    {
                                        while (movesReader.Read())
                                        {
                                            var move = new MoveRecord
                                            {
                                                PlayerName = movesReader.GetString(2),
                                                MoveType = (MoveType)movesReader.GetInt32(3),
                                                Timestamp = movesReader.GetDateTime(6),
                                                Description = movesReader.GetString(7),
                                                MoveNumber = movesReader.GetInt32(1)
                                            };

                                            // Восстанавливаем карту если есть
                                            if (!movesReader.IsDBNull(4) && !movesReader.IsDBNull(5))
                                            {
                                                move.Card = new Card(
                                                    (Suit)movesReader.GetInt32(4),
                                                    (Rank)movesReader.GetInt32(5)
                                                );
                                            }

                                            history.Moves.Add(move);
                                        }
                                    }
                                }

                                return history;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки игры: {ex.Message}");
            }

            return null;
        }


        public GameStatistics GetPlayerStatistics(string playerName)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM PlayerStats WHERE PlayerName = @Player";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Player", playerName);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var totalGames = reader.GetInt32(1);
                                var wins = reader.GetInt32(2);

                                return new GameStatistics
                                {
                                    PlayerName = reader.GetString(0),
                                    TotalGames = totalGames,
                                    Wins = wins,
                                    Losses = reader.GetInt32(3),
                                    Draws = reader.GetInt32(4),
                                    LastPlayed = reader.GetDateTime(5),
                                    WinRate = totalGames > 0 ? (double)wins / totalGames * 100 : 0
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }

            return new GameStatistics(playerName);
        }

        public List<GameStatistics> GetAllPlayerStatistics()
        {
            var stats = new List<GameStatistics>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM PlayerStats ORDER BY Wins DESC, TotalGames DESC";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var totalGames = reader.GetInt32(1);
                                var wins = reader.GetInt32(2);

                                stats.Add(new GameStatistics
                                {
                                    PlayerName = reader.GetString(0),
                                    TotalGames = totalGames,
                                    Wins = wins,
                                    Losses = reader.GetInt32(3),
                                    Draws = reader.GetInt32(4),
                                    LastPlayed = reader.GetDateTime(5),
                                    WinRate = totalGames > 0 ? (double)wins / totalGames * 100 : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }

            return stats;
        }
    }
}