using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;
using PokerConsoleApp.Models;
using PokerConsoleApp.UI;

const int smallBlind = 25;
const int bigBlind = 50;

List<ICard> cards = new List<ICard>();

foreach (CardSuit suit in Enum.GetValues<CardSuit>())
{
    foreach (CardRank rank in Enum.GetValues<CardRank>())
    {
        cards.Add(new Card(suit, rank));
    }
}

List<IPlayer> players = new List<IPlayer>();
Dictionary<IPlayer, IChip> chips = new Dictionary<IPlayer, IChip>();
Dictionary<IPlayer, List<ICard>> holeCards = new Dictionary<IPlayer, List<ICard>>();
Dictionary<IPlayer, int> currentBets = new Dictionary<IPlayer, int>();
List<IPot> pots = new List<IPot>();

Deck deck = new Deck(cards);
Table table = new Table();

GameController gameController = new GameController(smallBlind, bigBlind, players, chips, holeCards, currentBets, pots, deck, table);
ConsoleRenderer consoleRenderer = new ConsoleRenderer(gameController);

consoleRenderer.RunGame(smallBlind, bigBlind);