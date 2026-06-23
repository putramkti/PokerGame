using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Pot : IPot
{
    public Pot()
    {
    }


    public Dictionary<IPlayer, int> Contributions { get; set; }
    public int TotalChips { get; set; }

}
