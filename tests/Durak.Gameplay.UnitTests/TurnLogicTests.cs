namespace Durak.Gameplay.UnitTests;

public class TurnLogicTests
{
    [Fact]
    public void Next_FirstBout_ShouldBePlayerWithLowestTrump()
    {
        // Arrange
        const char trumpSuit = ' ';
        var players = new List<Player>() { new(), new() };

        var dealer = Substitute.For<IDealer>();

        dealer
            .PlayerCards
            .Returns(new List<PlayerCards>()
                {
                    new(players[0], new List<Card>(){ new(2, trumpSuit) }),
                    new(players[1], new List<Card>(){ new(1, trumpSuit) })
                });

        dealer.TrumpSuit.Returns(trumpSuit);

        var sut = new TurnLogic(dealer);

        // Act
        var next = sut.Next();

        // Assert
        next.Should().Be(players[1]);
    }

    [Theory]
    [InlineData(AttackState.Successful, 2, 0)]
    [InlineData(AttackState.Successful, 3, 2)]
    [InlineData(AttackState.BeatenOff, 2, 1)]
    [InlineData(AttackState.BeatenOff, 3, 1)]
    public void Next_AfterFirstPlayerAttack_ShouldBeNextPlayer(AttackState attackState, int playerCount, int expectedNextIndex)
    {
        // Arrange
        var players = new List<Player>();
        var playerCards = new List<PlayerCards>();

        for (var i = 0 ; i < playerCount; i++)
        {
            var player = new Player();
            players.Add(player);
            playerCards.Add(new PlayerCards(player, new List<Card>()));
        }

        var dealer = Substitute.For<IDealer>();

        dealer.PlayerCards.Returns(playerCards);

        var sut = new TurnLogic(dealer);

        var bout = Substitute.For<IBout>();
        bout.AttackState.Returns(attackState);
        bout.PrincipalAttacker.Returns(players[0]);

        sut.AddBout(bout);

        // Act
        var next = sut.Next();

        // Assert
        next.Should().Be(players[expectedNextIndex]);
    }
}