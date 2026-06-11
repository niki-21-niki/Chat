using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DurakGame
{
    [Serializable]
    public class Player
    {
        public string Name { get; set; }
        public string Password { get; set; }

        [JsonIgnore]
        public List<Card> Hand { get; set; }

        public bool IsHuman { get; set; }
        public int Wins { get; set; }
        public int GamesPlayed { get; set; }
        public DateTime RegistrationDate { get; set; }

        [JsonIgnore]
        public bool IsOnline { get; set; }

        public Player()
        {
            Hand = new List<Card>();
            RegistrationDate = DateTime.Now;
            IsHuman = true;
            Password = "";
        }

        public Player(string name, string password = "", bool isHuman = true) : this()
        {
            Name = name;
            Password = password;
            IsHuman = isHuman;
        }

        [JsonIgnore]
        public double WinRate => GamesPlayed > 0 ? (double)Wins / GamesPlayed * 100 : 0;

        // Упрощенные методы для сериализации
        public void AddCard(Card card)
        {
            Hand.Add(card);
            SortHand();
        }

        public void SortHand()
        {
            Hand = Hand.OrderBy(c => c.Suit)
                      .ThenBy(c => c.Rank)
                      .ToList();
        }

        public Card PlayCard(int index)
        {
            if (index >= 0 && index < Hand.Count)
            {
                var card = Hand[index];
                Hand.RemoveAt(index);
                return card;
            }
            return null;
        }

        public bool CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(Password))
                return string.IsNullOrEmpty(password);
            return Password == password;
        }

        public override string ToString()
        {
            return $"{Name} (Побед: {Wins}, Рейтинг: {WinRate:F1}%)";
        }
    }
}