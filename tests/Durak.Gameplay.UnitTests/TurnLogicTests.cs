namespace Durak.Gameplay.UnitTests;

public class TurnLogicTests
{
    [Fact]
    public void NextAttack_FirstAttack_PrincipalAttackerShouldBePlayerWithLowestTrump()
    {
        // Arrange
        const char trumpSuit = ' ';
        var players = new List<Player>() { new(), new() };

        players[0].PickUp([new Card(2, trumpSuit)]);
        players[1].PickUp([new Card(1, trumpSuit)]);
        var dealer = Substitute.For<IDealer>();

        dealer.Players.Returns(players);
        dealer.TrumpSuit.Returns(trumpSuit);

        var sut = new TurnLogic(dealer);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(players[1]);
    }

    [Theory]
    [InlineData(AttackState.Successful, 2, 0)]
    [InlineData(AttackState.Successful, 3, 2)]
    [InlineData(AttackState.BeatenOff, 2, 1)]
    [InlineData(AttackState.BeatenOff, 3, 1)]
    public void NextAttack_AfterFirstPlayerAttack_ShouldBeNextPlayer(
        AttackState attackState, int playerCount, int expectedNextIndex)
    {
        // Arrange
        var players = new List<Player>();

        for (var i = 0 ; i < playerCount; i++)
        {
            var player = new Player();
            player.PickUp([new Card(1, ' ')]);
            players.Add(player);
        }

        var dealer = Substitute.For<IDealer>();
        dealer.Players.Returns(players);

        var sut = new TurnLogic(dealer);

        var attack = Substitute.For<IAttack>();
        attack.State.Returns(attackState);
        attack.PrincipalAttacker.Returns(players[0]);

        sut.AddAttack(attack);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(players[expectedNextIndex]);
    }
}