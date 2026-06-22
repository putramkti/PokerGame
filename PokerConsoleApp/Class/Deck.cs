using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Deck : IDeck
{
    public Deck()
    {
    }


    public List<ICard> Cards { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

}
