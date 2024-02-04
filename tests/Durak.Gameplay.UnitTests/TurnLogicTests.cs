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
        dealer.PlayerCards.Returns(
            new List<PlayerCards>()
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

    [Fact]
    public void Next_SuccessfulAttack_ShouldBeNextPlayer()
    {
        // Arrange
        var players = new List<Player>() { new(), new() };


        var dealer = Substitute.For<IDealer>();
        dealer.PlayerCards.Returns(
            new List<PlayerCards>()
            {
                new(players[0], new List<Card>()),
                new(players[1], new List<Card>())
            });


        var sut = new TurnLogic(dealer);

        var bout = new Bout(players[0], players[1]);
        bout.SetAttackState(AttackState.Successful);

        sut.AddBout(bout);

        // Act
        var next = sut.Next();

        // Assert
        next.Should().Be(players[0]);
    }
}