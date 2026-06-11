using System;
using System.Drawing;

namespace DurakGame
{
    [Serializable]
    public class Card : ICloneable
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public bool IsTrump { get; set; }

        [NonSerialized]
        private Image? _image;

        public Image Image
        {
            get => _image ?? CardImageManager.GetCardImage(Suit, Rank);
            set => _image = value;
        }

        public Card()
        {
            Suit = Suit.Hearts;
            Rank = Rank.Six;
            IsTrump = false;
        }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
            IsTrump = false;
        }

        public Card(Suit suit, Rank rank, bool isTrump)
        {
            Suit = suit;
            Rank = rank;
            IsTrump = isTrump;
        }

        public bool CanBeat(Card attackingCard, Suit trumpSuit)
        {
            // Козырная карта бьет некозырную
            if (Suit == trumpSuit && attackingCard.Suit != trumpSuit)
                return true;

            // Карта той же масти бьет карту меньшего достоинства
            if (Suit == attackingCard.Suit)
                return (int)Rank > (int)attackingCard.Rank;

            // Козырная карта бьет козырную карту меньшего достоинства
            if (Suit == trumpSuit && attackingCard.Suit == trumpSuit)
                return (int)Rank > (int)attackingCard.Rank;

            return false;
        }

        public override string ToString()
        {
            string rankStr = Rank switch
            {
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "В",
                Rank.Queen => "Д",
                Rank.King => "К",
                Rank.Ace => "Т",
                _ => "?"
            };

            string suitStr = Suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };

            return $"{rankStr}{suitStr}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is Card other)
                return Suit == other.Suit && Rank == other.Rank;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Suit, Rank);
        }

        public object Clone()
        {
            return new Card(Suit, Rank, IsTrump);
        }
    }

    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum Rank
    {
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public enum GameMode
    {
        ThrowIn
    }
}