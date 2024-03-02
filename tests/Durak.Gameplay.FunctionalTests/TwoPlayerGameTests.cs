using System.Text;
using Xunit.Abstractions;

namespace Durak.Gameplay.FunctionalTests;

public class TwoPlayerGameTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TwoPlayerGameTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TurnLogic_NextAttackIsNull_ShouldHaveAPlayerWithNoCards()
    {
        var player1 = new Player("P1");
        var player2 = new Player("P2");
        var players = new List<Player>() { player1, player2 };
        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
        var dealer = new Dealer(6, players, deck);
        var turnLogic = new TurnLogic(players, deck.TrumpSuit);

        try
        {
            while (dealer.Deal())
            {
                if (!Attack(turnLogic))
                {
                    throw new NotImplementedException("TODO3");
                }
            }

            while (Attack(turnLogic))
            {
                // Do nothing
            }
        }
        catch (Exception ex) when (ex is not GameplayException)
        {
            _testOutputHelper.WriteLine(ex.Message);
            _testOutputHelper.WriteLine(ex.StackTrace);
            _testOutputHelper.WriteLine(string.Join(',', players.Select(p => p.Cards.Count)));
        }

        players.Should().Contain(p => p.Cards.Count == 0);
    }

    private bool Attack(TurnLogic turnLogic)
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

        _testOutputHelper.WriteLine(ToString(attack));
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
                if (ex.Message == "Cannot have more attacking cards in this attack")
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

        var attackers = string.Join(", ", attack.Attackers.Select(a => a.Id));

        stringBuilder.AppendFormat("{0} vs {1}", attackers, attack.Defender.Id);
        stringBuilder.AppendLine();

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
