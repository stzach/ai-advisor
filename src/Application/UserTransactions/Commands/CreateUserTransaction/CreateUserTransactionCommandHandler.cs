using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;

namespace AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;

public class CreateUserTransactionCommandHandler : IRequestHandler<CreateUserTransactionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateUserTransactionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(CreateUserTransactionCommand request, CancellationToken cancellationToken)
    {
        var userProduct = await _context.UserProducts
            .FirstOrDefaultAsync(up => up.ProductId == request.ProductId && up.UserId == _user.Id, cancellationToken);

        if (userProduct is null)
            throw new InvalidOperationException($"Product {request.ProductId} not found for user.");

        var absAmount = Math.Abs(request.Amount);

        if (request.TransactionDirection == TransactionDirection.Outgoing)
            userProduct.AvailableBalance -= absAmount;
        else
            userProduct.AvailableBalance += absAmount;

        var entity = new UserTransaction
        {
            UserId               = _user.Id!,
            ProductId            = request.ProductId,
            TransactionType      = request.TransactionType,
            TransactionCategory  = request.TransactionCategory,
            TransactionDirection = request.TransactionDirection,
            Amount               = request.TransactionDirection == TransactionDirection.Outgoing ? -absAmount : absAmount,
            From                 = request.From,
            To                   = request.To
        };

        _context.UserTransactions.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.TransactionId;
    }
}
