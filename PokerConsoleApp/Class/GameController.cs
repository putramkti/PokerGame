using PokerConsoleApp.Enumeration;
using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class GameController
{
    private Dictionary<IPlayer, List<IChip>> _chips;
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
    }

    public void AddPlayer(string name, int chips)
    {
        
    }

    public void StartGame()
    {
        
    }

    public void StartNexthand()
    {
        
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
        
    }

    public IPlayer GetGameWinner()
    {
        
    }

    public List<IPlayer> GetHandWinners()
    {
        
    }

    public IPlayer GetCurrentPlayer()
    {
        
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

    public List<IChip> GetPlayerChips(IPlayer player)
    {
        return _chips[player];
    }

    public int GetPlayerTotalChips(IPlayer player)
    {
        
    }

    public int GetPlayerCurrentBet(IPlayer player)
    {
        
    }

    public int GetSmallBind()
    {
        
    }

    public int GetBigBlind()
    {
        
    }

    public int GetCurrentHighestBet()
    {
        
    }

    public int GetCallAmount(IPlayer player)
    {
        
    }

    public int GetMinRaise()
    {
        
    }

    public List<BettingAction> GetAvailableBettingAction(IPlayer player)
    {
        
    }


    public List<ICard> GetCommunityCards()
    {
        
    }

    public IPot GetMainPot()
    {
        
    }

    public List<IPot> GetSidePots()
    {
        
    }

    public int GetTotalPotAmount()
    {
        
    }

    public void Fold(IPlayer player)
    {
        
    }

    public void Check(IPlayer player)
    {
        
    }

    public void Call(IPlayer player)
    {
        
    }

    public void Raise(IPlayer player, int amount)
    {
        
    }

    public void AllIn(IPlayer player)
    {
        
    }

    private void StartNewHand()
    {
        
    }

    private void ShuffleDeck()
    {
        
    }

    private void RotateDealer()
    {
        
    }

    private void PostBlinds()
    {
        
    }

    private void DealHoleCards()
    {
        
    }

    private ICard DealCard()
    {
        
    }

    private void DealFlop()
    {
        
    }

    private void DealTurn()
    {
        
    }

    private void DealTurn()
    {
        
    }

    private void DealRiver()
    {
        
    }

    private void RunShowdown()
    {
        
    }

    private void RunBettingRound()
    {
        
    }

    private void NextPlayer()
    {
        
    }

    private bool IsBettingRoundOver()
    {
        
    }

    private void ResolveIfOnePlayerLeft()
    {
        
    }

    private HandRank EvaluateHand(IPlayer player)
    {
        
    }

    private List<IPlayer> CompareHands(List<IPlayer> players)
    {
        
    }

    private void CreateSidePots()
    {
        
    }

    private void AwardPot()
    {
        
    }

    private void EliminateBustedPlayers()
    {

    }


    

}
