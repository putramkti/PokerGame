using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Player : IPlayer
{
    public Player(string name)
    {
        Name = name;
    }


    public string Name { get; set; }
    public PlayerStatus Status { get; set; }

}
