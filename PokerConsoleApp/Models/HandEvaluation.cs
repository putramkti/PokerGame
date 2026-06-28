using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;

namespace PokerConsoleApp.Models;

public class HandEvaluation : IComparable<HandEvaluation>
{
    public HandEvaluation(IPlayer player, HandRank handrank, List<ICard> bestFiveCards)
    {
        Player = player;
        HandRank = handrank;
        BestFiveCards = bestFiveCards;
    }
    public IPlayer Player {get; init;}
    public HandRank HandRank { get; init; }

    public List<ICard> BestFiveCards {get; init;}

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
