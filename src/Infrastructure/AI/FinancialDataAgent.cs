using AiAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

/// <summary>
/// Agent 1: Fetches user's financial data and builds personalized system prompt
/// </summary>
public interface IFinancialDataAgent
{
    Task<string> BuildUserSystemPromptAsync(string userId, CancellationToken ct);
}

public class FinancialDataAgent : IFinancialDataAgent
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FinancialDataAgent> _logger;

    public FinancialDataAgent(ApplicationDbContext dbContext, ILogger<FinancialDataAgent> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> BuildUserSystemPromptAsync(string userId, CancellationToken ct)
    {
        try
        {
            // Fetch user products (accounts and cards)
            var userProducts = await _dbContext.UserProducts
                .Where(up => up.UserId == userId && up.IsActive)
                .Include(up => up.Product)
                .ToListAsync(ct);

            // Fetch user transactions (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
            var userTransactions = await _dbContext.UserTransactions
                .Where(ut => ut.UserId == userId && ut.Created >= thirtyDaysAgo)
                .Include(ut => ut.Product)
                .OrderByDescending(ut => ut.Created)
                .ToListAsync(ct);

            // Build financial profile
            var accountsSection = BuildAccountsSection(userProducts);
            var expensesSummary = BuildExpensesSummary(userTransactions);
            var recentTransactionsSection = BuildRecentTransactionsSection(userTransactions.Take(10));

            var systemPrompt = $"""
                You are a concise AI financial advisor. Always answer in 2-4 sentences or short bullet points.
                Use the following personalized financial profile to give tailored advice:

                ACCOUNTS & CARDS:
                {accountsSection}

                MONTHLY EXPENSES BY CATEGORY:
                {expensesSummary}

                RECENT TRANSACTIONS:
                {recentTransactionsSection}
                """;

            _logger.LogInformation("System prompt built for user {UserId}", userId);
            return systemPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system prompt for user {UserId}", userId);
            throw;
        }
    }

    private string BuildAccountsSection(List<Domain.Entities.UserProduct> products)
    {
        if (products.Count == 0)
            return "No active accounts or cards found.";

        var lines = products.Select(p =>
        {
            var type = p.Product?.ProductType ?? Domain.Enums.ProductType.Account;
            var displayNumber = p.AccountNumber ?? p.CardNumber ?? "****";
            var lastFour = displayNumber.Length > 4 ? displayNumber[^4..] : displayNumber;
            return $"- {p.Product?.ProductName} ({lastFour}): €{p.AvailableBalance:F2}";
        });

        return string.Join("\n", lines);
    }

    private string BuildExpensesSummary(List<Domain.Entities.UserTransaction> transactions)
    {
        if (transactions.Count == 0)
            return "No transactions found.";

        var expenses = transactions
            .Where(t => t.TransactionDirection.ToString() == "Outgoing")
            .GroupBy(t => t.TransactionCategory)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Sum(t => t.Amount)
            );

        if (expenses.Count == 0)
            return "No expenses found.";

        var total = expenses.Values.Sum();
        var lines = expenses
            .OrderByDescending(x => x.Value)
            .Select(x => $"- {x.Key}: €{x.Value:F2} ({(x.Value / total * 100):F0}%)")
            .ToList();

        lines.Add($"- Total: €{total:F2}");

        return string.Join("\n", lines);
    }

    private string BuildRecentTransactionsSection(IEnumerable<Domain.Entities.UserTransaction> transactions)
    {
        var lines = transactions.Select(t =>
        {
            var direction = t.TransactionDirection.ToString() == "Incoming" ? "+" : "-";
            var counterparty = t.TransactionDirection.ToString() == "Incoming" ? t.From : t.To;
            return $"- {t.Created:d MMM}: {counterparty} ({direction}€{t.Amount:F2}) [{t.TransactionCategory}]";
        });

        return string.Join("\n", lines);
    }
}
