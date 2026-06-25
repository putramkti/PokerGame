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

    public void ProccessTurn(IPlayer player)
    {
        List<BettingAction> availableActions = _controller.GetAvailableBettingAction(player);

        Console.WriteLine($"\n Giliran {player.Name}. Aksi:");

        for(int i  = 0; i< availableActions.Count(); i++)
        {
            string additionalInfo = "";
            if(availableActions[i] == BettingAction.Call)
            {
                additionalInfo = $" ({_controller.GetCallAmount(player)} chips)";
            }
            else if (availableActions[i] == BettingAction.Raise)
            {
                additionalInfo = $" (Min: {_controller.GetMinRaise()} chips)";
            }

            Console.WriteLine($" [{i + 1}] {availableActions[i]}{additionalInfo}");
        }

        int selectedindex = -1;
        while(selectedindex < 0 || selectedindex >= availableActions.Count)
        {
            Console.Write($" Ketik angka opsi (1-{availableActions.Count}): ");
            string userInput = Console.ReadLine();

            if(int.TryParse(userInput, out int choiceNumber) && choiceNumber >=1 && choiceNumber <= availableActions.Count)
            {
                selectedindex = choiceNumber - 1;
            }
            else
            {
                Console.WriteLine(" Input tidak valid! Masukkan angka yang tertera pada menu.");
            }
        }

        BettingAction chosenAction = availableActions[selectedindex];

        switch (chosenAction)
        {
            case BettingAction.Fold:
                _controller.Fold(player);
                Console.WriteLine($" {player.Name} memilih FOLD.");
                break;

            case BettingAction.Check:
                _controller.Check(player);
                Console.WriteLine($" {player.Name} memilih CHECK.");
                break;

            case BettingAction.Call:
                _controller.Call(player);
                Console.WriteLine($" {player.Name} memilih CALL.");
                break;

            case BettingAction.Raise:
                ProcessRaiseAction(player);
                break;

            case BettingAction.AllIn:
                _controller.AllIn(player);
                Console.WriteLine($" {player.Name} memilih ALL-IN! 🔥");
                break;
        }
    }

    private void ProcessRaiseAction(IPlayer player)
    {
        int minRaise = _controller.GetMinRaise();
        int maxChips = _controller.GetPlayerTotalChips(player) + _controller.GetPlayerCurrentBet(player);
        int raiseTargetAmount = 0;

        while(raiseTargetAmount < minRaise || raiseTargetAmount > maxChips)
        {
            Console.Write($" Masukkan total taruhan baru Anda ({minRaise} - {maxChips}): ");
            string userInput = Console.ReadLine();

            if (int.TryParse(userInput, out int inputAmount))
            {
                raiseTargetAmount = inputAmount;
                if (raiseTargetAmount < minRaise)
                    Console.WriteLine($" Nominal terlalu kecil! Minimal Raise adalah {minRaise}.");
                else if (raiseTargetAmount > maxChips)
                    Console.WriteLine(" Chip Anda tidak mencukupi untuk melakukan Raise.");
            }
            else
            {
                Console.WriteLine(" Masukkan format angka yang valid!");
            }
        }

        _controller.Raise(player, raiseTargetAmount);
        Console.WriteLine($" {player.Name} melakukan RAISE menjadi total {raiseTargetAmount} chips.");
    }
}