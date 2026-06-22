using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Table : ITable
{
    public Table()
    {
    }


    public List<ICard> CommunityCards { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

}
