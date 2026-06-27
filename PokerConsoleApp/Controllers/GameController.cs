using System.Net;
using System.Runtime.CompilerServices;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

using PokerConsoleApp.Models;
namespace PokerConsoleApp.Controllers;

public class GameController
{
    // private Dictionary<IPlayer, List<IChip>> _chips;
    private Dictionary<IPlayer, int> _chips;
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

    public Action<GameRound> OnRoundChanged;
    public Action<List<IPlayer>> OnHandWinnersDecided;

    public GameController(int smallBlind, int bigBlind)
    {
        _smallBlind = smallBlind;
        _bigBlind = bigBlind;

        _players = new List<IPlayer>();
        // _chips = new Dictionary<IPlayer, List<IChip>>();
        _chips = new Dictionary<IPlayer, int>();
        _holeCards = new Dictionary<IPlayer, List<ICard>>();
        _currentBets = new Dictionary<IPlayer, int>();
        _pots = new List<IPot>();

        _deck = new Deck();
        _table = new Table();

        _gameState = GameState.WaitingForPlayers;
        _currentRound = GameRound.PreFlop;
    }

    public void AddPlayer(string name, int chips)
    {
        IPlayer newPlayer = new Player(name) { Status = PlayerStatus.Active };
        _players.Add(newPlayer);

        // IChip newChips = new Chip(chips);
        _chips[newPlayer] = chips;
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
        if (_gameState != GameState.HandComplete && _gameState != GameState.InProgress)
        {
            throw new InvalidOperationException("Belum bisa mulai hand baru, hand sebelumnya belum selesai");
        }

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
        var activePlayers = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        return CompareHands(activePlayers);
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
        return _chips[player];
    }

