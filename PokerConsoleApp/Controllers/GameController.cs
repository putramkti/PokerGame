using System.Net;
using System.Runtime.CompilerServices;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

using PokerConsoleApp.Models;
namespace PokerConsoleApp.Controllers;

public class GameController
{
    // private Dictionary<IPlayer, List<IChip>> _chips;
    private Dictionary<IPlayer, IChip> _chips;
    private Dictionary<IPlayer, List<ICard>> _holeCards;
    private Dictionary<IPlayer, int> _currentBets;
    private List<IPlayer> _players;
    private List<IPot> _pots;
    private IDeck _deck;
    private ITable _table;

    private int _playersToAct;
    private int _dealerIndex;
    private int _currentPlayerIndex;
    private int _lastRaiserIndex;
    private int _lastRaiseAmount;
    private int _currentHighestbet;
    private int _smallBlind;
    private int _bigBlind;

    private GameRound _currentRound;
    private GameState _gameState;
    public Action<GameRound>? OnRoundChanged;
    public Action<List<IPlayer>>? OnHandWinnersDecided;

    public GameController(int smallBlind, int bigBlind, List<IPlayer> players, Dictionary<IPlayer, IChip> chips, Dictionary<IPlayer, List<ICard>> holeCards, Dictionary<IPlayer, int> currentBet, List<IPot> pot, IDeck deck, ITable table)
    {
        _smallBlind = smallBlind;
        _bigBlind = bigBlind;

        _players = players;
        _chips = chips;
        _holeCards = holeCards;
        _currentBets = currentBet;
        _pots = pot;

        _deck = deck;
        _table = table;

        _gameState = GameState.WaitingForPlayers;
        _currentRound = GameRound.PreFlop;
    }

    public void AddPlayer(string name, int chips)
    {
        IPlayer newPlayer = new Player(name) { Status = PlayerStatus.Active };
        _players.Add(newPlayer);

        _chips[newPlayer] = new Chip(chips);
        _holeCards[newPlayer] = new List<ICard>();
        _currentBets[newPlayer] = 0;
    }

    public void StartGame()
    {
        _gameState = GameState.InProgress;

        Random random = new Random();
        _dealerIndex = random.Next(0, _players.Count);

        StartNewHand();
    }

    public void StartNexthand()
    {
        EliminateBustedPlayers();

        if (IsGameOver())
        {
            _gameState = GameState.GameOver;
            return;
        }

        RotateDealer();

        StartNewHand();
    }

    public GameState GetGameState()
    {
        return _gameState;

    }

    public GameRound GetCurrentRound()
    {
        return _currentRound;
    }
    public bool IsGameOver()
    {
        return _players.Count(p => p.Status != PlayerStatus.Bust) <= 1;
    }

    public IPlayer GetGameWinner()
    {
        return _players.First(p => p.Status != PlayerStatus.Bust);
    }

    public List<IPlayer> GetHandWinners()
    {
        List<IPlayer> activePlayers = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        return CompareHands(activePlayers);
    }

    public HandRank GetPlayerHandRank(IPlayer player)
    {
        return EvaluateHand(player);
    }

    public List<ICard> GetPlayerBestFiveCards(IPlayer player)
    {
        List<ICard> allSevenCards = _holeCards[player].Concat(_table.CommunityCards).ToList();
        HandEvaluation result = GetBestHandResult(player, allSevenCards);
        return result.BestFiveCards;
    }

    public IPlayer GetCurrentPlayer()
    {
        return _players[_currentPlayerIndex];
    }

    public List<IPlayer> GetAllPlayers()
    {
        return _players;
    }

    public IPlayer GetDealer()
    {
        return _players[_dealerIndex];
    }

    public PlayerStatus GetPlayerStatus(IPlayer player)
    {
        return player.Status;
    }

    public List<ICard> GetPlayerHoleCards(IPlayer player)
    {
        return _holeCards[player];
    }

    public int GetPlayerChips(IPlayer player)
    {
        return _chips.ContainsKey(player) ? _chips[player].Amount : 0;
    }

    public int GetPlayerCurrentBet(IPlayer player)
    {
        return _currentBets.ContainsKey(player) ? _currentBets[player] : 0;
    }

    public int GetSmallBind()
    {
        return _smallBlind;
    }

    public int GetBigBlind()
    {
        return _bigBlind;
    }

    public int GetCurrentHighestBet()
    {
        return _currentHighestbet;
    }

