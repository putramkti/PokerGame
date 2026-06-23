using PokerConsoleApp.Enums;

namespace PokerConsoleApp.Interfaces;

public interface IPlayer
{
    string Name {get; set;}
    PlayerStatus Status {get; set;}
}
