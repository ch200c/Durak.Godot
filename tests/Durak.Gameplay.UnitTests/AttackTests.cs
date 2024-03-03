namespace Durak.Gameplay.UnitTests;

public class AttackTests
{
    [Fact]
    public void Construct_PrincipalAttacker_ShouldBeAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player();

        // Act
        var sut = new Attack(principalAttacker, new Player(), ' ');

        // Assert
        sut.PrincipalAttacker.Should().Be(principalAttacker);
    }

    [Fact]
    public void Construct_Defender_ShouldBeDefenderFromConstructor()
    {
        // Arrange
        var defender = new Player();

        // Act
        var sut = new Attack(new Player(), defender, ' ');

        // Assert
        sut.Defender.Should().Be(defender);
    }

    [Fact]
    public void Construct_Attackers_ShouldContainSingleAttackerFromConstructor()
    {
        // Arrange
        var principalAttacker = new Player();

        // Act
        var sut = new Attack(principalAttacker, new Player(), ' ');

        // Assert
        sut.Attackers.Should().ContainSingle(a => a == principalAttacker);
    }

    [Fact]
    public void AddAttacker_Attackers_ShouldContainAddedAttacker()
    {
        // Arrange
        var sut = new Attack(new Player(), new Player(), ' ');
        var attacker = new Player();

        // Act
        sut.AddAttacker(attacker);

        // Assert
        sut.Attackers.Should().Contain(attacker);
    }

    [Fact]
    public void Play_FirstCard_Cards_ShouldContainSinglePlayedCard()
    {
        // Arrange
        var attacker = new Player();
        attacker.PickUp([new Card(1, ' ')]);

        var defender = new Player();
        defender.PickUp([new Card(2, ' ')]);

        var sut = new Attack(attacker, defender, ' ');

        // Act
        sut.Play(attacker, attacker.Cards[0]);

        // Assert
        sut.Cards.Should().ContainSingle(c => c.Card.Rank == 1 && c.Card.Suit == ' ');
    }

    [Fact]
    public void Play_DefenderWithNoCards_ShouldThrowGameplayException()
    {
        // Arrange
        var attacker = new Player();
        attacker.PickUp([new Card(1, ' ')]);

        var defender = new Player();

        var sut = new Attack(attacker, defender, ' ');

        // Act
        var act = () => sut.Play(attacker, attacker.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }

    [Theory]
    [InlineData(7, 'c', 6, 'c')]    // lower rank, same suit, trump
    [InlineData(7, 'c', 6, 'd')]    // lower rank, different suit, non-trump
    [InlineData(7, 'd', 6, 'd')]    // lower rank, same suit, non-trump
    [InlineData(7, 'c', 8, 'd')]    // higher rank, different suit, non-trump
    public void Play_DefendWithInvalidCard_ShouldThrowGameplayException(
        int attackerRank, char attackerSuit, int defenderRank, char defenderSuit)
    {
        // Arrange
        var attacker = new Player();
        attacker.PickUp([new Card(attackerRank, attackerSuit)]);

        var defender = new Player();
        defender.PickUp([new Card(defenderRank, defenderSuit)]);

        const char trumpSuit = 'c';
        var sut = new Attack(attacker, defender, trumpSuit);

        sut.Play(attacker, attacker.Cards[0]);

        // Act
        var act = () => sut.Play(defender, defender.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }

    [Fact]
    public void Play_DifferentRankCard_ShouldThrowGameplayException()
    {
        // Arrange
        var attacker = new Player();
        attacker.PickUp([new Card(6, 'c'), new Card(7, 'c')]);

        var defender = new Player();
        defender.PickUp([new Card(10, 'c'), new Card(11, 'c')]);

        const char trumpSuit = 'd';
        var sut = new Attack(attacker, defender, trumpSuit);

        sut.Play(attacker, attacker.Cards[0]);
        sut.Play(defender, defender.Cards[0]);

        // Act
        var act = () => sut.Play(attacker, attacker.Cards[0]);

        // Assert
        act.Should().Throw<GameplayException>();
    }
}