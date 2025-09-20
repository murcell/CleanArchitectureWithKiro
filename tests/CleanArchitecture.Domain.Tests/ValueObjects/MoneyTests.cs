using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;

namespace CleanArchitecture.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMoney()
    {
        // Arrange
        var amount = 99.99m;
        var currency = "USD";
        
        // Act
        var money = Money.Create(amount, currency);
        
        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }
    
    [Fact]
    public void Create_WithDecimalPlaces_ShouldRoundToTwoPlaces()
    {
        // Arrange
        var amount = 99.999m;
        var currency = "USD";
        
        // Act
        var money = Money.Create(amount, currency);
        
        // Assert
        money.Amount.Should().Be(100.00m);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidCurrency_ShouldThrowDomainException(string invalidCurrency)
    {
        // Act & Assert
        var action = () => Money.Create(100m, invalidCurrency);
        action.Should().Throw<DomainException>()
            .WithMessage("Currency cannot be null or empty.");
    }
    
    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Create_WithInvalidCurrencyLength_ShouldThrowDomainException(string invalidCurrency)
    {
        // Act & Assert
        var action = () => Money.Create(100m, invalidCurrency);
        action.Should().Throw<DomainException>()
            .WithMessage("Currency must be a 3-letter ISO code.");
    }
    
    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Act & Assert
        var action = () => Money.Create(-100m, "USD");
        action.Should().Throw<DomainException>()
            .WithMessage("Money amount cannot be negative.");
    }
    
    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");
        
        // Act
        var result = money1.Add(money2);
        
        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Add_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "EUR");
        
        // Act & Assert
        var action = () => money1.Add(money2);
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot add different currencies: USD and EUR");
    }
    
    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(30m, "USD");
        
        // Act
        var result = money1.Subtract(money2);
        
        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Subtract_ResultingInNegative_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = Money.Create(50m, "USD");
        var money2 = Money.Create(100m, "USD");
        
        // Act & Assert
        var action = () => money1.Subtract(money2);
        action.Should().Throw<DomainException>()
            .WithMessage("Subtraction would result in negative amount.");
    }
    
    [Fact]
    public void Multiply_WithPositiveFactor_ShouldReturnProduct()
    {
        // Arrange
        var money = Money.Create(100m, "USD");
        var factor = 1.5m;
        
        // Act
        var result = money.Multiply(factor);
        
        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Multiply_WithNegativeFactor_ShouldThrowDomainException()
    {
        // Arrange
        var money = Money.Create(100m, "USD");
        
        // Act & Assert
        var action = () => money.Multiply(-1.5m);
        action.Should().Throw<DomainException>()
            .WithMessage("Multiplication factor cannot be negative.");
    }
    
    [Fact]
    public void Equals_WithSameAmountAndCurrency_ShouldReturnTrue()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(100m, "USD");
        
        // Act & Assert
        money1.Equals(money2).Should().BeTrue();
        (money1 == money2).Should().BeTrue();
    }
    
    [Fact]
    public void Equals_WithDifferentAmount_ShouldReturnFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(200m, "USD");
        
        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }
    
    [Fact]
    public void Equals_WithDifferentCurrency_ShouldReturnFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(100m, "EUR");
        
        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var money = Money.Create(100.5m, "USD");
        
        // Act & Assert
        money.ToString().Should().Be("100.50 USD");
    }
    
    [Fact]
    public void OperatorAdd_ShouldWork()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");
        
        // Act
        var result = money1 + money2;
        
        // Assert
        result.Amount.Should().Be(150m);
    }
    
    [Fact]
    public void OperatorSubtract_ShouldWork()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(30m, "USD");
        
        // Act
        var result = money1 - money2;
        
        // Assert
        result.Amount.Should().Be(70m);
    }
    
    [Fact]
    public void OperatorMultiply_ShouldWork()
    {
        // Arrange
        var money = Money.Create(100m, "USD");
        
        // Act
        var result = money * 2m;
        
        // Assert
        result.Amount.Should().Be(200m);
    }
}