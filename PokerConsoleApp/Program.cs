using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;
using PokerConsoleApp.UI;

GameController gameController = new GameController(25, 50);

gameController.AddPlayer("Putra1", 1000);
gameController.AddPlayer("Putra2", 1000);
gameController.AddPlayer("Putra3", 1000);
gameController.AddPlayer("Putra4", 1000);

ConsoleRenderer consoleRenderer = new ConsoleRenderer(gameController);
InputHandler inputHandler = new InputHandler(gameController);


gameController.StartGame();

while(gameController.GetGameState() != GameState.GameOver)
{
    if (gameController.GetGameState() == GameState.HandComplete)
    {
        Console.Clear();
        Console.WriteLine("Hand baruu....");

        gameController.StartNexthand();
        continue;
    }

    consoleRenderer.DrawGameBoard();

    IPlayer activePlayer = gameController.GetCurrentPlayer();

    inputHandler.ProccessTurn(activePlayer);
}