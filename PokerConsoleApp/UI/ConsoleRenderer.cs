using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;
using PokerConsoleApp.Models;

namespace PokerConsoleApp.UI;

public class ConsoleRenderer
{
    private readonly GameController _controller;

    public ConsoleRenderer(GameController controller)
    {
        _controller = controller;
    }

    public void DrawGameBoard()
    {
        Console.Clear();
        Console.WriteLine("=====================================================================");
        Console.WriteLine($"         INI POKER CUY (Round: {_controller.GetCurrentRound()})     ");
        Console.WriteLine("=====================================================================");

        DrawCommunityCards();
        DrawPotInfo();
        DrawPlayersList();

        Console.WriteLine("=====================================================================");
    }

    private void DrawCommunityCards()
    {
        Console.Write("Community Cards :");
        var cards = _controller.GetCommunityCards();

        if (cards == null || cards.Count() == 0)
        {
            Console.WriteLine("[Belum ada kartu]");
        }
        else
        {
            foreach (var card in cards)
            {
                setCardColor(card.Suit);
                Console.Write($"[{GetRankDisplay(card.Rank)}{GetSuitSymbol(card.Suit)}] ");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void DrawPotInfo()
    {
        Console.WriteLine($" Total POT di Meja : {_controller.GetTotalPotAmount()} Chips");
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void DrawPlayersList()
    {
        Console.WriteLine("Daftar Pemain:");
        var allPlayers = _controller.GetAllPlayers();
        IPlayer currentPlayer = _controller.GetCurrentPlayer();
        IPlayer dealer = _controller.GetDealer();

        for (int i = 0; i < allPlayers.Count; i++)
        {
            var player = allPlayers[i];

            string turnMarker = (player == currentPlayer && _controller.GetGameState() == GameState.InProgress) ? "--> " : "    ";

            string dealerMarker = (player == dealer) ? "[D] " : "    ";

            Console.Write($"{turnMarker}{dealerMarker} {i + 1}. {player.Name,-10} | Chips: {_controller.GetPlayerChips(player),-6} | Bets: {_controller.GetPlayerCurrentBet(player),-5} | Status: {player.Status}");

            var holeCards = _controller.GetPlayerHoleCards(player);
            Console.Write(" | Kartu: ");

            if (holeCards == null || holeCards.Count == 0)
            {
                Console.Write("Kartru belum dibagikan");
            }
            else
            {
                foreach (var card in holeCards)
                {
                    if (player == currentPlayer || _controller.GetCurrentRound() == GameRound.Showdown || card.IsRevealed)
                    {
                        setCardColor(card.Suit);
                        Console.Write($"[{GetRankDisplay(card.Rank)}{GetSuitSymbol(card.Suit)}] ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("[??] ");
                        Console.ResetColor();
                    }
                }
            }
            Console.WriteLine();
        }
    }

    private string GetSuitSymbol(CardSuit suit)
    {
        return suit switch
        {
            CardSuit.Spades => "♠",
            CardSuit.Hearts => "♥",
            CardSuit.Diamonds => "♦",
            CardSuit.Clubs => "♣",
            _ => "?"
        };
    }

    private string GetRankDisplay(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => "A",
            CardRank.King => "K",
            CardRank.Queen => "Q",
            CardRank.Jack => "J",
            CardRank.Ten => "10",
            _ => ((int)rank).ToString()
        };
    }

    private void setCardColor(CardSuit suit)
    {
        if (suit == CardSuit.Hearts || suit == CardSuit.Diamonds)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
