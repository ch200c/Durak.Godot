namespace Durak.Gameplay.UnitTests;

public class BoutTests
{
    [Fact]
    public void Construct_Attackers_ShouldContainSingleAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player();

        // Act
        var sut = new Bout(principalAttacker, new Player());

        // Assert
        sut.Attackers.Should().ContainSingle(a => a == principalAttacker);
    }

    [Fact]
    public void Construct_Defender_ShouldBeDefenderFromConstructor()
    {
        // Arrange
        var defender = new Player();

        // Act
        var sut = new Bout(new Player(), defender);

        // Assert
        sut.Defender.Should().Be(defender);
    }

    [Fact]
    public void AddAttacker_Attackers_ShouldContainAddedAttacker()
    {
        // Arrange
        var sut = new Bout(new Player(), new Player());
        var attacker = new Player();

        // Act
        sut.AddAttacker(attacker);

        // Assert
        sut.Attackers.Should().Contain(attacker);
    }
}
