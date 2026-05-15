using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.UnitTests.Common;
using AiAdvisor.Application.UserProducts.Queries.GetUserProducts;
using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AiAdvisor.Application.UnitTests.UserProducts.Queries;

public class GetUserProductsQueryHandlerTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IUser> _user = null!;
    private GetUserProductsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _user = new Mock<IUser>();
        _handler = new GetUserProductsQueryHandler(_context.Object, _user.Object);
    }

    [Test]
    public async Task ShouldReturnOnlyProductsForCurrentUser()
    {
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var product1 = MakeProduct("Mastercard Credit Card", ProductType.Card);
        var product2 = MakeProduct("Current Account", ProductType.Account);
        var product3 = MakeProduct("Savings Account", ProductType.Account);

        var data = new List<UserProduct>
        {
            new() { Id = 1, UserId = userId,      ProductId = product1.ProductId, Product = product1, AvailableBalance = 1500,   IsActive = true  },
            new() { Id = 2, UserId = userId,      ProductId = product2.ProductId, Product = product2, AvailableBalance = 27945,  IsActive = true  },
            new() { Id = 3, UserId = otherUserId, ProductId = product3.ProductId, Product = product3, AvailableBalance = 5000,   IsActive = true  },
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserProducts).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserProductsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldAllBe(p => p.ProductName == "Mastercard Credit Card" || p.ProductName == "Current Account");
    }

    [Test]
    public async Task ShouldReturnEmptyListWhenUserHasNoProducts()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _context.Setup(c => c.UserProducts).Returns(MockDbSetHelper.CreateMockDbSet(new List<UserProduct>()).Object);

        var result = await _handler.Handle(new GetUserProductsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldMapAllFieldsCorrectly()
    {
        var userId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();

        var product = new Product
        {
            ProductId   = productId,
            ProductName = "Visa Debit Card",
            ProductDescription = "Debit Card",
            ProductType = ProductType.Card
        };

        var data = new List<UserProduct>
        {
            new()
            {
                Id               = 7,
                UserId           = userId,
                ProductId        = productId,
                Product          = product,
                AvailableBalance = 2500.50m,
                IsActive         = true,
                CardNumber       = "4111 1111 1111 1111",
                AccountNumber    = null
            }
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserProducts).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserProductsQuery(), CancellationToken.None);
        var dto = result.Single();

        dto.Id.ShouldBe(7);
        dto.ProductId.ShouldBe(productId);
        dto.ProductName.ShouldBe("Visa Debit Card");
        dto.ProductDescription.ShouldBe("Debit Card");
        dto.ProductType.ShouldBe(nameof(ProductType.Card));
        dto.AvailableBalance.ShouldBe(2500.50m);
        dto.IsActive.ShouldBeTrue();
        dto.CardNumber.ShouldBe("4111 1111 1111 1111");
        dto.AccountNumber.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnInactiveProductsAsWell()
    {
        var userId = Guid.NewGuid().ToString();
        var product = MakeProduct("Mortgage Loan", ProductType.Loan);

        var data = new List<UserProduct>
        {
            new() { Id = 1, UserId = userId, ProductId = product.ProductId, Product = product, AvailableBalance = 0, IsActive = false }
        };

        _user.Setup(u => u.Id).Returns(userId);
        _context.Setup(c => c.UserProducts).Returns(MockDbSetHelper.CreateMockDbSet(data).Object);

        var result = await _handler.Handle(new GetUserProductsQuery(), CancellationToken.None);

        result.Count.ShouldBe(1);
        result.Single().IsActive.ShouldBeFalse();
    }

    private static Product MakeProduct(string name, ProductType type) => new()
    {
        ProductId   = Guid.NewGuid(),
        ProductName = name,
        ProductType = type
    };
}