    public int GetCallAmount(IPlayer player)
    {
        return _currentHighestbet - GetPlayerCurrentBet(player);
    }

    public int GetMinRaise()
    {
        return _currentHighestbet + _lastRaiseAmount;
    }

    public List<BettingAction> GetAvailableBettingAction(IPlayer player)
    {
        List<BettingAction> actions = new List<BettingAction>();

        bool isNotCurrentPlayerTurn = player != GetCurrentPlayer() || player.Status != PlayerStatus.Active;
        if (isNotCurrentPlayerTurn)
        {
            return actions;
        }

        int callAmount = GetCallAmount(player);
        int playerChips = GetPlayerChips(player);

        actions.Add(BettingAction.Fold);

        if (callAmount == 0)
        {
            actions.Add(BettingAction.Check);
        }
        else if (playerChips >= callAmount)
        {
            actions.Add(BettingAction.Call);
        }

        int minRaise = GetMinRaise();
        bool canRaise = playerChips + GetPlayerCurrentBet(player) >= minRaise;
        if (canRaise)
        {
            actions.Add(BettingAction.Raise);
        }

        actions.Add(BettingAction.AllIn);
        return actions;
    }


    public List<ICard> GetCommunityCards()
    {
        return _table.CommunityCards;
    }

    public IPot? GetMainPot()
    {
        return _pots.FirstOrDefault();
    }

    public List<IPot> GetSidePots()
    {
        return _pots.Skip(1).ToList();
    }

    public int GetTotalPotAmount()
    {
        return _pots.Sum(p => p.TotalChips) + _currentBets.Values.Sum();
    }

    public void Fold(IPlayer player)
    {
        player.Status = PlayerStatus.Folded;
        NextPlayer();
    }

    public void Check(IPlayer player)
    {
        NextPlayer();
    }

