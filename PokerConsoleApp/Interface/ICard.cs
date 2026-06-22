using PokerConsoleApp.Enumeration;

namespace PokerConsoleApp.Interface;

public interface ICard
{
    CardSuit Suit {get; set;}
    CardRank Rank {get; set;}
    bool IsRevealed {get; set;}

    string ToString();
}
