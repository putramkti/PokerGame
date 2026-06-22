using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Pot : IPot
{
    public Pot()
    {
    }


    public Dictionary<IPlayer, int> Contributions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int TotalChips { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

}
