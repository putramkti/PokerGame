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


GameController gameController = new GameController(smallBlind, bigBlind, players, chips, holeCards, currentBets, pots ,deck, table);
ConsoleRenderer consoleRenderer = new ConsoleRenderer(gameController);
InputHandler inputHandler = new InputHandler(gameController);

void HandleRoundChanged(GameRound round)
{
    if (round == GameRound.PreFlop || round == GameRound.Showdown) return;

    consoleRenderer.ShowRoundTransition(round, gameController.GetCommunityCards());
    inputHandler.WaitForEnter();
}

void HandleHandWinners(List<IPlayer> winners)
{
    List<IPot> pots = new List<IPot>();
    IPot? mainPot = gameController.GetMainPot();
    if (mainPot != null) pots.Add(mainPot);
    pots.AddRange(gameController.GetSidePots());

    consoleRenderer.ShowHandResult(winners, pots);
    inputHandler.WaitForEnter();
}


try
{
    gameController.OnRoundChanged += HandleRoundChanged;
    gameController.OnHandWinnersDecided += HandleHandWinners;

    consoleRenderer.ShowSetupBanner();


    int playerCount = 0;
    while (playerCount < 2 || playerCount > 10)
    {
        Console.Write("\nMasukkan jumlah pemain (2-10): ");
        string input = Console.ReadLine() ?? string.Empty;

        if (!int.TryParse(input, out playerCount) || playerCount < 2 || playerCount > 10)
        {
            Console.WriteLine("Jumlah pemain harus antara 2-10!");
        }
    }

    List<string> registeredNames = new List<string>();

    for (int i = 1; i <= playerCount; i++)
    {
        Console.WriteLine($"\n--- Pemain {i} ---");

        string name = "";
        bool validName = false;

        while (!validName)
        {
            Console.Write($"Masukkan nama pemain {i}: ");
            name = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Nama tidak boleh kosong!");
                continue;
            }

            if (registeredNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Nama '{name}' sudah digunakan! Pilih nama lain.");
                continue;
            }

            validName = true;
        }

        registeredNames.Add(name);
        gameController.AddPlayer(name, 1000);
    }

    consoleRenderer.ShowSetupConfirmation(smallBlind, bigBlind);
    Console.ReadLine();



    gameController.StartGame();


    while (gameController.GetGameState() != GameState.GameOver)
    {
        if (gameController.GetGameState() == GameState.HandComplete)
        {
            gameController.StartNexthand();
            continue;
        }

        IPlayer activePlayer = gameController.GetCurrentPlayer();
        consoleRenderer.ShowPlayerTransitionScreen(activePlayer.Name);
        inputHandler.WaitForEnter();

        consoleRenderer.DrawPlayerView(activePlayer);
        inputHandler.ProcessTurn(activePlayer);
    }

}
finally
{
    gameController.OnRoundChanged -= HandleRoundChanged;
    gameController.OnHandWinnersDecided -= HandleHandWinners;
}


consoleRenderer.ShowGameOver(gameController.GetGameWinner());