    public void Call(IPlayer player)
    {
        int callAmount = GetCallAmount(player);

        int chipsToBet = Math.Min(callAmount, _chips[player].Amount);

        _chips[player].Amount -= chipsToBet;
        _currentBets[player] += chipsToBet;

        bool isPlayerAllIn = _chips[player].Amount == 0;

        if (isPlayerAllIn)
        {
            player.Status = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void Raise(IPlayer player, int amount)
    {
        int minRaise = _currentHighestbet + _lastRaiseAmount;
        int maxBet = _chips[player].Amount + _currentBets[player];

        int additionalBet = amount - _currentBets[player];

        _chips[player].Amount -= additionalBet;
        _currentBets[player] = amount;

        _lastRaiseAmount = amount - _currentHighestbet;
        _currentHighestbet = amount;
        _lastRaiserIndex = _currentPlayerIndex;

        _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active);

        bool isPlayerAllIn = _chips[player].Amount == 0;

        if (isPlayerAllIn)
        {
            player.Status = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void AllIn(IPlayer player)
    {
        int allInAmount = _chips[player].Amount + _currentBets[player];

        bool isAllInRaise = allInAmount > _currentHighestbet;
        if (isAllInRaise)
        {
            _lastRaiseAmount = allInAmount - _currentHighestbet;
            _currentHighestbet = allInAmount;
            _lastRaiserIndex = _currentPlayerIndex;
            _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active);
        }

        _currentBets[player] = allInAmount;
        _chips[player].Amount = 0;
        player.Status = PlayerStatus.AllIn;

        NextPlayer();
    }

    private void StartNewHand()
    {
        _currentRound = GameRound.PreFlop;
        _gameState = GameState.InProgress;

        // _table.CommunityCards.Clear();
        _pots.Clear();
        _pots.Add(new Pot());

        foreach (IPlayer player in _players)
        {
            _currentBets[player] = 0;
            // _holeCards[player].Clear();
            if (player.Status != PlayerStatus.Bust)
            {
                player.Status = PlayerStatus.Active;
            }
        }

        CollectCommunityCardsToDeck();
        CollectHoleCardsToDeck();

        ShuffleDeck();
        PostBlinds();
        DealHoleCards();

        _currentPlayerIndex = GetNextActivePlayerIndex(_lastRaiserIndex);
        RunBettingRound();
    }

    private void CollectCommunityCardsToDeck()
    {
        foreach (ICard card in _table.CommunityCards)
        {
            _deck.Cards.Add(card);
        }
        _table.CommunityCards.Clear();
    }

    private void CollectHoleCardsToDeck()
    {
        foreach (IPlayer player in _players)
        {
            foreach (ICard card in _holeCards[player])
            {
                _deck.Cards.Add(card);
            }
            _holeCards[player].Clear();
        }
    }
    private void ShuffleDeck()
    {
        // _deck = new Deck();
        Random random = new Random();
        _deck.Cards = _deck.Cards.OrderBy(c => random.Next()).ToList();

    }

    private void RotateDealer()
    {
        bool isDealerBust = true;
        do
        {
            _dealerIndex = (_dealerIndex + 1) % _players.Count();
            isDealerBust = _players[_dealerIndex].Status == PlayerStatus.Bust;
        } while (isDealerBust);
    }

    

    private void PostBlinds()
    {
        int sbIndex = GetNextActivePlayerIndex(_dealerIndex);
        int bbIndex = GetNextActivePlayerIndex(sbIndex);

        IPlayer sbPlayer = _players[sbIndex];
        int sbTax = Math.Min(_smallBlind, _chips[sbPlayer].Amount);
        _chips[sbPlayer].Amount -= sbTax;
        _currentBets[sbPlayer] = sbTax;

        if (_chips[sbPlayer].Amount == 0)
        {
            sbPlayer.Status = PlayerStatus.AllIn;
        }

        IPlayer bbPlayer = _players[bbIndex];
        int bbTax = Math.Min(_bigBlind, _chips[bbPlayer].Amount);
        _chips[bbPlayer].Amount -= bbTax;
        _currentBets[bbPlayer] = bbTax;

        if (_chips[bbPlayer].Amount == 0)
        {
            bbPlayer.Status = PlayerStatus.AllIn;
        }

        _currentHighestbet = Math.Max(sbTax, bbTax);
        _lastRaiseAmount = Math.Max(bbTax - sbTax, _bigBlind - _smallBlind);
        _lastRaiserIndex = bbIndex;

        if (_lastRaiseAmount <= 0)
        {
            _lastRaiseAmount = _bigBlind - _smallBlind;
        }


        _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active);
    }

    private int GetNextActivePlayerIndex(int currentIndex)
    {
        int nextactivePlayerIndex = (currentIndex + 1) % _players.Count;
        int start = nextactivePlayerIndex;
        bool isnotActive = _players[nextactivePlayerIndex].Status != PlayerStatus.Active;

        while (isnotActive)
        {
            nextactivePlayerIndex = (nextactivePlayerIndex + 1) % _players.Count;
            isnotActive = _players[nextactivePlayerIndex].Status != PlayerStatus.Active;
        }
        return nextactivePlayerIndex;
    }

    private void DealHoleCards()
    {
        for (int i = 0; i < 2; i++)
        {
            foreach (IPlayer player in _players.Where(p => p.Status == PlayerStatus.Active))
            {
                _holeCards[player].Add(DealCard());
            }
        }
    }

    private ICard DealCard()
    {
        ICard card = _deck.Cards[0];
        _deck.Cards.RemoveAt(0);
        return card;
    }

    private void DealFlop()
    {
        DealCard();

        _table.CommunityCards.Add(DealCard());
        _table.CommunityCards.Add(DealCard());
        _table.CommunityCards.Add(DealCard());
    }

    private void DealTurn()
    {
        DealCard();

        _table.CommunityCards.Add(DealCard());
    }

    private void DealRiver()
    {
        DealCard();

        _table.CommunityCards.Add(DealCard());
    }

    private void RunBettingRound()
    {
        if (IsBettingRoundOver())
        {
            CollectBetsToPot();
            TransitionToNextRound();
        }
    }

    private void NextPlayer()
    {
        _playersToAct--;
        if (IsBettingRoundOver())
        {
            CollectBetsToPot();
            TransitionToNextRound();
            return;
        }

        _currentPlayerIndex = GetNextActivePlayerIndex(_currentPlayerIndex);
    }

    private bool IsBettingRoundOver()
    {
        List<IPlayer> activePlayers = _players.Where(p => p.Status == PlayerStatus.Active).ToList();

        List<IPlayer> nonFoldedPlayers = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();

        if (nonFoldedPlayers.Count <= 1)
        {
            return true;
        }

        if (activePlayers.Count == 0)
        {

            return true;
        }

        // return activePlayers.All(p => _currentBets[p] == _currentHighestbet) && _currentPlayerIndex == _lastRaiserIndex;
        return _playersToAct <= 0 && activePlayers.All(p => _currentBets[p] == _currentHighestbet);

    }

    private void CollectBetsToPot()
    {
        CreateSidePots();

        foreach (IPlayer key in _currentBets.Keys.ToList())
        {
            _currentBets[key] = 0;
        }
    }

    private void TransitionToNextRound()
    {
        bool isOnlyOneActivePlayer = _players.Count(p => p.Status == PlayerStatus.Active) <= 1;
        if (isOnlyOneActivePlayer)
        {
            while (_table.CommunityCards.Count < 5)
            {
                if (_table.CommunityCards.Count == 0)
                {
                    DealFlop();
                }
                else if (_table.CommunityCards.Count == 3)
                {
                    DealTurn();
                }
                else if (_table.CommunityCards.Count == 4)
                {
                    DealRiver();
                }
            }

            RunShowdown();
            return;
        }


        _currentHighestbet = 0;
        _lastRaiseAmount = _bigBlind;
        // _lastRaiserIndex = -1;

        _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active);

        _currentPlayerIndex = GetNextActivePlayerIndex(_dealerIndex);

        switch (_currentRound)
        {
            case GameRound.PreFlop:
                _currentRound = GameRound.Flop;
                DealFlop();
                break;
            case GameRound.Flop:
                _currentRound = GameRound.Turn;
                DealTurn();
                break;
            case GameRound.Turn:
                _currentRound = GameRound.River;
                DealRiver();
                break;
            case GameRound.River:
                _currentRound = GameRound.Showdown;
                RunShowdown();
                break;
        }

        OnRoundChanged?.Invoke(_currentRound);

    }

