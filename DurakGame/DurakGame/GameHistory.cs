using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DurakGame
{
    /// <summary>
    /// Тип игрового хода
    /// </summary>
    public enum MoveType
    {
        Attack,         // Атака картой
        Defend,         // Защита картой
        TakeCards,      // Взятие карт со стола
        Pass,           // Пас ("Бито")
        Transfer,       // Перевод хода
        GameStart,      // Начало игры
        GameEnd,        // Конец игры
        CardDealt,      // Взятие карты из колоды
        TrumpRevealed   // Открытие козыря
    }

    /// <summary>
    /// Запись о ходе в игре
    /// </summary>
    [Serializable]
    public class MoveRecord
    {
        /// <summary>
        /// Имя игрока, совершившего ход
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Карта, которой был совершен ход
        /// </summary>
        public Card Card { get; set; } = new Card();

        /// <summary>
        /// Тип хода
        /// </summary>
        public MoveType MoveType { get; set; }

        /// <summary>
        /// Время совершения хода
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Дополнительное описание хода
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Номер хода в игре
        /// </summary>
        public int MoveNumber { get; set; }

        public MoveRecord()
        {
            Timestamp = DateTime.Now;
        }

        public MoveRecord(string playerName, Card card, MoveType moveType, int moveNumber, string description = "")
        {
            PlayerName = playerName;
            Card = card;
            MoveType = moveType;
            MoveNumber = moveNumber;
            Timestamp = DateTime.Now;
            Description = description;
        }

        public override string ToString()
        {
            string cardInfo = (Card != null && Card.Suit != 0 && Card.Rank != 0) ? $" картой {Card}" : "";
            return $"{Timestamp:HH:mm:ss} - [{MoveNumber:000}] {PlayerName}: {GetMoveTypeText()}{cardInfo} {Description}";
        }

        /// <summary>
        /// Получить текстовое описание типа хода
        /// </summary>
        private string GetMoveTypeText()
        {
            return MoveType switch
            {
                MoveType.Attack => "Атаковал",
                MoveType.Defend => "Защитился",
                MoveType.TakeCards => "Взял карты",
                MoveType.Pass => "Пас",
                MoveType.Transfer => "Перевел ход",
                MoveType.GameStart => "Начало игры",
                MoveType.GameEnd => "Конец игры",
                MoveType.CardDealt => "Взял карту из колоды",
                MoveType.TrumpRevealed => "Открыт козырь",
                _ => "Совершил действие"
            };
        }

        /// <summary>
        /// Получить краткое описание хода для отображения в UI
        /// </summary>
        public string GetShortDescription()
        {
            string icon = GetMoveTypeIcon();
            string cardInfo = (Card != null && Card.Suit != 0 && Card.Rank != 0) ? $" {Card}" : "";
            return $"{icon} Ход {MoveNumber}: {PlayerName} {GetMoveTypeText()}{cardInfo}";
        }

        /// <summary>
        /// Получить иконку для типа хода
        /// </summary>
        private string GetMoveTypeIcon()
        {
            return MoveType switch
            {
                MoveType.Attack => "⚔️",
                MoveType.Defend => "🛡",
                MoveType.TakeCards => "🃏",
                MoveType.Pass => "✅",
                MoveType.Transfer => "🔄",
                MoveType.GameStart => "🎮",
                MoveType.GameEnd => "🏁",
                MoveType.CardDealt => "📥",
                MoveType.TrumpRevealed => "🎭",
                _ => "📝"
            };
        }
    }

    /// <summary>
    /// История игры - запись всех ходов и событий
    /// </summary>
    [Serializable]
    public class GameHistory
    {
        /// <summary>
        /// Список всех ходов в игре
        /// </summary>
        public List<MoveRecord> Moves { get; set; }

        /// <summary>
        /// Время начала игры
        /// </summary>
        public DateTime GameStart { get; set; }

        /// <summary>
        /// Время окончания игры
        /// </summary>
        public DateTime? GameEnd { get; set; }

        /// <summary>
        /// Идентификатор игры
        /// </summary>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// Игрок 1
        /// </summary>
        public string Player1Name { get; set; } = string.Empty;

        /// <summary>
        /// Игрок 2
        /// </summary>
        public string Player2Name { get; set; } = string.Empty;

        /// <summary>
        /// Режим игры
        /// </summary>
        public GameMode GameMode { get; set; }

        /// <summary>
        /// Победитель игры
        /// </summary>
        public string Winner { get; set; } = string.Empty;

        /// <summary>
        /// Счетчик ходов
        /// </summary>
        [XmlIgnore]
        private int moveCounter;

        public GameHistory()
        {
            Moves = new List<MoveRecord>();
            GameStart = DateTime.Now;
            GameId = GenerateGameId();
            moveCounter = 0;
        }

        public GameHistory(Player player1, Player player2, GameMode gameMode) : this()
        {
            Player1Name = player1.Name;
            Player2Name = player2.Name;
            GameMode = gameMode;

            // Записываем начало игры
            RecordGameStart();
        }

        /// <summary>
        /// Записать ход с картой
        /// </summary>
        public void RecordMove(Player player, Card card, MoveType moveType, string description = "")
        {
            moveCounter++;
            Moves.Add(new MoveRecord(player.Name, card, moveType, moveCounter, description));
        }

        /// <summary>
        /// Записать специальный ход (без карты)
        /// </summary>
        public void RecordSpecialMove(Player player, MoveType moveType, string description = "")
        {
            moveCounter++;
            Moves.Add(new MoveRecord(player.Name, new Card(), moveType, moveCounter, description));
        }

        /// <summary>
        /// Записать начало игры
        /// </summary>
        public void RecordGameStart()
        {
            RecordSpecialMove(new Player("Система"), MoveType.GameStart,
                $"Начало игры: {Player1Name} vs {Player2Name}. Режим: {GetGameModeText()}");
        }

        /// <summary>
        /// Записать конец игры
        /// </summary>
        public void RecordGameEnd(Player? winner = null)
        {
            GameEnd = DateTime.Now;
            Winner = winner?.Name ?? "Ничья";

            string result = winner != null ?
                $"Победитель: {winner.Name}" :
                "Ничья! Оба игрока остались без карт";

            RecordSpecialMove(new Player("Система"), MoveType.GameEnd,
                $"{result}. Продолжительность: {GetGameDuration()}");
        }

        /// <summary>
        /// Завершить игру (альтернативное название для RecordGameEnd)
        /// </summary>
        public void EndGame(Player? winner = null)
        {
            RecordGameEnd(winner);
        }

        /// <summary>
        /// Записать открытие козыря
        /// </summary>
        public void RecordTrumpRevealed(Suit trumpSuit)
        {
            RecordSpecialMove(new Player("Система"), MoveType.TrumpRevealed,
                $"Козырная масть: {GetSuitName(trumpSuit)}");
        }

        /// <summary>
        /// Получить историю ходов
        /// </summary>
        public List<string> GetMoveHistory(int maxMoves = 20)
        {
            var result = new List<string>();
            var startIndex = Math.Max(0, Moves.Count - maxMoves);

            for (int i = startIndex; i < Moves.Count; i++)
            {
                result.Add(Moves[i].ToString());
            }

            return result;
        }

        /// <summary>
        /// Получить краткую историю ходов для UI
        /// </summary>
        public List<string> GetShortMoveHistory(int maxMoves = 15)
        {
            var result = new List<string>();
            var startIndex = Math.Max(0, Moves.Count - maxMoves);

            for (int i = startIndex; i < Moves.Count; i++)
            {
                result.Add(Moves[i].GetShortDescription());
            }

            return result;
        }

        /// <summary>
        /// Получить продолжительность игры
        /// </summary>
        public string GetGameDuration()
        {
            var end = GameEnd ?? DateTime.Now;
            var duration = end - GameStart;
            return $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
        }

        /// <summary>
        /// Получить статистику игры
        /// </summary>
        public GameStatistics GetStatistics()
        {
            return new GameStatistics(this);
        }

        /// <summary>
        /// Сохранить историю в файл
        /// </summary>
        public void SaveToFile(string? filename = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    string directory = "GameHistory";
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    filename = Path.Combine(directory, $"GameHistory_{GameId}.xml");
                }

                XmlSerializer serializer = new XmlSerializer(typeof(GameHistory));
                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения истории игры: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузить историю из файла
        /// </summary>
        public static GameHistory? LoadFromFile(string filename)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GameHistory));
                using (FileStream stream = new FileStream(filename, FileMode.Open))
                {
                    return (GameHistory)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки истории игры: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Сгенерировать уникальный идентификатор игры
        /// </summary>
        private string GenerateGameId()
        {
            return $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>
        /// Получить текстовое описание режима игры
        /// </summary>
        private string GetGameModeText()
        {
            return GameMode switch
            {
                GameMode.ThrowIn => "Подкидной дурак",
               
                _ => "Неизвестный режим"
            };
        }

        /// <summary>
        /// Получить название масти
        /// </summary>
        private string GetSuitName(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "Черви",
                Suit.Diamonds => "Бубны",
                Suit.Clubs => "Трефы",
                Suit.Spades => "Пики",
                _ => "Неизвестно"
            };
        }
    }

    /// <summary>
    /// Статистика игры и игрока (объединенный класс)
    /// </summary>
    public class GameStatistics
    {
        // Свойства для статистики игры
        public string GameId { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public int TotalMoves { get; set; }
        public int Player1Moves { get; set; }
        public int Player2Moves { get; set; }
        public int AttackMoves { get; set; }
        public int DefendMoves { get; set; }
        public string Winner { get; set; } = string.Empty;
        public DateTime GameDate { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;

        // Свойства для статистики игрока
        public string PlayerName { get; set; } = "";
        public int TotalGames { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public DateTime LastPlayed { get; set; }

        // Конструкторы
        public GameStatistics() { }

        public GameStatistics(string playerName)
        {
            PlayerName = playerName;
        }

        public GameStatistics(GameHistory history)
        {
            if (history != null)
            {
                // Инициализация статистики игры
                GameId = history.GameId;
                Duration = history.GetGameDuration();
                TotalMoves = history.Moves.Count;
                GameDate = history.GameStart;
                Winner = history.Winner;
                Player1Name = history.Player1Name;
                Player2Name = history.Player2Name;

                // Подсчет статистики ходов
                foreach (var move in history.Moves)
                {
                    if (move.PlayerName == history.Player1Name) Player1Moves++;
                    if (move.PlayerName == history.Player2Name) Player2Moves++;
                    if (move.MoveType == MoveType.Attack) AttackMoves++;
                    if (move.MoveType == MoveType.Defend) DefendMoves++;
                }
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(GameId))
            {
                // Статистика игры
                return $"Игра {GameId} | Длительность: {Duration} | Ходов: {TotalMoves} | Победитель: {Winner}";
            }
            else
            {
                // Статистика игрока
                return $"{PlayerName}: {Wins}/{TotalGames} побед ({WinRate:F1}%)";
            }
        }

        /// <summary>
        /// Получить подробную статистику
        /// </summary>
        public string GetDetailedStats()
        {
            if (!string.IsNullOrEmpty(GameId))
            {
                // Статистика игры
                return $@"📊 Статистика игры:
🆔 ID: {GameId}
📅 Дата: {GameDate:dd.MM.yyyy HH:mm}
⏱️ Длительность: {Duration}
🎮 Всего ходов: {TotalMoves}
⚔️ Атакующих ходов: {AttackMoves}
🛡 Защитных ходов: {DefendMoves}
👤 Ходов {Player1Name}: {Player1Moves}
👤 Ходов {Player2Name}: {Player2Moves}
🏆 Победитель: {Winner}";
            }
            else
            {
                // Статистика игрока
                return $@"📊 Статистика игрока:
👤 Игрок: {PlayerName}
🎮 Всего игр: {TotalGames}
🏆 Побед: {Wins}
😞 Поражений: {Losses}
🤝 Ничьих: {Draws}
📊 Рейтинг побед: {WinRate:F1}%
📅 Последняя игра: {LastPlayed:dd.MM.yyyy HH:mm}";
            }
        }
    }
}