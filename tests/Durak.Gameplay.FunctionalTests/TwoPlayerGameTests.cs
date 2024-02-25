namespace Durak.Gameplay.FunctionalTests;

public class TwoPlayerGameTests
{
    [Fact]
    public void ShouldHaveAPlayerWithNoCardsInTheEnd()
    {
        var player1 = new Player("P1");
        var player2 = new Player("P2");
        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
        var dealer = new Dealer(6, [player1, player2], deck);
        var turnLogic = new TurnLogic(dealer);

        while (dealer.Deal())
        {
            Attack(turnLogic);
        }

        while (Attack(turnLogic))
        {
            // Do nothing
        }

        dealer.Players.Should().Contain(p => p.Cards.Count == 0);
    }

    private static bool Attack(TurnLogic turnLogic)
    {
        var attack = turnLogic.NextAttack();
        if (attack == null)
        {
            return false;
        }
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
            catch (GameplayException)
            {
                // Ignore
            }
        }

        return false;
    }
}