    private void RunShowdown()
    {
        _currentRound = GameRound.Showdown;
        _gameState = GameState.HandComplete;

        List<IPlayer> survivors = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        if (survivors.Count == 1)
        {
            ResolveIfOnePlayerLeft();
        }
        else
        {
            AwardPot();
        }
    }

    private void ResolveIfOnePlayerLeft()
    {
        List<IPlayer> survivors = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        if (survivors.Count == 1)
        {
            int uncollected = _currentBets.Values.Sum();
            _chips[survivors[0]].Amount += _pots.Sum(p => p.TotalChips) + uncollected;

            foreach (IPlayer key in _currentBets.Keys.ToList())
            {
                _currentBets[key] = 0;
            }

            OnHandWinnersDecided?.Invoke(survivors);
        }
    }

    private List<IPlayer> CompareHands(List<IPlayer> players)
    {
        if (players == null || players.Count == 0)
        {
            return new List<IPlayer>();
        }

        List<HandEvaluation> handResults = new List<HandEvaluation>();

        foreach (IPlayer player in players)
        {
            List<ICard> allSevenCards = _holeCards[player].Concat(_table.CommunityCards).ToList();

            HandEvaluation result = GetBestHandResult(player, allSevenCards);

            handResults.Add(result);
        }

        List<HandEvaluation> sortedResult = handResults.OrderByDescending(r => r).ToList();

        List<IPlayer> winners = new List<IPlayer>();

        if (sortedResult.Count > 0)
        {
            HandEvaluation bestResult = sortedResult[0];
            winners.Add(bestResult.Player);

            for (int i = 1; i < sortedResult.Count; i++)
            {
                if (sortedResult[i].CompareTo(bestResult) == 0)
                {
                    winners.Add(sortedResult[i].Player);
                }
                else
                {
                    break;
                }
            }
        }

        return winners;
    }
    private HandRank EvaluateHand(IPlayer player)
    {
        List<ICard> allSevenCard = _holeCards[player].Concat(_table.CommunityCards).ToList();
        HandEvaluation result = GetBestHandResult(player, allSevenCard);

        return result.HandRank;

    }

