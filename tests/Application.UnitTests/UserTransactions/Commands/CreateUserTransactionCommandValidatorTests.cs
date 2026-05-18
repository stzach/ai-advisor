using AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;
using AiAdvisor.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace AiAdvisor.Application.UnitTests.UserTransactions.Commands;

public class CreateUserTransactionCommandValidatorTests
{
    private CreateUserTransactionCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateUserTransactionCommandValidator();
    }

    [Test]
    public async Task ShouldPassForValidCommand()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldFailWhenProductIdIsEmpty()
    {
        var command = ValidCommand() with { ProductId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.ProductId));
    }

    [Test]
    public async Task ShouldFailWhenAmountIsZero()
    {
        var command = ValidCommand() with { Amount = 0 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Amount));
    }

    [Test]
    public async Task ShouldFailWhenAmountIsNegative()
    {
        var command = ValidCommand() with { Amount = -50m };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Amount));
    }

    [Test]
    public async Task ShouldFailWhenTransactionTypeIsInvalid()
    {
        var command = ValidCommand() with { TransactionType = (TransactionType)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransactionType));
    }

    [Test]
    public async Task ShouldFailWhenTransactionCategoryIsInvalid()
    {
        var command = ValidCommand() with { TransactionCategory = (TransactionCategory)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransactionCategory));
    }

    [Test]
    public async Task ShouldFailWhenTransactionDirectionIsInvalid()
    {
        var command = ValidCommand() with { TransactionDirection = (TransactionDirection)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.TransactionDirection));
    }

    [Test]
    public async Task ShouldFailWhenFromExceedsMaxLength()
    {
        var command = ValidCommand() with { From = new string('x', 201) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.From));
    }

    [Test]
    public async Task ShouldFailWhenToExceedsMaxLength()
    {
        var command = ValidCommand() with { To = new string('x', 201) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.To));
    }

    [Test]
    public async Task ShouldPassWhenFromAndToAreNull()
    {
        var command = ValidCommand() with { From = null, To = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldPassWhenFromAndToAreAtMaxLength()
    {
        var command = ValidCommand() with { From = new string('x', 200), To = new string('y', 200) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateUserTransactionCommand ValidCommand() => new()
    {
        ProductId            = Guid.NewGuid(),
        TransactionType      = TransactionType.Payment,
        TransactionCategory  = TransactionCategory.Food,
        TransactionDirection = TransactionDirection.Outgoing,
        Amount               = 100m,
        From                 = "Current Account",
        To                   = "Amazon"
    };
}
