using PokerConsoleApp.Enumeration;

namespace PokerConsoleApp.Interface;

public interface IPlayer
{
    string Name {get; set;}
    PlayerStatus Status {get; set;}
}
