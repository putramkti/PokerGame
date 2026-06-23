using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Card : ICard
{ 
    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public CardSuit Suit { get; set; }
    public CardRank Rank { get; set; }
    public bool IsRevealed { get; set; }

}
