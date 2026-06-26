using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class HandEvaluation : IComparable<HandEvaluation>
{

    public IPlayer Player {get; set;}
    public HandRank HandRank { get; set; }

    public List<ICard> BestFiveCards {get; set;}
    public int CompareTo(HandEvaluation? other)
    {
        if (other == null)
        {
            return 1;
        }

        if(this.HandRank != other.HandRank)
        {
            return this.HandRank.CompareTo(other.HandRank);
        }

        for (int i = 0; i < 5; i++)
        {
            if (this.BestFiveCards[i].Rank != other.BestFiveCards[i].Rank)
            {
                return this.BestFiveCards[i].Rank.CompareTo(other.BestFiveCards[i].Rank);
            }
        }

        return 0;
    }

}
