namespace Durak.Gameplay;

public class Dealer(int requiredPlayerCardCount, IEnumerable<Player> players, IDeck deck) : IDealer
{
    private readonly int requiredPlayerCardCount = requiredPlayerCardCount < 1 ? throw new ArgumentOutOfRangeException(nameof(requiredPlayerCardCount)) : requiredPlayerCardCount;

    public bool Deal(IAttack? previousAttack)
    {
        return previousAttack == null
            ? FirstDeal()
            : ConsecutiveDeal(previousAttack);
    }

    private bool FirstDeal()
    {
        var isReplenished = true;

        for (var i = 0; i < requiredPlayerCardCount; i++)
        {
            foreach (var player in players)
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
        }

        return isReplenished;
    }

    private bool ConsecutiveDeal(IAttack previousAttack)
    {
        var isReplenished = true;

        foreach (var player in previousAttack.Attackers.Union([previousAttack.Defender]))
        {
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
        }

        return isReplenished;
    }
}
