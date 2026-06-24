using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Deck : IDeck
{
    public Deck()
    {
        Cards = new List<ICard>();
        foreach(CardSuit suit in Enum.GetValues<CardSuit>())
        {
            foreach (CardRank rank in Enum.GetValues<CardRank>())
            {
                Cards.Add(new Card(suit, rank));
            }
        }
    }


    public List<ICard> Cards { get; set; }

}
