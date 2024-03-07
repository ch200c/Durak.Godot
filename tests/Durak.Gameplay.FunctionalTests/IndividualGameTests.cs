using Xunit.Abstractions;

namespace Durak.Gameplay.FunctionalTests;

public class IndividualGameTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public IndividualGameTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void TurnLogic_NextAttackIsNull_ShouldHaveOnePlayerWithCards(int playerCount)
    {
        var players = new List<Player>();
        for (var i = 0 ; i < playerCount; i++)
        {
            players.Add(new Player($"P{i + 1}"));
        }

        var deck = new Deck(new FrenchSuited36CardProvider(), new DefaultCardShuffler());
        var dealer = new Dealer(6, players, deck);
        var turnLogic = new TurnLogic(players, deck.TrumpSuit);
        var gameSimulator = new GameSimulator(_testOutputHelper, dealer, turnLogic);

        _testOutputHelper.WriteLine($"Trump: {deck.TrumpSuit}");

        try
        {
            gameSimulator.Simulate();
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine(ex.Message);
            _testOutputHelper.WriteLine(ex.StackTrace);
            _testOutputHelper.WriteLine(string.Join(',', players.Select(p => p.Cards.Count)));
        }

        var lastAttack = turnLogic.Attacks[^1];
        var isDraw = lastAttack.PrincipalAttacker.Cards.Count == 0 
            && lastAttack.Defender.Cards.Count == 0;

        if (!isDraw)
        {
            players.Should().ContainSingle(p => p.Cards.Count > 0);
        }
    }
}
