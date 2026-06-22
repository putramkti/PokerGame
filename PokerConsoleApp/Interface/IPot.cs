namespace PokerConsoleApp.Interface;

public interface IPot
{
    Dictionary<IPlayer, int> Contributions {get; set;}
    int TotalChips {get; set;}
}
