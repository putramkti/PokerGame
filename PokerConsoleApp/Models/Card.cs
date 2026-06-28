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

    public override string ToString()
    {
        string rank = Rank switch
        {
            CardRank.Ace => "A",
            CardRank.King => "K",
            CardRank.Queen => "Q",
            CardRank.Jack => "J",
            CardRank.Ten => "10",
            _ => ((int)Rank).ToString()
        };

        string suit = Suit switch
        {
            CardSuit.Spades => "♠",
            CardSuit.Hearts => "♥",
            CardSuit.Diamonds => "♦",
            CardSuit.Clubs => "♣",
            _ => "?"
        };

        return $"[{rank}{suit}]";
    }

}
