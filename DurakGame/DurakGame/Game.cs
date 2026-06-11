using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DurakGame.GameRepository;

namespace DurakGame
{
    public class Game
    {
        public List<Card> Deck { get; set; }
        public List<Card> DiscardPile { get; set; }
        public List<Card> Table { get; set; }
        public Player HumanPlayer { get; set; }
        public Player ComputerPlayer { get; set; }
        public Suit TrumpSuit { get; set; }
        public bool IsGameActive { get; set; }
        public Player CurrentAttacker { get; set; }
        public Player CurrentDefender { get; set; }
        public GameMode Mode { get; set; }
        public bool IsLocalMultiplayer { get; set; }
        public GameHistory History { get; set; }

        private GameRepository gameRepository;
        private System.Timers.Timer autoSaveTimer;
        private bool isOnlineGame;
        private bool isComputerThinking = false;

        public event Action? GameStateChanged;
        public event Action<string>? GameMessage;
        public event Action<Player?>? GameEnded;

        // В классе Game ЗАМЕНИТЕ конструктор и добавьте методы для сетевой игры
        public Game(Player humanPlayer, Player opponent, GameMode mode,
                bool isLocalMultiplayer = false, bool isOnlineGame = false)
        {
            HumanPlayer = humanPlayer;
            ComputerPlayer = opponent;
            Mode = mode;
            IsLocalMultiplayer = isLocalMultiplayer;
            this.isOnlineGame = isOnlineGame;

            Deck = new List<Card>();
            DiscardPile = new List<Card>();
            Table = new List<Card>();
            History = new GameHistory(humanPlayer, opponent, mode);

            // Только для локальных игр инициализируем автосохранение
            if (!isOnlineGame)
            {
                gameRepository = new GameRepository();
                SetupAutoSave();
            }

            InitializeGame();
        }

        // Добавьте метод для применения состояния из сети
        public void ApplyNetworkState(GameState gameState)
        {
            if (gameState == null) return;

            Deck = new List<Card>(gameState.Deck);
            Table = new List<Card>(gameState.Table);
            TrumpSuit = gameState.TrumpSuit;
            IsGameActive = gameState.IsGameActive;

            // Восстанавливаем текущих игроков
            if (gameState.CurrentAttackerIndex == 0)
            {
                CurrentAttacker = HumanPlayer;
                CurrentDefender = ComputerPlayer;
            }
            else
            {
                CurrentAttacker = ComputerPlayer;
                CurrentDefender = HumanPlayer;
            }

            GameStateChanged?.Invoke();
        }

        private void SetupAutoSave()
        {
            autoSaveTimer = new System.Timers.Timer(30000);
            autoSaveTimer.Elapsed += (s, e) => AutoSave();
            autoSaveTimer.AutoReset = true;
            autoSaveTimer.Start();
        }

        private void AutoSave()
        {
            if (IsGameActive)
            {
                try
                {
                    var gameState = GameState.FromGame(this);
                    gameRepository.SaveGame(History, gameState);

                    System.Diagnostics.Debug.WriteLine($"✅ Игра автосохранена: {DateTime.Now:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка автосохранения: {ex.Message}");
                }
            }
        }



