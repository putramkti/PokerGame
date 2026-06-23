namespace PokerConsoleApp.Interfaces;

public interface IPot
{
    Dictionary<IPlayer, int> Contributions {get; set;}
    int TotalChips {get; set;}
}
