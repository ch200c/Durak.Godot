namespace Durak.Gameplay.UnitTests;

public class TurnLogicTests
{
    [Fact]
    public void NextAttack_FirstAttack_PrincipalAttackerShouldBePlayerWithLowestTrump()
    {
        // Arrange
        const char trumpSuit = ' ';
        var players = new List<Player>() { new(), new() };

        players[0].PickUp([new Card(4, trumpSuit)]);
        players[1].PickUp([new Card(3, trumpSuit)]);

        var sut = new TurnLogic(players, trumpSuit);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(players[1]);
    }

    [Fact]
    public void NextAttack_AfterSuccessfulAttack_TwoPlayers_PrincipalAttackerShouldBeNextAfterDefender()
    {
        // Arrange
        var players = new List<Player>() { new(), new() };
        players[0].PickUp([new Card(2, 'd'), new Card(3, 'd')]);
        players[1].PickUp([new Card(4, 'd'), new Card(5, 'd')]);

        const char trumpSuit = 'c';
        var sut = new TurnLogic(players, trumpSuit);

        var successfulAttack = new Attack(players[0], players[1], trumpSuit);
        successfulAttack.Play(players[0], players[0].Cards[0]);
        successfulAttack.End();

        sut.AddAttack(successfulAttack);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(successfulAttack.PrincipalAttacker);
    }

    [Fact]
    public void NextAttack_AfterSuccessfulAttack_MoreThanTwoPlayers_PrincipalAttackerShouldBeNextAfterDefender()
    {
        // Arrange
        var players = new List<Player>() { new(), new(), new() };
        players[0].PickUp([new Card(2, 'd'), new Card(3, 'd')]);
        players[1].PickUp([new Card(4, 'd'), new Card(5, 'd')]);
        players[2].PickUp([new Card(6, 'd'), new Card(7, 'd')]);

        const char trumpSuit = 'c';
        var sut = new TurnLogic(players, trumpSuit);

        var successfulAttack = new Attack(players[0], players[1], trumpSuit);
        successfulAttack.Play(players[0], players[0].Cards[0]);
        successfulAttack.End();

        sut.AddAttack(successfulAttack);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(players[2]);
    }

    [Fact]
    public void NextAttack_AfterBeatenOffAttack_PrincipalAttackerShouldBeDefender()
    {
        // Arrange
        var players = new List<Player>() { new(), new() };
        players[0].PickUp([new Card(2, 'd'), new Card(3, 'd')]);
        players[1].PickUp([new Card(4, 'd'), new Card(5, 'd')]);

        const char trumpSuit = 'c';
        var sut = new TurnLogic(players, trumpSuit);

        var beatenOffAttack = new Attack(players[0], players[1], trumpSuit);
        beatenOffAttack.Play(players[0], players[0].Cards[0]);
        beatenOffAttack.Play(players[1], players[1].Cards[0]);
        beatenOffAttack.End();

        sut.AddAttack(beatenOffAttack);

        // Act
        var nextAttack = sut.NextAttack();

        // Assert
        nextAttack!.PrincipalAttacker.Should().Be(beatenOffAttack.Defender);
    }
}