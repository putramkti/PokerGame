using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Table : ITable
{
    public Table()
    {
        CommunityCards = new List<ICard>();
    }


    public List<ICard> CommunityCards { get; set; }

}
