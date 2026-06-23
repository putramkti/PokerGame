using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Table : ITable
{
    public Table()
    {
    }


    public List<ICard> CommunityCards { get; set; }

}
