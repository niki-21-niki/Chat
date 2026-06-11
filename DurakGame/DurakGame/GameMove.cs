using System;
using System.Text.Json.Serialization;

namespace DurakGame
{
    [Serializable]
    public class GameMove
    {
        public string PlayerName { get; set; }
        public MoveType MoveType { get; set; }
        public Card Card { get; set; }
        public int CardIndex { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(PlayerName);

        public GameMove()
        {
            Timestamp = DateTime.Now;
            Card = new Card();
        }

        public GameMove(string playerName, MoveType moveType) : this()
        {
            PlayerName = playerName;
            MoveType = moveType;
        }

        public GameMove(string playerName, MoveType moveType, Card card, int cardIndex = -1) : this(playerName, moveType)
        {
            Card = card;
            CardIndex = cardIndex;
        }

        public override string ToString()
        {
            string cardInfo = Card != null && Card.Suit != 0 && Card.Rank != 0 ? $" картой {Card}" : "";
            return $"{PlayerName}: {GetMoveTypeText()}{cardInfo}";
        }

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

        public string Serialize()
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сериализации GameMove: {ex.Message}");
                return string.Empty;
            }
        }

        public static GameMove Deserialize(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return new GameMove();
                return System.Text.Json.JsonSerializer.Deserialize<GameMove>(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка десериализации GameMove: {ex.Message}");
                return new GameMove();
            }
        }
    }
}