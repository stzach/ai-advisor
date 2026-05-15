using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.UserProducts.Queries.GetUserProducts;

[Authorize]
public record GetUserProductsQuery : IRequest<List<UserProductDto>>;

public class GetUserProductsQueryHandler : IRequestHandler<GetUserProductsQuery, List<UserProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetUserProductsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<UserProductDto>> Handle(GetUserProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.UserProducts
            .AsNoTracking()
            .Where(up => up.UserId == _user.Id)
            .Select(up => new UserProductDto
            {
                Id             = up.Id,
                ProductId      = up.ProductId,
                ProductName    = up.Product.ProductName,
                ProductDescription = up.Product.ProductDescription,
                ProductType    = up.Product.ProductType.ToString(),
                AvailableBalance = up.AvailableBalance,
                IsActive       = up.IsActive,
                CardNumber     = up.CardNumber,
                AccountNumber  = up.AccountNumber
            })
            .ToListAsync(cancellationToken);
    }
}