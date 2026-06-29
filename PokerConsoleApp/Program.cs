using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;
using PokerConsoleApp.UI;

const int smallBlind = 25;
const int bigBlind = 50;

GameController gameController = new GameController(smallBlind, bigBlind);
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
    // Tampilkan hand result
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

    GameRound previousRound = gameController.GetCurrentRound();

    while (gameController.GetGameState() != GameState.GameOver)
    {
        // if (gameController.GetGameState() == GameState.HandComplete)
        // {
        //     var allPlayers = gameController.GetAllPlayers();
        //     var activePlayers = allPlayers.Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust).ToList();
        //     var communityCards = gameController.GetCommunityCards();

        //     if (activePlayers.Count > 1)
        //     {
        //         consoleRenderer.ShowShowdown(activePlayers, communityCards);
        //         inputHandler.WaitForEnter();
        //     }

        //     var winners = gameController.GetHandWinners();
        //     var pots = new List<IPot>();
        //     var mainPot = gameController.GetMainPot();
        //     if (mainPot != null) pots.Add(mainPot);
        //     pots.AddRange(gameController.GetSidePots());

        //     consoleRenderer.ShowHandResult(winners, pots);
        //     inputHandler.WaitForEnter();

        //     previousRound = GameRound.PreFlop;

        //     gameController.StartNexthand();
        //     continue;
        // }

        // IPlayer activePlayer = gameController.GetCurrentPlayer();
        // consoleRenderer.ShowPlayerTransitionScreen(activePlayer.Name);
        // inputHandler.WaitForEnter();

        // GameRound currentRound = gameController.GetCurrentRound();
        // if (currentRound != previousRound && currentRound != GameRound.PreFlop)
        // {
        //     consoleRenderer.ShowRoundTransition(currentRound, gameController.GetCommunityCards());
        //     inputHandler.WaitForEnter();
        //     previousRound = currentRound;
        // }

        // consoleRenderer.DrawPlayerView(activePlayer);

        // inputHandler.ProcessTurn(activePlayer);

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

