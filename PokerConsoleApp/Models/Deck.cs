using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Deck : IDeck
{
    public Deck(List<ICard> cards)
    {
        Cards = cards;
    }


    public List<ICard> Cards { get; set; }

}
