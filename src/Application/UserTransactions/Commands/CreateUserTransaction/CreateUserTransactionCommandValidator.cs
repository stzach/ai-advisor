using AiAdvisor.Domain.Enums;

namespace AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;

public class CreateUserTransactionCommandValidator : AbstractValidator<CreateUserTransactionCommand>
{
    public CreateUserTransactionCommandValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty();

        RuleFor(v => v.TransactionType)
            .IsInEnum();

        RuleFor(v => v.TransactionCategory)
            .IsInEnum();

        RuleFor(v => v.TransactionDirection)
            .IsInEnum();

        RuleFor(v => v.Amount)
            .GreaterThan(0);

        RuleFor(v => v.From)
            .MaximumLength(200);

        RuleFor(v => v.To)
            .MaximumLength(200);
    }
}