    private HandEvaluation GetBestHandResult(IPlayer player, List<ICard> sevenCards)
    {

        List<ICard> orderedCards = sevenCards.OrderByDescending(c => c.Rank).ToList();

        List<IGrouping<CardRank, ICard>> rankGroups = orderedCards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        IGrouping<CardSuit, ICard>? suitGroups = orderedCards.GroupBy(c => c.Suit).Where(g => g.Count() >= 5).FirstOrDefault();

        bool isFlush = suitGroups != null;
        if (isFlush)
        {
            List<ICard> flushCards = suitGroups.OrderByDescending(c => c.Rank).ToList();

            List<ICard> straightFlushCards = GetStraightSequence(flushCards);

            bool isStraightFlush = straightFlushCards != null && straightFlushCards.Count > 0;
            if (isStraightFlush)
            {
                bool isRoyalFlush = straightFlushCards[0].Rank == CardRank.Ace;
                if (isRoyalFlush)
                {
                    return new HandEvaluation(player, HandRank.RoyalFlush, straightFlushCards);
                }
                return new HandEvaluation(player, HandRank.StraightFlush, straightFlushCards);
            }

            return new HandEvaluation(player, HandRank.Flush, flushCards.Take(5).ToList());
        }

        bool isFourOfAKind = rankGroups[0].Count() == 4;
        if (isFourOfAKind)
        {
            List<ICard> bestFive = rankGroups[0].ToList();

            ICard kicker = orderedCards.First(c => c.Rank != rankGroups[0].Key);
            bestFive.Add(kicker);
            return new HandEvaluation(player, HandRank.FourOfAKind, bestFive);
        }

        bool isFullHouse = rankGroups[0].Count() == 3 && rankGroups.Count > 1 && rankGroups[1].Count() >= 2;
        if (isFullHouse)
        {
            List<ICard> bestFive = rankGroups[0].ToList();

            bestFive.AddRange(rankGroups[1].Take(2));
            return new HandEvaluation(player, HandRank.FullHouse, bestFive);
        }

        List<ICard> straightCards = GetStraightSequence(orderedCards);
        bool isStraight = straightCards != null && straightCards.Count > 0;
        if (isStraight)
        {
            return new HandEvaluation(player, HandRank.Straight, straightCards);
        }

        bool isThreeOfAKind = rankGroups[0].Count() == 3;
        if (isThreeOfAKind)
        {
            List<ICard> bestFive = rankGroups[0].ToList();

            IEnumerable<ICard> kickers = orderedCards.Where(c => c.Rank != rankGroups[0].Key).Take(2);
            bestFive.AddRange(kickers);
            return new HandEvaluation(player, HandRank.ThreeOfAKind, bestFive);
        }

        bool isTwoPair = rankGroups[0].Count() == 2 && rankGroups.Count > 1 && rankGroups[1].Count() == 2;
        if (isTwoPair)
        {
            List<ICard> bestFive = rankGroups[0].ToList();
            bestFive.AddRange(rankGroups[1].ToList());

            ICard kicker = orderedCards.First(c => c.Rank != rankGroups[0].Key && c.Rank != rankGroups[1].Key);
            bestFive.Add(kicker);
            return new HandEvaluation(player, HandRank.TwoPair, bestFive);
        }

        bool isOnePair = rankGroups[0].Count() == 2;
        if (isOnePair)
        {
            List<ICard> bestFive = rankGroups[0].ToList();

            IEnumerable<ICard> kickers = orderedCards.Where(c => c.Rank != rankGroups[0].Key).Take(3);
            bestFive.AddRange(kickers);
            return new HandEvaluation(player, HandRank.OnePair, bestFive);
        }

        return new HandEvaluation(player, HandRank.HighCard, orderedCards.Take(5).ToList());
    }

    private List<ICard> GetStraightSequence(List<ICard> cards)
    {
        List<ICard> uniqueCards = cards.GroupBy(c => c.Rank).Select(g => g.First()).OrderByDescending(c => c.Rank).ToList();

        if (uniqueCards.Count < 5)
        {
            return new List<ICard>();
        }

        for (int i = 0; i <= uniqueCards.Count - 5; i++)
        {
            if ((int)uniqueCards[i].Rank - (int)uniqueCards[i + 4].Rank == 4)
            {
                return uniqueCards.Skip(i).Take(5).ToList();
            }
        }

        bool hasAce = uniqueCards.Any(c => c.Rank == CardRank.Ace);
        bool hasTwo = uniqueCards.Any(c => c.Rank == CardRank.Two);
        bool hasThree = uniqueCards.Any(c => c.Rank == CardRank.Three);
        bool hasFour = uniqueCards.Any(c => c.Rank == CardRank.Four);
        bool hasFive = uniqueCards.Any(c => c.Rank == CardRank.Five);

        bool isWheelStraight = hasAce && hasTwo && hasThree && hasFour && hasFive;

        if (isWheelStraight)
        {
            List<ICard> wheelCards = new List<ICard>()
            {
                uniqueCards.First(c => c.Rank == CardRank.Five),
                uniqueCards.First(c => c.Rank == CardRank.Four),
                uniqueCards.First(c => c.Rank == CardRank.Three),
                uniqueCards.First(c => c.Rank == CardRank.Two),
                uniqueCards.First(c => c.Rank == CardRank.Ace)
            };

            return wheelCards;
        }
        return new List<ICard>();
    }

