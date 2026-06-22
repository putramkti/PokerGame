using PokerConsoleApp.Enumeration;
using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Card : ICard
{ 
    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public CardSuit Suit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public CardRank Rank { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool IsRevealed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

}
