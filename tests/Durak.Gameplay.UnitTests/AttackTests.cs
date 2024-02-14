namespace Durak.Gameplay.UnitTests;

public class AttackTests
{
    [Fact]
    public void Construct_Attackers_ShouldContainSingleAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player();

        // Act
        var sut = new Attack(principalAttacker, new Player());

        // Assert
        sut.Attackers.Should().ContainSingle(a => a == principalAttacker);
    }

    [Fact]
    public void Construct_Defender_ShouldBeDefenderFromConstructor()
    {
        // Arrange
        var defender = new Player();

        // Act
        var sut = new Attack(new Player(), defender);

        // Assert
        sut.Defender.Should().Be(defender);
    }

    [Fact]
    public void AddAttacker_Attackers_ShouldContainAddedAttacker()
    {
        // Arrange
        var sut = new Attack(new Player(), new Player());
        var attacker = new Player();

        // Act
        sut.AddAttacker(attacker);

        // Assert
        sut.Attackers.Should().Contain(attacker);
    }

    [Fact]
    public void Play_Cards_ShouldContainSinglePlayedCard()
    {
        // Arrange
        var sut = new Attack(new Player(), new Player());

        // Act
        sut.Play(sut.Attackers[0], new Card(1, ' '));

        // Assert
        sut.Cards.Should().ContainSingle(c => c.Card.Rank == 1 && c.Card.Suit == ' ');
    }
}
