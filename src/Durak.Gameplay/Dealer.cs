namespace Durak.Gameplay;

public class Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck) : IDealer
{
    public bool Deal()
    {
        foreach (var player in players)
        {
            if (!Replenish(player))
            {
                return false;
            }
        }

        return true;
    }

    //todo fix replenish order
    private bool Replenish(Player player)
    {
        var isReplenished = true;
       
        while (player.Cards.Count < requiredPlayerCardCount)
        {
            if (deck.TryDequeue(out var card))
            {
                player.PickUp([card]);
            }
            else
            {
                isReplenished = false;
                break;
            }
        }

        return isReplenished;
    }
}