        private void InitializeGame()
        {
            CreateDeck();
            ShuffleDeck();
            DetermineTrump();
            DealCards();
            DetermineFirstAttacker();
            IsGameActive = true;

            History.RecordGameStart();
            History.RecordTrumpRevealed(TrumpSuit);

            GameStateChanged?.Invoke();
            GameMessage?.Invoke($"Игра началась! Первым ходит {CurrentAttacker?.Name}");

            if (CurrentAttacker == ComputerPlayer && !ComputerPlayer.IsHuman)
            {
                // Простая задержка перед первым ходом компьютера
                Task.Delay(1000).ContinueWith(t =>
                {
                    if (IsGameActive && !isComputerThinking)
                    {
                        ComputerTurn();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        public void CreateDeck()
        {
            Deck.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    if ((int)rank >= 6)
                    {
                        Deck.Add(new Card(suit, rank));
                    }
                }
            }
        }

        public void ShuffleDeck()
        {
            Random rng = new Random();
            int n = Deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card value = Deck[k];
                Deck[k] = Deck[n];
                Deck[n] = value;
            }
        }

        public void DetermineTrump()
        {
            if (Deck.Count > 0)
            {
                Random rng = new Random();
                int trumpIndex = rng.Next(Deck.Count);
                TrumpSuit = Deck[trumpIndex].Suit;

                // Отмечаем все козырные карты
                foreach (var card in Deck)
                {
                    card.IsTrump = card.Suit == TrumpSuit;
                }

                // Перемещаем козырную карту в конец колоды (последней)
                var trumpCard = Deck[trumpIndex];
                Deck.RemoveAt(trumpIndex);
                Deck.Add(trumpCard); // Добавляем в конец

                System.Diagnostics.Debug.WriteLine($"Козырь: {TrumpSuit}, козырная карта в конце колоды");
            }
        }

        public void DealCards()
        {
            HumanPlayer.Hand.Clear();
            ComputerPlayer.Hand.Clear();

            for (int i = 0; i < 6; i++)
            {
                if (Deck.Count > 0)
                {
                    HumanPlayer.AddCard(Deck[0]);
                    Deck.RemoveAt(0);
                }
                if (Deck.Count > 0)
                {
                    ComputerPlayer.AddCard(Deck[0]);
                    Deck.RemoveAt(0);
                }
            }
        }

        public void DetermineFirstAttacker()
        {
            var humanTrump = HumanPlayer.Hand.Where(c => c.Suit == TrumpSuit)
                .OrderBy(c => c.Rank)
                .FirstOrDefault();

            var computerTrump = ComputerPlayer.Hand.Where(c => c.Suit == TrumpSuit)
                .OrderBy(c => c.Rank)
                .FirstOrDefault();

            if (humanTrump == null && computerTrump == null)
            {
                // Если козырей нет, ищем наименьшую карту любой масти
                var humanMin = HumanPlayer.Hand.OrderBy(c => c.Rank).FirstOrDefault();
                var computerMin = ComputerPlayer.Hand.OrderBy(c => c.Rank).FirstOrDefault();

                CurrentAttacker = (humanMin?.Rank <= computerMin?.Rank) ? HumanPlayer : ComputerPlayer;
            }
            else if (humanTrump == null)
            {
                CurrentAttacker = ComputerPlayer;
            }
            else if (computerTrump == null)
            {
                CurrentAttacker = HumanPlayer;
            }
            else
            {
                CurrentAttacker = (humanTrump.Rank <= computerTrump.Rank) ? HumanPlayer : ComputerPlayer;
            }

            CurrentDefender = CurrentAttacker == HumanPlayer ? ComputerPlayer : HumanPlayer;
        }

        

        public bool CanDefendWithCard(Card defendingCard, Card attackingCard)
        {
            if (defendingCard.Suit == attackingCard.Suit)
            {
                return defendingCard.Rank > attackingCard.Rank;
            }
            else if (defendingCard.Suit == TrumpSuit && attackingCard.Suit != TrumpSuit)
            {
                return true;
            }
            return false;
        }

        

        public bool CanAttackMore()
        {
            if (Table.Count == 0 || Table.Count % 2 != 0)
                return false;

            // Максимум 6 карт на столе (3 пары)
            if (Table.Count >= 6)
                return false;

            // Нельзя подкидывать больше карт, чем есть у защищающегося
            if (Table.Count >= CurrentDefender!.Hand.Count * 2)
                return false;

            var tableRanks = Table.Select(card => card.Rank).Distinct();
            return CurrentAttacker!.Hand.Any(card =>
                tableRanks.Contains(card.Rank) &&
                !Table.Any(t => t.Equals(card))); // Проверяем, что карта еще не на столе
        }

        public bool AllCardsBeaten()
        {
            return Table.Count > 0 && Table.Count % 2 == 0;
        }

        public void HumanPass()
        {
            if (!IsGameActive || CurrentAttacker != HumanPlayer || Table.Count == 0)
                return;

            // Можно завершить ход только если все карты побиты
            if (AllCardsBeaten())
            {
                CompleteRound(true);
                CheckGameState();
            }
            else
            {
                GameMessage?.Invoke("Не все карты побиты! Нельзя завершить раунд.");
            }
        }

        public void HumanAttack(int cardIndex)
        {
            if (!IsGameActive || CurrentAttacker != HumanPlayer)
                return;

            if (cardIndex < 0 || cardIndex >= HumanPlayer.Hand.Count)
                return;

            var card = HumanPlayer.Hand[cardIndex];

            // Проверяем можно ли подкидывать эту карту
            if (Table.Count > 0 && !CanAttackWithCard(card))
            {
                GameMessage?.Invoke("Можно подкидывать только карты того же достоинства, что уже лежат на столе!");
                return;
            }

            // Проверяем лимит карт на столе (максимум 6)
            if (Table.Count >= 6)
            {
                GameMessage?.Invoke("Нельзя подкидывать больше 6 карт!");
                return;
            }

            var playedCard = HumanPlayer.PlayCard(cardIndex);
            if (playedCard != null)
            {
                Table.Add(playedCard);
                History.RecordMove(HumanPlayer, playedCard, MoveType.Attack);
                SoundManager.PlayCardPlace();

                GameStateChanged?.Invoke();
                CheckGameState();

                // Если после атаки защитник - компьютер, он должен защищаться
                if (IsGameActive && CurrentDefender == ComputerPlayer && !ComputerPlayer.IsHuman)
                {
                    Task.Delay(1000).ContinueWith(t =>
                    {
                        if (IsGameActive && !isComputerThinking)
                        {
                            ComputerDefend();
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        public void HumanDefend(int cardIndex)
        {
            if (!IsGameActive || CurrentDefender != HumanPlayer || Table.Count == 0)
                return;

            if (cardIndex < 0 || cardIndex >= HumanPlayer.Hand.Count)
                return;

            var attackingCard = Table.Last();
            var defendingCard = HumanPlayer.Hand[cardIndex];

            if (defendingCard.CanBeat(attackingCard, TrumpSuit))
            {
                var playedCard = HumanPlayer.PlayCard(cardIndex);
                if (playedCard != null)
                {
                    Table.Add(playedCard);
                    History.RecordMove(HumanPlayer, playedCard, MoveType.Defend);
                    SoundManager.PlayCardPlace();

                    GameStateChanged?.Invoke();

                    if (!CanAttackMore())
                    {
                        CompleteRound(true);
                    }
                    else
                    {
                        GameStateChanged?.Invoke();
                        GameMessage?.Invoke("Вы успешно отбились! Атакующий может подкинуть еще карты.");
                    }

                    CheckGameState();
                }
            }
            else
            {
                GameMessage?.Invoke("Эта карта не может побить атакующую карту!");
            }
        }

        public void HumanTakeCards()
        {
            if (!IsGameActive || CurrentDefender != HumanPlayer || Table.Count == 0) return;

            HumanPlayer.Hand.AddRange(Table);
            Table.Clear();
            HumanPlayer.SortHand();
            CompleteRound(false);
            CheckGameState();

            // УБЕРИТЕ старый вызов ComputerTurn - он теперь в CompleteRound
            // if (IsGameActive && CurrentAttacker == ComputerPlayer && !ComputerPlayer.IsHuman)
            // {
            //     Task.Delay(1000).ContinueWith(t =>
            //     {
            //         if (IsGameActive && !isComputerThinking)
            //         {
            //             ComputerTurn();
            //         }
            //     }, TaskScheduler.FromCurrentSynchronizationContext());
            // }
        }


        

        // ЗАМЕНИТЕ метод ComputerTurn в классе Game на этот:
        private void ComputerTurn()
        {
            if (!IsGameActive || isComputerThinking) return;

            isComputerThinking = true;

            try
            {
                // Используем Task.Delay для имитации размышления
                Task.Delay(1000).ContinueWith(t =>
                {
                    if (!IsGameActive)
                    {
                        isComputerThinking = false;
                        return;
                    }

                    try
                    {
                        if (CurrentAttacker == ComputerPlayer)
                        {
                            ComputerAttack();
                        }
                        else if (CurrentDefender == ComputerPlayer)
                        {
                            ComputerDefend();
                        }
                    }
                    finally
                    {
                        isComputerThinking = false;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch
            {
                isComputerThinking = false;
            }
        }

        // Улучшенный метод ComputerAttack
        public void ComputerAttack()
        {
            if (ComputerPlayer.Hand.Count == 0) return;

            var possible = GetPossibleAttackCards();
            if (!possible.Any()) return;

            var card = possible.First();
            Table.Add(card);
            ComputerPlayer.Hand.Remove(card);

            CurrentAttacker = HumanPlayer;
            CurrentDefender = ComputerPlayer;

            GameStateChanged?.Invoke();
            GameMessage?.Invoke("Компьютер подкидывает карту");
        }

        public void ComputerDefend()
        {
            var attackingCard = Table.FirstOrDefault(c => !IsBeaten(c));
            if (attackingCard == null) return;

            var possible = ComputerPlayer.Hand.Where(c => c.CanBeat(attackingCard, TrumpSuit)).ToList();
            if (possible.Any())
            {
                var card = possible.OrderBy(c => (int)c.Rank).First();
                Table.Add(card);
                ComputerPlayer.Hand.Remove(card);
                GameMessage?.Invoke("Компьютер отбился");
            }
            else
            {
                TakeCards(ComputerPlayer);
                GameMessage?.Invoke("Компьютер берёт карты");
            }

            GameStateChanged?.Invoke();
        }

        private List<Card> GetPossibleAttackCards()
        {
            var ranksOnTable = Table.Select(c => c.Rank).Distinct();
            return ComputerPlayer.Hand.Where(c =>
                Table.Count == 0 || ranksOnTable.Contains(c.Rank)
            ).ToList();
        }
        

        private void CompleteRound(bool successfulDefense)
        {
            if (successfulDefense)
            {
                DiscardPile.AddRange(Table);
                Table.Clear();
                History.RecordSpecialMove(CurrentDefender!, MoveType.Pass, "Успешная защита");
                GameMessage?.Invoke("✅ Все карты отбиты! Раунд завершен.");
            }
            else
            {
                CurrentDefender!.Hand.AddRange(Table);
                Table.Clear();
                CurrentDefender.SortHand();
                History.RecordSpecialMove(CurrentDefender!, MoveType.TakeCards, "Взял карты");
                GameMessage?.Invoke($"🃏 {CurrentDefender!.Name} взял карты со стола.");
            }

            SwitchRoles();
            DealAdditionalCards();
            GameStateChanged?.Invoke();
            CheckGameState();

            // ВОССТАНАВЛИВАЕМ вызов ComputerTurn для продолжения игры
            if (IsGameActive && CurrentAttacker == ComputerPlayer && !ComputerPlayer.IsHuman)
            {
                Task.Delay(1500).ContinueWith(t =>
                {
                    if (IsGameActive && !isComputerThinking)
                    {
                        ComputerTurn();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void SwitchRoles()
        {
            var temp = CurrentAttacker;
            CurrentAttacker = CurrentDefender;
            CurrentDefender = temp;
        }

        private void DealAdditionalCards()
        {
            while (CurrentAttacker!.Hand.Count < 6 && Deck.Count > 0)
            {
                CurrentAttacker.AddCard(Deck[0]);
                Deck.RemoveAt(0);
            }

            while (CurrentDefender!.Hand.Count < 6 && Deck.Count > 0)
            {
                CurrentDefender.AddCard(Deck[0]);
                Deck.RemoveAt(0);
            }
        }

        // Улучшенный метод выбора карты для атаки
        private Card ChooseComputerAttackCard()
        {
            if (Table.Count == 0)
            {
                // Первая атака - выбираем наименьшую некозырную карту
                return ComputerPlayer.Hand
                    .Where(c => !c.IsTrump)
                    .OrderBy(c => c.Rank)
                    .FirstOrDefault()
                    ?? ComputerPlayer.Hand
                    .OrderBy(c => c.Rank)
                    .FirstOrDefault();
            }
            else
            {
                // Подкидывание - ищем карты того же достоинства
                var availableRanks = Table.Select(c => c.Rank).Distinct();
                return ComputerPlayer.Hand
                    .Where(c => availableRanks.Contains(c.Rank))
                    .OrderBy(c => c.IsTrump ? 1 : 0) // Сначала некозырные
                    .ThenBy(c => c.Rank)
                    .FirstOrDefault();
            }
        }

        // Улучшенный метод выбора карты для защиты
        private Card ChooseComputerDefendCard()
        {
            if (Table.Count == 0 || ComputerPlayer.Hand.Count == 0)
                return null;

            var attackingCard = Table.Last();

            // Сначала ищем карты той же масти с большим достоинством
            var sameSuitCards = ComputerPlayer.Hand
                .Where(c => c.Suit == attackingCard.Suit && (int)c.Rank > (int)attackingCard.Rank)
                .OrderBy(c => c.Rank)
                .ToList();

            if (sameSuitCards.Any())
                return sameSuitCards.First();

            // Затем ищем козырные карты (если атакующая карта не козырь)
            if (!attackingCard.IsTrump)
            {
                var trumpCards = ComputerPlayer.Hand
                    .Where(c => c.IsTrump)
                    .OrderBy(c => c.Rank)
                    .ToList();

                if (trumpCards.Any())
                    return trumpCards.First();
            }

            // Если атакующая карта - козырь, ищем козырные карты большего достоинства
            if (attackingCard.IsTrump)
            {
                var higherTrumpCards = ComputerPlayer.Hand
                    .Where(c => c.IsTrump && (int)c.Rank > (int)attackingCard.Rank)
                    .OrderBy(c => c.Rank)
                    .ToList();

                if (higherTrumpCards.Any())
                    return higherTrumpCards.First();
            }

            return null;
        }

        public void CheckGameState()
        {
            if (!IsGameActive) return;

            // Игрок выиграл если у него нет карт и колода пуста
            if (HumanPlayer.Hand.Count == 0 && Deck.Count == 0)
            {
                EndGame(HumanPlayer);
                return;
            }

            // Компьютер выиграл если у него нет карт и колода пуста
            if (ComputerPlayer.Hand.Count == 0 && Deck.Count == 0)
            {
                EndGame(ComputerPlayer);
                return;
            }

            // Ничья если оба игрока без карт и колода пуста
            if (HumanPlayer.Hand.Count == 0 && ComputerPlayer.Hand.Count == 0 && Deck.Count == 0)
            {
                EndGame(null);
                return;
            }

            // ДОБАВЛЯЕМ: Автоматически продолжаем ход компьютера если нужно
            if (IsGameActive && CurrentAttacker == ComputerPlayer && !ComputerPlayer.IsHuman && !isComputerThinking)
            {
                Task.Delay(1000).ContinueWith(t =>
                {
                    if (IsGameActive && !isComputerThinking)
                    {
                        ComputerTurn();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void EndGame(Player? winner)
        {
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();

            IsGameActive = false;

            if (winner != null)
            {
                winner.Wins++;
                winner.GamesPlayed++;

                var loser = winner == HumanPlayer ? ComputerPlayer : HumanPlayer;
                loser.GamesPlayed++;

                PlayerManager.Instance.UpdatePlayerStats(HumanPlayer);
                if (!ComputerPlayer.IsHuman)
                {
                    PlayerManager.Instance.UpdatePlayerStats(ComputerPlayer);
                }

                History.RecordGameEnd(winner);

                try
                {
                    gameRepository.SaveGame(History, null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения завершенной игры: {ex.Message}");
                }

                GameEnded?.Invoke(winner);

                // ДОБАВЛЕНО: Воспроизведение музыки и создание сообщения
                if (winner == HumanPlayer)
                {
                    SoundManager.PlayWin();
                    GameMessage?.Invoke("🎉 ПОЗДРАВЛЯЕМ! ВЫ ВЫИГРАЛИ!");

                    // Показываем сообщение о победе
                    ShowGameResultMessage("🎉 ПОБЕДА!", "Поздравляем! Вы выиграли эту партию!", Color.Green);
                }
                else
                {
                    SoundManager.PlayLose();
                    GameMessage?.Invoke("😔 К сожалению, вы проиграли.");

                    // Показываем сообщение о проигрыше
                    ShowGameResultMessage("😔 ПОРАЖЕНИЕ", "К сожалению, вы проиграли эту партию.", Color.Red);
                }
            }
            else
            {
                HumanPlayer.GamesPlayed++;
                ComputerPlayer.GamesPlayed++;
                History.RecordGameEnd(null);

                try
                {
                    gameRepository.SaveGame(History, null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения завершенной игры: {ex.Message}");
                }

                GameEnded?.Invoke(null);
                GameMessage?.Invoke("🤝 Ничья! Оба игрока остались без карт.");

                // ДОБАВЛЕНО: Сообщение о ничьей
                ShowGameResultMessage("🤝 НИЧЬЯ", "Оба игрока остались без карт!", Color.Orange);
            }

            GameStateChanged?.Invoke();
        }

        // ДОБАВЬТЕ этот метод в класс Game
        private void ShowGameResultMessage(string title, string message, Color color)
        {
            // Используем MessageBox для простоты
            Task.Delay(500).ContinueWith(t =>
            {
                System.Windows.Forms.MessageBox.Show(message, title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        

        public string GetGameStatus()
        {
            if (!IsGameActive)
            {
                if (HumanPlayer.Hand.Count == 0 && ComputerPlayer.Hand.Count == 0)
                    return "🤝 Ничья!";
                else if (HumanPlayer.Hand.Count == 0)
                    return $"🎉 {HumanPlayer.Name} победил!";
                else
                    return $"🎉 {ComputerPlayer.Name} победил!";
            }

            if (CurrentAttacker == HumanPlayer)
                return "🎯 ВАШ ХОД (АТАКА)";
            else if (CurrentDefender == HumanPlayer)
                return "🛡 ВАШ ХОД (ЗАЩИТА)";
            else if (CurrentAttacker == ComputerPlayer && !ComputerPlayer.IsHuman)
                return "⏳ ХОД КОМПЬЮТЕРА";
            else
                return "⏳ ХОД ПРОТИВНИКА";
        }

        public static Game LoadSavedGame(string gameId, Player humanPlayer, Player opponent)
        {
            try
            {
                var repository = new GameRepository();
                var history = repository.LoadGame(gameId);

                if (history != null)
                {
                    var game = new Game(humanPlayer, opponent, history.GameMode)
                    {
                        History = history,
                        TrumpSuit = history.Moves
                            .FirstOrDefault(m => m.MoveType == MoveType.TrumpRevealed)?
                            .Description.Contains("Черви") ?? false ? Suit.Hearts :
                            history.Moves.FirstOrDefault(m => m.MoveType == MoveType.TrumpRevealed)?
                            .Description.Contains("Бубны") ?? false ? Suit.Diamonds :
                            history.Moves.FirstOrDefault(m => m.MoveType == MoveType.TrumpRevealed)?
                            .Description.Contains("Трефы") ?? false ? Suit.Clubs : Suit.Spades
                    };

                    // Восстанавливаем стол
                    var lastMoves = history.Moves
                        .Where(m => m.MoveType == MoveType.Attack || m.MoveType == MoveType.Defend)
                        .TakeLast(10)
                        .ToList();

                    foreach (var move in lastMoves)
                    {
                        if (move.Card != null && move.Card.Suit != 0 && move.Card.Rank != 0)
                        {
                            game.Table.Add(move.Card);
                        }
                    }

                    // Восстанавливаем текущих игроков
                    var lastMove = history.Moves.LastOrDefault();
                    if (lastMove != null)
                    {
                        game.CurrentAttacker = lastMove.PlayerName == humanPlayer.Name ?
                            humanPlayer : opponent;
                        game.CurrentDefender = game.CurrentAttacker == humanPlayer ?
                            opponent : humanPlayer;
                    }

                    game.IsGameActive = true;
                    return game;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки игры: {ex.Message}");
                MessageBox.Show($"Не удалось загрузить игру: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }


        public static Game LoadSavedGame(SavedGameData savedGame, Player humanPlayer, Player computerPlayer)
        {
            if (savedGame?.GameState == null || !savedGame.GameState.IsValid())
            {
                MessageBox.Show("Не удалось загрузить состояние игры", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            try
            {
                // Создаем новую игру
                var game = new Game(humanPlayer, computerPlayer, savedGame.GameMode)
                {
                    History = savedGame.History,
                    IsGameActive = savedGame.GameState.IsGameActive
                };

                // Применяем полное состояние
                savedGame.GameState.ApplyToGame(game, humanPlayer, computerPlayer);

                // Восстанавливаем историю
                game.History = savedGame.History;

                System.Diagnostics.Debug.WriteLine($"✅ Игра загружена: {savedGame.GameId}");
                return game;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки игры: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки игры: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public async Task SyncWithOpponent(SimpleNetworkManager networkManager)
        {
            if (networkManager != null && networkManager.IsConnected)
            {
                var gameState = GameState.FromGame(this);
                await networkManager.SendGameState(gameState);
            }
        }

        public void ApplyNetworkGameState(GameState gameState)
        {
            if (gameState == null) return;

            // Очищаем текущее состояние
            Deck.Clear();
            Table.Clear();
            HumanPlayer.Hand.Clear();
            ComputerPlayer.Hand.Clear();

            // Восстанавливаем колоду
            foreach (var card in gameState.Deck)
            {
                var newCard = new Card(card.Suit, card.Rank);
                newCard.IsTrump = (newCard.Suit == gameState.TrumpSuit);
                Deck.Add(newCard);
            }

            // Восстанавливаем стол
            foreach (var card in gameState.Table)
            {
                var newCard = new Card(card.Suit, card.Rank);
                newCard.IsTrump = (newCard.Suit == gameState.TrumpSuit);
                Table.Add(newCard);
            }

            // Восстанавливаем руки
            foreach (var card in gameState.Player1Hand)
            {
                var newCard = new Card(card.Suit, card.Rank);
                newCard.IsTrump = (newCard.Suit == gameState.TrumpSuit);
                HumanPlayer.Hand.Add(newCard);
            }

            foreach (var card in gameState.Player2Hand)
            {
                var newCard = new Card(card.Suit, card.Rank);
                newCard.IsTrump = (newCard.Suit == gameState.TrumpSuit);
                ComputerPlayer.Hand.Add(newCard);
            }

            // Устанавливаем остальные параметры
            TrumpSuit = gameState.TrumpSuit;
            IsGameActive = gameState.IsGameActive;
            Mode = gameState.Mode;

            // Устанавливаем текущих игроков
            if (gameState.CurrentAttackerIndex == 0)
            {
                CurrentAttacker = HumanPlayer;
                CurrentDefender = ComputerPlayer;
            }
            else
            {
                CurrentAttacker = ComputerPlayer;
                CurrentDefender = HumanPlayer;
            }

            // Сортируем карты
            HumanPlayer.SortHand();
            ComputerPlayer.SortHand();

            // Обновляем UI
            GameStateChanged?.Invoke();
        }

        public bool CanAttackWithCard(Card card)
        {
            if (Table.Count == 0)
                return true; // Первую карту можно ходить любой

            // Проверяем, что на столе есть карты такого же достоинства
            // Берем только атакующие карты (четные индексы)
            var attackingCardsOnTable = new List<Card>();
            for (int i = 0; i < Table.Count; i += 2)
            {
                if (i < Table.Count)
                    attackingCardsOnTable.Add(Table[i]);
            }

            var tableRanks = attackingCardsOnTable.Select(c => c.Rank).Distinct();
            return tableRanks.Contains(card.Rank);
        }
        public bool IsTrumpCardAvailable()
        {
            // Козырная карта доступна только когда в колоде осталась только она
            return Deck.Count == 1 && Deck[0].Suit == TrumpSuit;
        }
        private bool IsBeaten(Card card)
        {
            // Находим индекс карты на столе
            int index = Table.IndexOf(card);
            if (index == -1) return false;

            // Если карта атакующая (четный индекс), проверяем есть ли защитная карта после нее
            if (index % 2 == 0) // атакующая карта
            {
                // Есть ли защитная карта после этой?
                if (index + 1 < Table.Count)
                {
                    return true; // Карта побита
                }
            }

            return false; // Карта не побита
        }

        private void TakeCards(Player player)
        {
            if (Table.Count == 0 || player == null) return;

            // Игрок берет все карты со стола
            player.Hand.AddRange(Table);
            Table.Clear();

            // Сортируем карты
            player.SortHand();

            // Записываем в историю
            History.RecordSpecialMove(player, MoveType.TakeCards, "Взял карты со стола");

            // Завершаем раунд (неудачная защита)
            CompleteRound(false);

            // Обновляем UI
            GameStateChanged?.Invoke();
            GameMessage?.Invoke($"{player.Name} взял карты со стола");
        }

        public void Surrender()
        {
            if (IsGameActive)
            {
                EndGame(ComputerPlayer);
            }
        }
    }
}