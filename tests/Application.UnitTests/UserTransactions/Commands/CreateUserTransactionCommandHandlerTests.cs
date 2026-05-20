using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.UnitTests.Common;
using AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;
using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AiAdvisor.Application.UnitTests.UserTransactions.Commands;

public class CreateUserTransactionCommandHandlerTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IUser> _user = null!;
    private CreateUserTransactionCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _user = new Mock<IUser>();
        _handler = new CreateUserTransactionCommandHandler(_context.Object, _user.Object);

        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Test]
    public async Task ShouldDeductBalanceForOutgoingTransaction()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        var userProduct = MakeUserProduct(userId, productId, balance: 1000m);

        SetupContext(new List<UserProduct> { userProduct });

        var command = MakeCommand(productId, TransactionDirection.Outgoing, amount: 250m);

        await _handler.Handle(command, CancellationToken.None);

        userProduct.AvailableBalance.ShouldBe(750m);
    }

    [Test]
    public async Task ShouldNotDeductBalanceForIncomingTransaction()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        var userProduct = MakeUserProduct(userId, productId, balance: 1000m);

        SetupContext(new List<UserProduct> { userProduct });

        var command = MakeCommand(productId, TransactionDirection.Incoming, amount: 500m);

        await _handler.Handle(command, CancellationToken.None);

        userProduct.AvailableBalance.ShouldBe(1000m);
    }

    [Test]
    public async Task ShouldReturnNewTransactionId()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        SetupContext(new List<UserProduct> { MakeUserProduct(userId, productId) });

        var result = await _handler.Handle(MakeCommand(productId, TransactionDirection.Outgoing), CancellationToken.None);

        result.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task ShouldCreateTransactionWithCorrectFields()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        UserTransaction? captured = null;

        var mockTxSet = MockDbSetHelper.CreateMockDbSet(new List<UserTransaction>());
        mockTxSet.Setup(m => m.Add(It.IsAny<UserTransaction>()))
                 .Callback<UserTransaction>(tx => captured = tx);

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserProducts)
                .Returns(MockDbSetHelper.CreateMockDbSet(new List<UserProduct> { MakeUserProduct(userId, productId) }).Object);
        _context.Setup(c => c.UserTransactions).Returns(mockTxSet.Object);

        var command = new CreateUserTransactionCommand
        {
            ProductId            = productId,
            TransactionType      = TransactionType.Transfer,
            TransactionCategory  = TransactionCategory.Housing,
            TransactionDirection = TransactionDirection.Outgoing,
            Amount               = 850m,
            From                 = "Current Account ···1234",
            To                   = "GR16 0140 1250 1234"
        };

        await _handler.Handle(command, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured.ProductId.ShouldBe(productId);
        captured.TransactionType.ShouldBe(TransactionType.Transfer);
        captured.TransactionCategory.ShouldBe(TransactionCategory.Housing);
        captured.TransactionDirection.ShouldBe(TransactionDirection.Outgoing);
        captured.Amount.ShouldBe(850m);
        captured.From.ShouldBe("Current Account ···1234");
        captured.To.ShouldBe("GR16 0140 1250 1234");
    }

    [Test]
    public async Task ShouldThrowWhenProductNotFound()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _context.Setup(c => c.UserProducts)
                .Returns(MockDbSetHelper.CreateMockDbSet(new List<UserProduct>()).Object);

        var command = MakeCommand(Guid.NewGuid(), TransactionDirection.Outgoing);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task ShouldThrowWhenProductBelongsToAnotherUser()
    {
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserProducts)
                .Returns(MockDbSetHelper.CreateMockDbSet(
                    new List<UserProduct> { MakeUserProduct(otherUserId, productId) }).Object);

        var command = MakeCommand(productId, TransactionDirection.Outgoing);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupContext(List<UserProduct> products)
    {
        _user.Setup(u => u.Id).Returns(products.FirstOrDefault()?.UserId ?? Guid.NewGuid().ToString());
        _context.Setup(c => c.UserProducts)
                .Returns(MockDbSetHelper.CreateMockDbSet(products).Object);
        _context.Setup(c => c.UserTransactions)
                .Returns(MockDbSetHelper.CreateMockDbSet(new List<UserTransaction>()).Object);
    }

    private static UserProduct MakeUserProduct(string userId, Guid productId, decimal balance = 5000m) => new()
    {
        UserId           = userId,
        ProductId        = productId,
        AvailableBalance = balance,
        IsActive         = true
    };

    private static CreateUserTransactionCommand MakeCommand(
        Guid productId,
        TransactionDirection direction,
        decimal amount = 100m) => new()
    {
        ProductId            = productId,
        TransactionType      = TransactionType.Payment,
        TransactionCategory  = TransactionCategory.Other,
        TransactionDirection = direction,
        Amount               = amount,
        From                 = "Test Account",
        To                   = "Recipient"
    };
}
