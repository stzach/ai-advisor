using AiAdvisor.Application.Common.Interfaces;

namespace AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;

public class GetUserTransactionsQueryHandler : IRequestHandler<GetUserTransactionsQuery, List<UserTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetUserTransactionsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<UserTransactionDto>> Handle(GetUserTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.UserTransactions
            .AsNoTracking()
            .Where(t => t.UserId == _user.Id)
            .Where(t => request.From == null || t.Created >= request.From)
            .Where(t => request.To   == null || t.Created <= request.To)
            .Select(t => new UserTransactionDto
            {
                TransactionId       = t.TransactionId,
                ProductId           = t.ProductId,
                ProductName         = t.Product.ProductName,
                TransactionType     = t.TransactionType.ToString(),
                TransactionCategory = t.TransactionCategory.ToString(),
                Amount              = t.Amount,
                From                = t.From,
                To                  = t.To,
                Created             = t.Created
            })
            .OrderByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }
}
