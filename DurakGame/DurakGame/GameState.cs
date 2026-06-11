using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DurakGame
{
    [Serializable]
    public class GameState
    {
        // Основное состояние игры
        public List<Card> Deck { get; set; }
        public List<Card> Table { get; set; }
        public Suit TrumpSuit { get; set; }
        public bool IsGameActive { get; set; }
        public GameMode Mode { get; set; }

        // Текущие игроки
        public int CurrentAttackerIndex { get; set; }
        public int CurrentDefenderIndex { get; set; }

        // Полное состояние рук игроков
        public List<Card> Player1Hand { get; set; }
        public List<Card> Player2Hand { get; set; }

        // Информация об игроках
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }

        // Дополнительная информация
        public DateTime SaveTime { get; set; }
        public string GameId { get; set; }
        public int MoveCount { get; set; }

        [XmlIgnore]
        public Card TrumpCard { get; set; }

        public GameState()
        {
            Deck = new List<Card>();
            Table = new List<Card>();
            Player1Hand = new List<Card>();
            Player2Hand = new List<Card>();
            SaveTime = DateTime.Now;
        }

        // Метод для создания полного состояния из игры
        public static GameState FromGame(Game game)
        {
            var state = new GameState
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
                GameId = game.History?.GameId ?? Guid.NewGuid().ToString(),
                MoveCount = game.History?.Moves.Count ?? 0
            };

            return state;
        }

        // Метод применения состояния к игре
        // В класс GameState добавьте:
        public void ApplyToGame(Game game, Player humanPlayer, Player opponent)
        {
            try
            {
                // Очищаем текущее состояние
                game.Deck.Clear();
                game.Table.Clear();
                humanPlayer.Hand.Clear();
                opponent.Hand.Clear();

                // Копируем базовые данные
                foreach (var card in this.Deck)
                {
                    game.Deck.Add(new Card(card.Suit, card.Rank, card.Suit == this.TrumpSuit));
                }

                foreach (var card in this.Table)
                {
                    game.Table.Add(new Card(card.Suit, card.Rank, card.Suit == this.TrumpSuit));
                }

                game.TrumpSuit = this.TrumpSuit;
                game.IsGameActive = this.IsGameActive;
                game.Mode = this.Mode;

                // Копируем руки
                foreach (var card in this.Player1Hand)
                {
                    var newCard = new Card(card.Suit, card.Rank);
                    newCard.IsTrump = (newCard.Suit == game.TrumpSuit);
                    humanPlayer.Hand.Add(newCard);
                }

                foreach (var card in this.Player2Hand)
                {
                    var newCard = new Card(card.Suit, card.Rank);
                    newCard.IsTrump = (newCard.Suit == game.TrumpSuit);
                    opponent.Hand.Add(newCard);
                }

                // Сортируем
                humanPlayer.SortHand();
                opponent.SortHand();

                // Устанавливаем текущих игроков
                if (this.CurrentAttackerIndex == 0)
                {
                    game.CurrentAttacker = humanPlayer;
                    game.CurrentDefender = opponent;
                }
                else
                {
                    game.CurrentAttacker = opponent;
                    game.CurrentDefender = humanPlayer;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка применения состояния игры: {ex.Message}", ex);
            }
        }

        public string Serialize()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GameState));
                using (var writer = new System.IO.StringWriter())
                {
                    serializer.Serialize(writer, this);
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сериализации GameState: {ex.Message}");
                return string.Empty;
            }
        }

        public static GameState Deserialize(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return new GameState();

                XmlSerializer serializer = new XmlSerializer(typeof(GameState));
                using (var reader = new System.IO.StringReader(data))
                {
                    return (GameState)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка десериализации GameState: {ex.Message}");
                return new GameState();
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Player1Name) &&
                   !string.IsNullOrEmpty(Player2Name) &&
                   Deck != null && Table != null &&
                   Player1Hand != null && Player2Hand != null;
        }

        public override string ToString()
        {
            return $"GameState: {Player1Name} vs {Player2Name}, " +
                   $"Cards: P1({Player1Hand.Count}) P2({Player2Hand.Count}) " +
                   $"Deck({Deck.Count}) Table({Table.Count})";
        }
    }
}