    private void CreateSidePots()
    {
        Dictionary<IPlayer, int> grandTotal = _players.ToDictionary(p => p, p => 0);

        foreach (IPot pot in _pots)
        {
            foreach (KeyValuePair<IPlayer, int> kvp in pot.Contributions)
            {
                grandTotal[kvp.Key] += kvp.Value;
            }
        }

        foreach (KeyValuePair<IPlayer, int> kvp in _currentBets)
        {
            grandTotal[kvp.Key] += kvp.Value;
        }

        _pots.Clear();

        List<IPlayer> contributors = _players.Where(p => grandTotal[p] > 0).ToList();

        if (contributors.Count == 0)
        {
            foreach (IPlayer key in _currentBets.Keys.ToList())
            {
                _currentBets[key] = 0;
            }
            return;
        }

        List<int> allInLevels = contributors
            .Select(p => grandTotal[p])
            .Distinct()
            .OrderBy(amount => amount)
            .ToList();

        List<IPlayer> eligible = new List<IPlayer>(contributors);
        // batas minimal setiap level pots
        int previousLevel = 0;

        foreach (int level in allInLevels)
        {
            int sliceSize = level - previousLevel;
            bool isSliceValid = sliceSize > 0 && eligible.Count > 0;

            if (isSliceValid)
            {
                IPot pot = new Pot();

                foreach (IPlayer player in eligible)
                {
                    int playerTotal = grandTotal[player];
                    int contributionToThissSlice = Math.Min(sliceSize, Math.Max(0, playerTotal - previousLevel));
                    if (contributionToThissSlice > 0)
                    {
                        pot.Contributions[player] = contributionToThissSlice;
                    }
                }
                pot.TotalChips = pot.Contributions.Values.Sum();

                if (pot.TotalChips > 0)
                {
                    _pots.Add(pot);
                }
            }

            previousLevel = level;


            eligible.RemoveAll(p => grandTotal[p] <= level);
        }

        foreach (IPlayer key in _currentBets.Keys.ToList())
        {
            _currentBets[key] = 0;
        }

    }

    private void AwardPot()
    {

        if (_pots.Count == 0)
        {
            return;
        }
        List<IPlayer> allWinners = new List<IPlayer>();


        foreach (IPot pot in _pots)
        {
            bool IsEligiblePlayer(IPlayer p) => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust;
            List<IPlayer> eligibleActive = pot.Contributions
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .Where(IsEligiblePlayer)
                .ToList();

            if (eligibleActive.Count == 0)
            {
                continue;
            }

            List<IPlayer> winners = CompareHands(eligibleActive);
            if (winners.Count == 0)
            {
                continue;
            }

            int share = pot.TotalChips / winners.Count;
            int remainder = pot.TotalChips % winners.Count;

            int firstToActSeat = GetNextEligiblePlayerIndex(_dealerIndex);
            List<IPlayer> orderedWinners = winners.OrderBy(p =>
                {
                    int idx = _players.IndexOf(p);
                    int distance = idx - firstToActSeat;
                    if (distance < 0)
                    {
                        distance += _players.Count;
                    }
                    return distance;
                }).ToList();

            for (int i = 0; i < orderedWinners.Count; i++)
            {
                int amount = share + (i < remainder ? 1 : 0);
                _chips[orderedWinners[i]].Amount += amount;
            }
            allWinners.AddRange(orderedWinners);
        }

        if (allWinners.Count > 0)
        {
            List<IPlayer> uniqueWinners = allWinners.Distinct().ToList();
            OnHandWinnersDecided?.Invoke(uniqueWinners);
        }
    }

    private int GetNextEligiblePlayerIndex(int currentIndex)
    {
        int next = (currentIndex + 1) % _players.Count;
        int start = next;

        bool isEliminatedPlayer = _players[next].Status == PlayerStatus.Bust || _players[next].Status == PlayerStatus.Folded;
        while (isEliminatedPlayer)
        {
            next = (next + 1) % _players.Count;
            if (next == start)
            {
                return currentIndex;
            }
            isEliminatedPlayer = _players[next].Status == PlayerStatus.Bust || _players[next].Status == PlayerStatus.Folded;
        }
        return next;
    }

    private void EliminateBustedPlayers()
    {
        foreach (IPlayer player in _players)
        {
            if (_chips[player].Amount <= 0)
            {
                player.Status = PlayerStatus.Bust;
            }
        }
    }

}
