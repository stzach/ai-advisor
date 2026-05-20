using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.UserProducts.Queries.GetUserProducts;

[Authorize]
public record GetUserProductsQuery : IRequest<List<UserProductDto>>;
