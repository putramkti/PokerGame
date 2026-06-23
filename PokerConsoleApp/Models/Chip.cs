using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class Chip : IChip
{
    public Chip(int amount)
    {
        Amount = amount;
    }

    public int Amount { get; set; }
}
