using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Deck : IDeck
{
    public Deck()
    {
    }


    public List<ICard> Cards { get; set; }

}
