using PokerConsoleApp.Enumeration;
using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Player : IPlayer
{
    public Player(string name)
    {
        Name = name;
    }


    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public PlayerStatus Status { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

}