    public int GetPlayerTotalChips(IPlayer player)
    {
        return _chips.ContainsKey(player) ? _chips[player] : 0;
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
        var actions = new List<BettingAction>();

        if (player != GetCurrentPlayer() || player.Status != PlayerStatus.Active)
        {
            return actions;
        }

        int callAmount = GetCallAmount(player);
        int playerChips = GetPlayerTotalChips(player);

        actions.Add(BettingAction.Fold);

        if (callAmount == 0)
        {
            actions.Add(BettingAction.Check);
        }
        else if (playerChips >= callAmount)
        {
            actions.Add(BettingAction.Call);
        }

        if (playerChips > callAmount)
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

    public IPot GetMainPot()
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

        int chipsToBet = Math.Min(callAmount, _chips[player]);

        _chips[player] -= chipsToBet;
        _currentBets[player] += chipsToBet;

        if (_chips[player] == 0)
        {
            player.Status = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void Raise(IPlayer player, int amount)
    {
        int minRaise = _currentHighestbet + _lastRaiseAmount;
        int maxBet = _chips[player] + _currentBets[player];

        if (amount < minRaise)
            throw new InvalidOperationException($"Raise minimal adalah {minRaise}");
        if (amount > maxBet)
            throw new InvalidOperationException("Chips tidak cukup");

        int additionalBet = amount - _currentBets[player];

        _chips[player] -= additionalBet;
        _currentBets[player] = amount;

        _lastRaiseAmount = amount - _currentHighestbet;
        _currentHighestbet = amount;
        _lastRaiserIndex = _currentPlayerIndex;

        _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active) - 1;

        if (_chips[player] == 0)
        {
            player.Status = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void AllIn(IPlayer player)
    {
        int allInAmount = _chips[player] + _currentBets[player];

        if (allInAmount > _currentHighestbet)
        {
            _lastRaiseAmount = allInAmount - _currentHighestbet;
            _currentHighestbet = allInAmount;
            _lastRaiserIndex = _currentPlayerIndex;
            _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active) - 1;
        }

        _currentBets[player] = allInAmount;
        _chips[player] = 0;
        player.Status = PlayerStatus.AllIn;

        NextPlayer();
    }

    private void StartNewHand()
    {
        _currentRound = GameRound.PreFlop;
        _gameState = GameState.InProgress;

        _table.CommunityCards.Clear();
        _pots.Clear();
        _pots.Add(new Pot());

        foreach (var player in _players)
        {
            _currentBets[player] = 0;
            _holeCards[player].Clear();
            if (player.Status != PlayerStatus.Bust)
            {
                player.Status = PlayerStatus.Active;
            }
        }

        ShuffleDeck();
        PostBlinds();
        DealHoleCards();

        _currentPlayerIndex = GetNextActivePlayerIndex(_lastRaiserIndex);
        RunBettingRound();

    }

    private void ShuffleDeck()
    {
        _deck = new Deck();
        Random random = new Random();
        _deck.Cards = _deck.Cards.OrderBy(c => random.Next()).ToList();

    }

    private void RotateDealer()
    {
        do
        {
            _dealerIndex = (_dealerIndex + 1) % _players.Count();
        } while (_players[_dealerIndex].Status == PlayerStatus.Bust);
    }

    private void PostBlinds()
    {
        // TODO: tambahkan skip bust/fold player
        int sbIndex = GetNextActivePlayerIndex(_dealerIndex);
        int bbIndex = GetNextActivePlayerIndex(sbIndex);

        var sbPlayer = _players[sbIndex];
        int sbTax = Math.Min(_smallBlind, _chips[sbPlayer]);
        _chips[sbPlayer] -= sbTax;
        _currentBets[sbPlayer] = sbTax;
        if (_chips[sbPlayer] == 0) sbPlayer.Status = PlayerStatus.AllIn;

        var bbPlayer = _players[bbIndex];
        int bbTax = Math.Min(_bigBlind, _chips[bbPlayer]);
        _chips[bbPlayer] -= bbTax;
        _currentBets[bbPlayer] = bbTax;
        if (_chips[bbPlayer] == 0) bbPlayer.Status = PlayerStatus.AllIn;

        // TODO: CEK apakah sesuai aturan resmi poker?

        _currentHighestbet = bbTax < sbTax ? bbTax : _bigBlind;
        _lastRaiseAmount = _bigBlind;
        _lastRaiserIndex = bbIndex;

        _playersToAct = _players.Count(p => p.Status == PlayerStatus.Active);
    }

    private int GetNextActivePlayerIndex(int currentIndex)
    {
        int nextactivePlayerIndex = (currentIndex + 1) % _players.Count;
        while (_players[nextactivePlayerIndex].Status != PlayerStatus.Active)
        {
            nextactivePlayerIndex = (nextactivePlayerIndex + 1) % _players.Count;
        }
        return nextactivePlayerIndex;
    }

    private void DealHoleCards()
    {
        for (int i = 0; i < 2; i++)
        {
            foreach (var player in _players.Where(p => p.Status == PlayerStatus.Active))
            {
                _holeCards[player].Add(DealCard());
            }
        }
    }

    private ICard DealCard()
    {
        if (_deck.Cards.Count == 0)
        {
            throw new InvalidOperationException("Kartu di deck habis!");
        }

        ICard card = _deck.Cards[0];
        _deck.Cards.RemoveAt(0);
        return card;
    }

    private void DealFlop()
    {
        //TODO: coba riset lagi apakah perlu kartunya di burn
        DealCard();

        _table.CommunityCards.Add(DealCard());
        _table.CommunityCards.Add(DealCard());
        _table.CommunityCards.Add(DealCard());
    }

    private void DealTurn()
    {
        //TODO: coba riset lagi apakah perlu kartunya di burn
        DealCard();

        _table.CommunityCards.Add(DealCard());
    }

    private void DealRiver()
    {
        //TODO: coba riset lagi apakah perlu kartunya di burn
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
        //     do
        //     {
        //         _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count();
        //     } while (_players[_currentPlayerIndex].Status != PlayerStatus.Active);
    }

    private bool IsBettingRoundOver()
    {
        List<IPlayer> activePlayers = _players.Where(p => p.Status == PlayerStatus.Active).ToList();

        if (activePlayers.Count <= 1)
        {

            return true;
        }

        // return activePlayers.All(p => _currentBets[p] == _currentHighestbet) && _currentPlayerIndex == _lastRaiserIndex;
        return _playersToAct <= 0 && activePlayers.All(p => _currentBets[p] == _currentHighestbet);

    }

    private void CollectBetsToPot()
    {
        // TODO: nanti cek lagi apakah sudah ada fungsi untuk kapan harus create side pots
        CreateSidePots();

        foreach (var key in _currentBets.Keys.ToList())
        {
            _currentBets[key] = 0;
        }
    }

    private void TransitionToNextRound()
    {
        if (_players.Count(p => p.Status == PlayerStatus.Active) <= 1)
        {
            while (_table.CommunityCards.Count < 5)
            {
                if (_table.CommunityCards.Count == 0)
                    DealFlop();
                else if (_table.CommunityCards.Count == 3)
                    DealTurn();
                else if (_table.CommunityCards.Count == 4)
                    DealRiver();
            }

            RoundShowdown();
            return;
        }


        _currentHighestbet = 0;
        _lastRaiseAmount = _bigBlind;
        _lastRaiserIndex = -1;

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
                RoundShowdown();
                break;
        }

    }

    private void RoundShowdown()
    {
        _currentRound = GameRound.Showdown;
        _gameState = GameState.HandComplete;

        // CreateSidePots();

        ResolveIfOnePlayerLeft();

        AwardPot();

    }

    private void ResolveIfOnePlayerLeft()
    {
        List<IPlayer> survivors = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        if (survivors.Count == 1)
        {
            int uncollected = _currentBets.Values.Sum();
            _chips[survivors[0]] += _pots.Sum(p => p.TotalChips) + uncollected;
            _pots.Clear();

            foreach (var key in _currentBets.Keys.ToList())
            {
                _currentBets[key] = 0;
            }
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

            var result = GetBestHandResult(player, allSevenCards);

            handResults.Add(result);
        }

        var sortedResult = handResults.OrderByDescending(r => r).ToList();

        List<IPlayer> winners = new List<IPlayer>();

        if (sortedResult.Count > 0)
        {
            var bestResult = sortedResult[0];
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
        // TODO: nanti tambahkan algoritma kombinasi kartu
        List<ICard> allSevenCard = _holeCards[player].Concat(_table.CommunityCards).ToList();
        var result = GetBestHandResult(player, allSevenCard);

        return result.HandRank;

    }

    private HandEvaluation GetBestHandResult(IPlayer player, List<ICard> sevenCards)
    {

        List<ICard> orderedCards = sevenCards.OrderByDescending(c => c.Rank).ToList();

        var rankGroups = orderedCards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        var suitGroups = orderedCards.GroupBy(c => c.Suit).Where(g => g.Count() >= 5).FirstOrDefault();

        bool isFlush = suitGroups != null;

        if (isFlush)
        {
            var flushCards = suitGroups.OrderByDescending(c => c.Rank).ToList();

            var straightFlushCards = GetStraightSequence(flushCards);

            if (straightFlushCards != null)
            {
                if (straightFlushCards[0].Rank == CardRank.Ace)
                {
                    return new HandEvaluation() { Player = player, HandRank = HandRank.RoyalFlush, BestFiveCards = straightFlushCards };
                }
                return new HandEvaluation() { Player = player, HandRank = HandRank.StraightFlush, BestFiveCards = straightFlushCards };
            }

            return new HandEvaluation() { Player = player, HandRank = HandRank.Flush, BestFiveCards = flushCards.Take(5).ToList() };
        }

        // four of a kind
        if (rankGroups[0].Count() == 4)
        {
            var bestFive = rankGroups[0].ToList();

            var kicker = orderedCards.First(c => c.Rank != rankGroups[0].Key);
            bestFive.Add(kicker);
            return new HandEvaluation { Player = player, HandRank = HandRank.FourOfAKind, BestFiveCards = bestFive };
        }

        // fullhouse (3kembar + 2lembar)
        if (rankGroups[0].Count() == 3 && rankGroups.Count > 1 && rankGroups[1].Count() >= 2)
        {
            var bestFive = rankGroups[0].ToList();

            bestFive.AddRange(rankGroups[1].Take(2));
            return new HandEvaluation { Player = player, HandRank = HandRank.FullHouse, BestFiveCards = bestFive };
        }

        //straight
        var straightCards = GetStraightSequence(orderedCards);
        if (straightCards != null)
        {
            return new HandEvaluation { Player = player, HandRank = HandRank.Straight, BestFiveCards = straightCards };
        }

        // three of a kind
        if (rankGroups[0].Count() == 3)
        {
            var bestFive = rankGroups[0].ToList();

            var kickers = orderedCards.Where(c => c.Rank != rankGroups[0].Key).Take(2);
            bestFive.AddRange(kickers);
            return new HandEvaluation { Player = player, HandRank = HandRank.ThreeOfAKind, BestFiveCards = bestFive };
        }

        // two pair
        if (rankGroups[0].Count() == 2 && rankGroups.Count > 1 && rankGroups[1].Count() == 2)
        {
            var bestFive = rankGroups[0].ToList();
            bestFive.AddRange(rankGroups[1].ToList());

            var kicker = orderedCards.First(c => c.Rank != rankGroups[0].Key && c.Rank != rankGroups[1].Key);
            bestFive.Add(kicker);
            return new HandEvaluation { Player = player, HandRank = HandRank.TwoPair, BestFiveCards = bestFive };
        }

        // one pair
        if (rankGroups[0].Count() == 2)
        {
            var bestFive = rankGroups[0].ToList();

            var kickers = orderedCards.Where(c => c.Rank != rankGroups[0].Key).Take(3);
            bestFive.AddRange(kickers);
            return new HandEvaluation { Player = player, HandRank = HandRank.OnePair, BestFiveCards = bestFive };
        }

        return new HandEvaluation { Player = player, HandRank = HandRank.HighCard, BestFiveCards = orderedCards.Take(5).ToList() };
    }

    private List<ICard> GetStraightSequence(List<ICard> cards)
    {
        List<ICard> uniqueCards = cards.GroupBy(c => c.Rank).Select(g => g.FirstOrDefault()).OrderByDescending(c => c.Rank).ToList();

        if (uniqueCards.Count < 5)
        {
            return null;
        }

        for (int i = 0; i <= uniqueCards.Count - 5; i++)
        {
            if ((int)uniqueCards[i].Rank - (int)uniqueCards[i + 4].Rank == 4)
            {
                return uniqueCards.Skip(i).Take(5).ToList();
            }
        }

        //  Well straight
        if (uniqueCards.Any(c => c.Rank == CardRank.Ace) &&
            uniqueCards.Any(c => c.Rank == CardRank.Two) &&
            uniqueCards.Any(c => c.Rank == CardRank.Three) &&
            uniqueCards.Any(c => c.Rank == CardRank.Four) &&
            uniqueCards.Any(c => c.Rank == CardRank.Five))
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
        return null;
    }

    private void CreateSidePots()
    {
        // int totalCollected = _currentBets.Values.Sum();
        // if (totalCollected > 0)
        // {
        //     IPot pot = new Pot();
        //     pot.TotalChips = totalCollected;
        //     _pots.Add(pot);
        // }

        Dictionary<IPlayer, int> grandTotal = _players.ToDictionary(p => p, p => 0);

        foreach (IPot pot in _pots)
        {
            foreach (var kvp in pot.Contributions)
            {
                grandTotal[kvp.Key] += kvp.Value;
            }
        }

        foreach (var kvp in _currentBets)
        {
            grandTotal[kvp.Key] += kvp.Value;
        }

        _pots.Clear();

        var contributors = _players.Where(p => grandTotal[p] > 0).ToList();

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
        int previousLevel = 0;

        foreach (int level in allInLevels)
        {
            int sliceSize = level - previousLevel;
            if (sliceSize > 0 && eligible.Count > 0)
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

        foreach (var key in _currentBets.Keys.ToList())
        {
            _currentBets[key] = 0;
        }

    }

    private void AwardPot()
    {

        if (_pots.Count == 0) return;

        foreach( IPot pot in _pots)
        {
            List<IPlayer> eligibleActive = pot.Contributions
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .Where(p => p.Status!= PlayerStatus.Folded && p.Status != PlayerStatus.Bust)
                .ToList();

            if(eligibleActive.Count == 0)
            {
                continue;
            }

            List<IPlayer> winners = CompareHands(eligibleActive);
            if(winners.Count == 0)
            {
                continue;
            }

            int share = pot.TotalChips/winners.Count;
            int remainder = pot.TotalChips % winners.Count;

            int firstToActSeat = GetNextEligiblePlayerIndex(_dealerIndex);
            List<IPlayer> orderedWinners = winners.OrderBy(p =>
                {
                    int idx = _players.IndexOf(p);
                    int distance = idx - firstToActSeat;
                    if(distance < 0)
                    {
                        distance += _players.Count;
                    }
                    return distance;
                }).ToList();

        for(int i = 0; i < orderedWinners.Count; i++)
            {
                int amount = share + (i < remainder ? 1 : 0);
                _chips[orderedWinners[i]] += amount;
            }

        }

        _pots.Clear();
    }

    private int GetNextEligiblePlayerIndex(int currentIndex)
{
    int next = (currentIndex + 1) % _players.Count;
    for (int i = 0; i < _players.Count; i++)
    {
        var status = _players[next].Status;
        if (status != PlayerStatus.Bust && status != PlayerStatus.Folded)
            return next;
        next = (next + 1) % _players.Count;
    }
    return currentIndex;
}

    private void EliminateBustedPlayers()
    {
        foreach (var player in _players)
        {
            if (_chips[player] <= 0)
            {
                player.Status = PlayerStatus.Bust;
            }
        }
    }

}
