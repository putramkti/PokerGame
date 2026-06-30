using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.UI;

public class InputHandler
{
    private readonly GameController _controller;

    public InputHandler(GameController controller)
    {
        _controller = controller;
    }

    public void WaitForEnter(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
        {
            Console.WriteLine(message);
        }
        Console.ReadLine();
    }

    public void ProcessTurn(IPlayer player)
    {
        List<BettingAction> availableActions = _controller.GetAvailableBettingAction(player);

        int selectedIndex = -1;
        while (selectedIndex < 0 || selectedIndex >= availableActions.Count)
        {
            string userInput = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(userInput, out int choiceNumber) && choiceNumber >= 1 && choiceNumber <= availableActions.Count)
            {
                selectedIndex = choiceNumber - 1;
            }
            else
            {
                Console.WriteLine("Input tidak valid! Masukkan angka yang tertera pada menu.");
                Console.Write($"Ketik angka opsi (1-{availableActions.Count}): ");
            }
        }

        BettingAction chosenAction = availableActions[selectedIndex];

        switch (chosenAction)
        {
            case BettingAction.Fold:
                _controller.Fold(player);
                break;

            case BettingAction.Check:
                _controller.Check(player);
                break;

            case BettingAction.Call:
                _controller.Call(player);
                break;

            case BettingAction.Raise:
                ProcessRaiseAction(player);
                break;

            case BettingAction.AllIn:
                _controller.AllIn(player);
                break;
        }
        
    }

    private void ProcessRaiseAction(IPlayer player)
    {
        int minRaise = _controller.GetMinRaise();
        int maxChips = _controller.GetPlayerChips(player) + _controller.GetPlayerCurrentBet(player);
        int raiseTargetAmount = 0;

        while (raiseTargetAmount < minRaise || raiseTargetAmount > maxChips)
        {
            Console.Write($"Masukkan total taruhan baru Anda ({minRaise} - {maxChips}): ");
            string userInput = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(userInput, out int inputAmount))
            {
                raiseTargetAmount = inputAmount;
                if (raiseTargetAmount < minRaise)
                    Console.WriteLine($"Nominal terlalu kecil! Minimal Raise adalah {minRaise}.");
                else if (raiseTargetAmount > maxChips)
                    Console.WriteLine("Chip Anda tidak mencukupi untuk melakukan Raise.");
            }
            else
            {
                Console.WriteLine("Masukkan format angka yang valid!");
            }
        }

        _controller.Raise(player, raiseTargetAmount);
    }
}
