using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.UnitTests.Common;
using AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;
using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AiAdvisor.Application.UnitTests.UserTransactions.Queries;

public class GetUserTransactionsQueryHandlerTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IUser> _user = null!;
    private GetUserTransactionsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _user = new Mock<IUser>();
        _handler = new GetUserTransactionsQueryHandler(_context.Object, _user.Object);
    }

    [Test]
    public async Task ShouldReturnOnlyTransactionsForCurrentUser()
    {
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var product = MakeProduct("Current Account", ProductType.Account);

        var data = new List<UserTransaction>
        {
            MakeTransaction(userId,      product, TransactionType.Payment,  TransactionCategory.Food,      150m),
            MakeTransaction(userId,      product, TransactionType.Transfer, TransactionCategory.Housing,   800m),
            MakeTransaction(otherUserId, product, TransactionType.Payment,  TransactionCategory.Transport, 50m),
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserTransactions).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserTransactionsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldReturnEmptyListWhenUserHasNoTransactions()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _context.Setup(c => c.UserTransactions).Returns(MockDbSetHelper.CreateMockDbSet(new List<UserTransaction>()).Object);

        var result = await _handler.Handle(new GetUserTransactionsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReturnTransactionsOrderedByMostRecentFirst()
    {
        var userId = Guid.NewGuid().ToString();
        var product = MakeProduct("Mastercard Credit Card", ProductType.Card);
        var now = DateTimeOffset.UtcNow;

        var data = new List<UserTransaction>
        {
            MakeTransaction(userId, product, TransactionType.Payment, TransactionCategory.Food,          50m,  now.AddDays(-3)),
            MakeTransaction(userId, product, TransactionType.Payment, TransactionCategory.Entertainment, 120m, now.AddDays(-1)),
            MakeTransaction(userId, product, TransactionType.Payment, TransactionCategory.Transport,     30m,  now.AddDays(-2)),
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserTransactions).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserTransactionsQuery(), CancellationToken.None);

        result[0].Created.ShouldBeGreaterThan(result[1].Created);
        result[1].Created.ShouldBeGreaterThan(result[2].Created);
    }

    [Test]
    public async Task ShouldMapAllFieldsCorrectly()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.AddDays(-1);

        var product = new Product
        {
            ProductId   = productId,
            ProductName = "Mortgage Loan",
            ProductType = ProductType.Loan
        };

        var data = new List<UserTransaction>
        {
            new()
            {
                TransactionId       = transactionId,
                UserId              = userId,
                ProductId           = productId,
                Product             = product,
                TransactionType     = TransactionType.Loan,
                TransactionCategory = TransactionCategory.Housing,
                Amount              = 200000m,
                From                = "Bank",
                To                  = "Customer",
                Created             = created
            }
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserTransactions).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserTransactionsQuery(), CancellationToken.None);
        var dto = result.Single();

        dto.TransactionId.ShouldBe(transactionId);
        dto.ProductId.ShouldBe(productId);
        dto.ProductName.ShouldBe("Mortgage Loan");
        dto.TransactionType.ShouldBe(nameof(TransactionType.Loan));
        dto.TransactionCategory.ShouldBe(nameof(TransactionCategory.Housing));
        dto.Amount.ShouldBe(200000m);
        dto.From.ShouldBe("Bank");
        dto.To.ShouldBe("Customer");
        dto.Created.ShouldBe(created);
    }

    private static Product MakeProduct(string name, ProductType type) => new()
    {
        ProductId   = Guid.NewGuid(),
        ProductName = name,
        ProductType = type
    };

    private static UserTransaction MakeTransaction(
        string userId,
        Product product,
        TransactionType type,
        TransactionCategory category,
        decimal amount,
        DateTimeOffset? created = null) => new()
    {
        TransactionId       = Guid.NewGuid(),
        UserId              = userId,
        ProductId           = product.ProductId,
        Product             = product,
        TransactionType     = type,
        TransactionCategory = category,
        Amount              = amount,
        Created             = created ?? DateTimeOffset.UtcNow
    };
}
