using AiAdvisor.Domain.Entities;

namespace AiAdvisor.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<Product> Products { get; }

    DbSet<UserProduct> UserProducts { get; }

    DbSet<UserTransaction> UserTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
