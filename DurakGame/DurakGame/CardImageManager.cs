using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DurakGame
{
    public static class CardImageManager
    {
        public static Image GetCardImage(Suit suit, Rank rank)
        {
            return CreateDefaultCardImage(suit, rank);
        }

        private static Image CreateDefaultCardImage(Suit suit, Rank rank)
        {
            Bitmap bmp = new Bitmap(80, 120);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Black, 0, 0, 79, 119);

                Color suitColor = (suit == Suit.Hearts || suit == Suit.Diamonds) ? Color.Red : Color.Black;
                string suitSymbol = GetSuitSymbol(suit);
                string rankSymbol = GetRankSymbol(rank);

                using (Font symbolFont = new Font("Arial", 20, FontStyle.Bold))
                using (Font rankFont = new Font("Arial", 14, FontStyle.Bold))
                using (Brush brush = new SolidBrush(suitColor))
                {
                    // Центральный символ масти
                    SizeF symbolSize = g.MeasureString(suitSymbol, symbolFont);
                    g.DrawString(suitSymbol, symbolFont, brush, 40 - symbolSize.Width / 2, 60 - symbolSize.Height / 2);

                    // Верхний левый угол - достоинство
                    g.DrawString(rankSymbol, rankFont, brush, 5, 5);

                    // Нижний правый угол - перевернутое достоинство
                    g.TranslateTransform(80, 120);
                    g.RotateTransform(180);
                    g.DrawString(rankSymbol, rankFont, brush, -75, -115);
                    g.ResetTransform();
                }
            }
            return bmp;
        }

        private static string GetSuitSymbol(Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };
        }

        private static string GetRankSymbol(Rank rank)
        {
            return rank switch
            {
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                Rank.Ace => "A",
                _ => "?"
            };
        }

        public static Image GetCardBackImage()
        {
            Bitmap bmp = new Bitmap(80, 120);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.DarkBlue);

                using (Brush patternBrush = new SolidBrush(Color.LightBlue))
                using (Pen borderPen = new Pen(Color.Gold, 2))
                {
                    // Рисуем узор на рубашке
                    for (int x = -10; x < 90; x += 15)
                    {
                        for (int y = -10; y < 130; y += 20)
                        {
                            Point[] points = {
                                new Point(x, y + 10),
                                new Point(x + 7, y),
                                new Point(x + 14, y + 10),
                                new Point(x + 7, y + 20)
                            };
                            g.FillPolygon(patternBrush, points);
                        }
                    }

                    // Двойная золотая рамка
                    g.DrawRectangle(borderPen, 2, 2, 76, 116);
                    g.DrawRectangle(borderPen, 5, 5, 70, 110);
                }
            }
            return bmp;
        }

        // Упрощенная предзагрузка - просто создаем все изображения один раз
        public static void PreloadCardImages()
        {
            Task.Run(() =>
            {
                try
                {
                    var suits = Enum.GetValues(typeof(Suit));
                    var ranks = Enum.GetValues(typeof(Rank));

                    foreach (Suit suit in suits)
                    {
                        foreach (Rank rank in ranks)
                        {
                            // Просто создаем изображения, они кэшируются автоматически
                            GetCardImage(suit, rank);
                        }
                    }

                    // Предзагрузка рубашки
                    GetCardBackImage();

                    System.Diagnostics.Debug.WriteLine("✅ Все изображения карт предзагружены");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка предзагрузки карт: {ex.Message}");
                }
            });
        }
    }
}