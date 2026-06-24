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

    public GameController(int smallBind, int bigBlind)
    {
        _smallBlind = smallBind;
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
        if(_gameState != GameState.HandComplete && _gameState != GameState.InProgress)
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
        //masih dummy
        return new Player("");

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

        if(callAmount == 0)
        {
            actions.Add(BettingAction.Check);
        }else if( playerChips >= callAmount)
        {
            actions.Add(BettingAction.Call);
        }

        if(playerChips > callAmount)
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
            player.Status  = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void Raise(IPlayer player, int amount)
    {
        int additionalBet = amount - _currentBets[player];

        _chips[player] -= additionalBet;
        _currentBets[player] = amount;

        _lastRaiseAmount = amount - _currentHighestbet;
        _currentHighestbet = amount;
        _lastRaiserIndex = _currentPlayerIndex;

        if(_chips[player] == 0)
        {
            player.Status = PlayerStatus.AllIn;
        }

        NextPlayer();
    }

    public void AllIn(IPlayer player)
    {
        int allInAmount = _chips[player] + _currentBets[player];

        if(allInAmount > _currentHighestbet)
        {
            _lastRaiseAmount = allInAmount - _currentHighestbet;
            _currentHighestbet = allInAmount;
            _lastRaiserIndex = _currentPlayerIndex;
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

        _currentPlayerIndex = (_lastRaiserIndex + 1) % _players.Count;
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
        }while(_players[_dealerIndex].Status == PlayerStatus.Bust);
    }

    private void PostBlinds()
    {
        int sbIndex = (_dealerIndex + 1) % _players.Count;
        int bbIndex = (_dealerIndex + 2) % _players.Count;

        var sbPlayer = _players[sbIndex];
        int sbTax = Math.Min(_smallBlind, _chips[sbPlayer]);
        _chips[sbPlayer] -= sbTax;
        _currentBets[sbPlayer] = sbTax;

        var bbPlayer = _players[bbIndex];
        int bbTax = Math.Min(_bigBlind, _chips[bbPlayer]);
        _chips[bbPlayer] -= bbTax;
        _currentBets[bbPlayer] = bbTax;

        _currentHighestbet = _bigBlind;
        _lastRaiseAmount = _bigBlind;
        _lastRaiserIndex = bbIndex;
    }

    private void DealHoleCards()
    {
        for(int i = 0; i < 2; i++)
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
        if (IsBettingRoundOver())
        {
            CollectBetsToPot();
            TransitionToNextRound();
            return;
        }

        do
        {
            _currentPlayerIndex  = (_currentPlayerIndex + 1) % _players.Count();
        } while(_players[_currentPlayerIndex].Status != PlayerStatus.Active);
    }

    private bool IsBettingRoundOver()
    {
        var activePlayers = _players.Where(p => p.Status == PlayerStatus.Active).ToList();

        if(activePlayers.Count <= 1)
        {
            return true;
        }

        return activePlayers.All(p => _currentBets[p] == _currentHighestbet) && _currentPlayerIndex == _lastRaiserIndex ;
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
        if(_players.Count(p => p.Status == PlayerStatus.Active) <= 1)
        {
            RoundShowdown();
            return;
        }

        switch(_currentRound)
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

        ResolveIfOnePlayerLeft();

        AwardPot();

    }

    private void ResolveIfOnePlayerLeft()
    {
        var survivors = _players.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        if (survivors.Count == 1)
        {
            // bypass evaluasi kartu dan 1 survivor menang
        }
    }

    private HandRank EvaluateHand(IPlayer player)
    {
        // TODO: nanti tambahkan algoritma kombinasi kartu
        return HandRank.HighCard;
    }

    private List<IPlayer> CompareHands(List<IPlayer> players)
    {
        // TODO: nanti tambahkan algoritma urutan kartu pemain dari terkuat ke terlemah dari hasil evaluateHands
        return players;
    }

    private void CreateSidePots()
    {
        int totalCollected = _currentBets.Values.Sum();
        if(totalCollected > 0)
        {
            IPot pot = new Pot();
            pot.TotalChips = totalCollected;
            _pots.Add(pot);
        }
    }

    private void AwardPot()
    {
        var winners = GetHandWinners();

        if(winners.Count > 0 && _pots.Count() > 0)
        {
            //TODO: nanti revisi algoritma pembagian chips dari pots nya, foreach dulu potsnya-> jika pemenang ada dalam contributions pots bagi ke pemenang
            int share = _pots.Sum(p => p.TotalChips) / winners.Count;

            foreach(var winner in winners)
            {
                _chips[winner] += share;
            }
        }
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
