using System.Text;
using Xunit.Abstractions;

namespace Durak.Gameplay.FunctionalTests;

public class GameSimulator(ITestOutputHelper testOutputHelper, IDealer dealer, ITurnLogic turnLogic)
{
    public void Simulate()
    {
        while (dealer.Deal())
        {
            if (!Attack())
            {
                throw new NotImplementedException("TODO3");
            }
        }

        while (Attack())
        {
            // Do nothing
        }
    }

    private bool Attack()
    {
        var attack = turnLogic.NextAttack();

        if (attack == null)
        {
            return false;
        }

        foreach (var attacker in attack.Attackers)
        {
            var cards = string.Join(',', attacker.Cards.Select(c => c.ToString()));
            testOutputHelper.WriteLine($"{attacker.Id} {cards}");
        }

        var cards2 = string.Join(',', attack.Defender.Cards.Select(c => c.ToString()));
        testOutputHelper.WriteLine($"{attack.Defender.Id} {cards2}");

        // TODO: random chance of skipping, adding attackers

        while (attack.State == AttackState.InProgress)
        {
            var isAttacking = PlayOrPass(attack, attack.PrincipalAttacker);
            if (isAttacking)
            {
                var isDefending = PlayOrPass(attack, attack.Defender);
                if (!isDefending)
                {
                    attack.End();
                }
            }
            else
            {
                attack.End();
            }
        }

        testOutputHelper.WriteLine(ToString(attack));
        return true;
    }

    private static bool PlayOrPass(IAttack attack, Player player)
    {
        var availableCardIndices = new Queue<int>(Enumerable.Range(0, player.Cards.Count));
        while (availableCardIndices.TryDequeue(out var cardIndex))
        {
            try
            {
                attack.Play(player, player.Cards[cardIndex]);
                return true;
            }
            catch (GameplayException ex)
            {
                if (ex.Message == "Cannot have more attacking cards in this attack"
                    || ex.Message.Contains("Reached more than"))
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static string ToString(IAttack? attack)
    {
        if (attack is null)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        var cardGroups = attack.Cards
            .Select(c => $"{c.Player.Id} {c.Card}")
            .Chunk(2);

        foreach (var cardGroup in cardGroups)
        {
            stringBuilder.AppendJoin(' ', cardGroup);
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }
}
