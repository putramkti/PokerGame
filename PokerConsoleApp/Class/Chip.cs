using PokerConsoleApp.Interface;

namespace PokerConsoleApp.Class;

public class Chip : IChip
{
    public Chip(int amount)
    {
        Amount = amount;
    }

    public int Amount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
