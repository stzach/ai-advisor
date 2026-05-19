using AiAdvisor.Domain.Constants;
using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;
using AiAdvisor.Domain.ValueObjects;
using AiAdvisor.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

// You must first install `dotnet tool install --global dotnet-ef`
// To create a new migration script you must run the following command from the solution root directory.
// dotnet ef migrations add MIGRATION_NAME `--project src/Infrastructure/Infrastructure.csproj ` --startup-project src/Web/Web.csproj

public class ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    private static readonly Guid MastercardId   = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid VisaDebitId    = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid CurrentAccId   = new("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly Guid SavingAccId    = new("d4e5f6a7-b8c9-0123-def0-234567890123");
    private static readonly Guid CyberInsId     = new("e5f6a7b8-c9d0-1234-ef01-345678901234");
    private static readonly Guid MortgageLoanId = new("f6a7b8c9-d0e1-2345-f012-456789012345");
    private static readonly Guid PersonalLoanId = new("a7b8c9d0-e1f2-3456-0123-567890123456");

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }

        await UpsertUserAsync("tzachristas",  "tzachristas@gmail.com",       "Stefanos",     "Tzachristas", "Asdf135!");
        await UpsertUserAsync("kafousis",     "kafousis@gmail.com",          "Lampros",      "Kafousis",    "Asdf135!");
        await UpsertUserAsync("geronymakis",  "geronymakis@gmail.com",       "Theodoros",    "Geronymakis", "Asdf135!");
        await UpsertUserAsync("kotrotsos",    "kotrotsos@gmail.com",         "Konstantinos", "Kotrotsos",   "Asdf135!");
        await UpsertUserAsync("komliki",      "christinakomliki@gmail.com",  "Christina",    "Komliki",     "Asdf135!");
        await UpsertUserAsync("billGates",    "plousios@gmail.com",          "Bill",         "Gates",       "Asdf135!");



        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            _context.TodoLists.Add(new TodoList
            {
                Title = "Tasks",
                Colour = Colour.Green,
                Items =
                {
                    new TodoItem { Title = "Make a todo list 📃" },
                    new TodoItem { Title = "Check off the first item ✅" },
                    new TodoItem { Title = "Realise you've already done two things on the list! 🤯"},
                    new TodoItem { Title = "Reward yourself with a nice, long nap 🏆" },
                }
            });

            await _context.SaveChangesAsync();
        }

        await TrySeedProductsAsync();
        await TrySeedUserProductsAsync();
        await TrySeedUserTransactionsAsync();
    }

    private async Task TrySeedProductsAsync()
    {
        var products = new List<Product>
        {
            new() { ProductId = MastercardId,   ProductName = "Mastercard Credit Card", ProductDescription = "Credit Card",     ProductPrice = 0, ProductType = ProductType.Card      },
            new() { ProductId = VisaDebitId,    ProductName = "Visa Debit Card",        ProductDescription = "Debit Card",      ProductPrice = 0, ProductType = ProductType.Card      },
            new() { ProductId = CurrentAccId,   ProductName = "Current Account",        ProductDescription = "Current Account", ProductPrice = 0, ProductType = ProductType.Account   },
            new() { ProductId = SavingAccId,    ProductName = "Saving Account",         ProductDescription = "Saving Account",  ProductPrice = 0, ProductType = ProductType.Account   },
            new() { ProductId = CyberInsId,     ProductName = "Cyber Insurance",        ProductDescription = "Cyber Insurance", ProductPrice = 0, ProductType = ProductType.Insurance },
            new() { ProductId = MortgageLoanId, ProductName = "Mortgage Loan",          ProductDescription = "Mortgage Loan",   ProductPrice = 0, ProductType = ProductType.Loan      },
            new() { ProductId = PersonalLoanId, ProductName = "Personal Loan",          ProductDescription = "Personal Loan",   ProductPrice = 0, ProductType = ProductType.Loan      },
        };

        var ids = products.Select(p => p.ProductId).ToList();
        var existing = await _context.Products
            .Where(p => ids.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var product in products)
        {
            if (existing.TryGetValue(product.ProductId, out var tracked))
            {
                tracked.ProductName        = product.ProductName;
                tracked.ProductDescription = product.ProductDescription;
                tracked.ProductPrice       = product.ProductPrice;
                tracked.ProductType        = product.ProductType;
            }
            else
            {
                _context.Products.Add(product);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task TrySeedUserProductsAsync()
    {
        var komliki = await _userManager.FindByNameAsync("komliki");
        if (komliki is not null)
        {
            await UpsertUserProductsAsync(komliki.Id, new List<UserProduct>
            {
                new() { UserId = komliki.Id, ProductId = CurrentAccId,  AvailableBalance = 4500,  AccountNumber = "GR13 1122 3344 5566 7788 9900 112", IsActive = true },
                new() { UserId = komliki.Id, ProductId = MastercardId,  AvailableBalance = 1500,  CardNumber    = "1234 8255 7654 5733",                IsActive = true },
            });
        }

        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null)
        {
            await UpsertUserProductsAsync(tzachristas.Id, new List<UserProduct>
            {
                new() { UserId = tzachristas.Id, ProductId = CurrentAccId,   AvailableBalance = 27945,    AccountNumber = "GR13 5678 2392 1690 9372 1847 123", IsActive = true },
                new() { UserId = tzachristas.Id, ProductId = CurrentAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 3421 9275 1235 1095 912", IsActive = true },
                new() { UserId = tzachristas.Id, ProductId = SavingAccId,    AvailableBalance = 69785,    AccountNumber = "GR13 1234 2222 6773 9421 5342 280", IsActive = true },
                new() { UserId = tzachristas.Id, ProductId = MortgageLoanId, AvailableBalance = 200000,   AccountNumber = "GR45 6543 3333 7832 4723 1239 931", IsActive = true },
                new() { UserId = tzachristas.Id, ProductId = CyberInsId,     AvailableBalance = 0,                                                             IsActive = true },
                new() { UserId = tzachristas.Id, ProductId = MastercardId,   AvailableBalance = 2820,     CardNumber    = "5555 5555 5555 4444",                IsActive = true }
            });
        }

        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null)
        {
            await UpsertUserProductsAsync(geronymakis.Id, new List<UserProduct>
            {
                new() { UserId = geronymakis.Id, ProductId = CurrentAccId,  AvailableBalance = 245,      AccountNumber = "GR13 6723 9388 6371 7319 1422 846", IsActive = true },
                new() { UserId = geronymakis.Id, ProductId = SavingAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 1111 5784 1235 1095 732", IsActive = true },
                new() { UserId = geronymakis.Id, ProductId = SavingAccId,   AvailableBalance = 2959.51m, AccountNumber = "GR13 7842 1234 9876 2637 1835 892", IsActive = true },
                new() { UserId = geronymakis.Id, ProductId = MastercardId,  AvailableBalance = 567,      CardNumber    = "1254 2333 3444 5555",                IsActive = true },
                new() { UserId = geronymakis.Id, ProductId = VisaDebitId,   AvailableBalance = 0,        CardNumber    = "6732 1212 6782 2333",                IsActive = true }
            });
        }

        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null)
        {
            await UpsertUserProductsAsync(kotrotsos.Id, new List<UserProduct>
            {
                new() { UserId = kotrotsos.Id, ProductId = CurrentAccId, AvailableBalance = 5672.89m, AccountNumber = "GR13 4555 1111 6789 1234 7890 543", IsActive = true },
                new() { UserId = kotrotsos.Id, ProductId = CurrentAccId, AvailableBalance = 1273.95m, AccountNumber = "GR13 6732 2323 1480 1780 9263 092", IsActive = true },
                new() { UserId = kotrotsos.Id, ProductId = SavingAccId,  AvailableBalance = 2959.51m, AccountNumber = "GR13 9845 1230 4567 8901 2345 678", IsActive = true },
                new() { UserId = kotrotsos.Id, ProductId = VisaDebitId,  AvailableBalance = 0,        CardNumber    = "5673 4567 1241 4523",                IsActive = true }
            });
        }

        var billGates = await _userManager.FindByNameAsync("billGates");
        if (billGates is not null)
        {
            await UpsertUserProductsAsync(billGates.Id, new List<UserProduct>
            {
                new() { UserId = billGates.Id, ProductId = MastercardId,   AvailableBalance = 50000m,    CardNumber    = "4916 2345 6789 0123",                IsActive = true },
                new() { UserId = billGates.Id, ProductId = VisaDebitId,    AvailableBalance = 25000m,    CardNumber    = "4539 1488 0343 6467",                IsActive = true },
                new() { UserId = billGates.Id, ProductId = CurrentAccId,   AvailableBalance = 200000m,   AccountNumber = "GR13 0110 2250 0000 0012 3456 789", IsActive = true },
                new() { UserId = billGates.Id, ProductId = CurrentAccId,   AvailableBalance = 350000m,   AccountNumber = "GR13 0260 2250 0001 2000 2330 113", IsActive = true },
                new() { UserId = billGates.Id, ProductId = SavingAccId,    AvailableBalance = 500000m,   AccountNumber = "GR13 0140 3250 0000 0023 4567 890", IsActive = true },
                new() { UserId = billGates.Id, ProductId = SavingAccId,    AvailableBalance = 300000m,   AccountNumber = "GR13 0110 2250 0000 0056 7890 456", IsActive = true },
                new() { UserId = billGates.Id, ProductId = MortgageLoanId, AvailableBalance = 1500000m,  AccountNumber = "GR45 1234 5678 9012 3456 7890 123", IsActive = true },
                new() { UserId = billGates.Id, ProductId = PersonalLoanId, AvailableBalance = 150000m,   AccountNumber = "GR45 1234 5678 9012 3456 7890 456", IsActive = true },
                new() { UserId = billGates.Id, ProductId = CyberInsId,     AvailableBalance = 0m,                                                             IsActive = true }
            });
        }

        var kafousis = await _userManager.FindByNameAsync("kafousis");
        if (kafousis is not null)
        {
            await UpsertUserProductsAsync(kafousis.Id, new List<UserProduct>
            {
                new() { UserId = kafousis.Id, ProductId = CurrentAccId,   AvailableBalance = 18500.00m, AccountNumber = "GR13 7821 4532 1098 7654 3210 987", IsActive = true },
                new() { UserId = kafousis.Id, ProductId = SavingAccId,    AvailableBalance = 45000.00m, AccountNumber = "GR13 6543 8921 4567 2345 8901 234", IsActive = true },
                new() { UserId = kafousis.Id, ProductId = MastercardId,   AvailableBalance =  3200.00m, CardNumber    = "4716 2837 5948 1023",                IsActive = true },
                new() { UserId = kafousis.Id, ProductId = VisaDebitId,    AvailableBalance =     0.00m, CardNumber    = "4539 7812 3456 9087",                IsActive = true },
                new() { UserId = kafousis.Id, ProductId = PersonalLoanId, AvailableBalance = 25000.00m, AccountNumber = "GR45 2345 6789 0123 4567 8901 234", IsActive = true },
                new() { UserId = kafousis.Id, ProductId = CyberInsId,     AvailableBalance =     0.00m,                                                      IsActive = true },
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpsertUserProductsAsync(string userId, List<UserProduct> products)
    {
        var existing = await _context.UserProducts
            .Where(up => up.UserId == userId)
            .ToListAsync();

        foreach (var product in products)
        {
            var tracked = product.AccountNumber is not null
                ? existing.FirstOrDefault(e => e.AccountNumber == product.AccountNumber)
                : product.CardNumber is not null
                    ? existing.FirstOrDefault(e => e.CardNumber == product.CardNumber)
                    : existing.FirstOrDefault(e => e.ProductId == product.ProductId);

            if (tracked is not null)
            {
                tracked.AvailableBalance = product.AvailableBalance;
                tracked.IsActive         = product.IsActive;
            }
            else
            {
                _context.UserProducts.Add(product);
            }
        }
    }

    private async Task TrySeedUserTransactionsAsync()
    {
        // komliki — Mastercard Credit Card: Payment transactions across all categories
        var komliki = await _userManager.FindByNameAsync("komliki");
        if (komliki is not null)
        {
            await UpsertTransactionsAsync(new List<UserTransaction>
            {
                new UserTransaction { TransactionId = new Guid("7b7a3f0e-4dcb-4d4a-b6a9-5f2a4f8d9c01"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Amazon Fresh",     TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = -85.50m  },
                new UserTransaction { TransactionId = new Guid("1c9d5a77-8e23-4d72-a2ef-3b7d0c5e91fa"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Netflix",           TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = -12.99m  },
                new UserTransaction { TransactionId = new Guid("6e2b1d9f-7c44-4af0-8a63-1d7e5b2c9f80"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "IKEA",              TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = -245.00m },
                new UserTransaction { TransactionId = new Guid("d0f9a2c7-1b55-4e6d-b3f8-7a1c2d4e9b65"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "DEI Electric Bill", TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = -94.80m  },
                new UserTransaction { TransactionId = new Guid("2a8e4c1d-9f30-4b72-8d6e-5c1a7f3b0d92"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Apple Store",       TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -999.00m }
            });
        }

        // tzachristas — Current Account x2, Saving Account, Mortgage Loan, Cyber Insurance, Mastercard
        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null)
        {
            await UpsertTransactionsAsync(new List<UserTransaction>
            {
                // Current Account — outgoing transfer to savings
                new UserTransaction { TransactionId = new Guid("3f1c8d5b-6a77-4e20-b9d4-2c5e7a1f8b63"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -2000.00m },
                // Saving Account — transfer in from current
                new UserTransaction { TransactionId = new Guid("4c7d1a9e-2b63-4e5f-a8d1-9b0c2f7e6a54"), UserId = tzachristas.Id, ProductId = SavingAccId,    From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  2000.00m },
                // Cyber Insurance — annual premium
                new UserTransaction { TransactionId = new Guid("0e5c8a2d-9f41-4b76-b3d7-6a1c5e2f8d90"), UserId = tzachristas.Id, ProductId = CyberInsId,     From = "GR13 5678 2392 1690 9372 1847 123", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -120.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("f7a1d3c5-2b84-4e69-8c0f-5d2a7b1e9c36"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "5555 5555 5555 4444",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = -210.00m  },
                new UserTransaction { TransactionId = new Guid("1b4e7d9a-5c20-4f81-a3d6-7e2c1f8b5a94"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "5555 5555 5555 4444",                  To = "Uber",                              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = -25.50m   },
                new UserTransaction { TransactionId = new Guid("e3c9a5d1-6b47-4e22-b8f0-2d7a1c5e9f63"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "5555 5555 5555 4444",                  To = "Steam Games",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = -59.99m   },
                new UserTransaction { TransactionId = new Guid("7d2a8f1c-3e95-4b64-a0d7-9c5e2b1f6a48"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "5555 5555 5555 4444",                  To = "COSMOTE Internet",                  TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = -34.90m   },
                new UserTransaction { TransactionId = new Guid("2f6c1a8d-4b73-4e59-b1d2-5a7c9f3e0d84"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "5555 5555 5555 4444",                  To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = -189.00m  }
            });
        }

        // geronymakis — Current Account, Saving Account x2, Mastercard, Visa Debit
        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null)
        {
            await UpsertTransactionsAsync(new List<UserTransaction>
            {
                // Current Account — savings transfer out
                new UserTransaction { TransactionId = new Guid("6a9d3e1c-5b80-4f22-b7c1-8d2a5e7f4c93"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -500.00m  },
                // Saving Accounts — transfers in
                new UserTransaction { TransactionId = new Guid("3c7f1b5a-9d24-4e68-a2f0-1e5c8b7d6a41"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  500.00m  },
                new UserTransaction { TransactionId = new Guid("b8a2d6c1-4f57-4e90-b3d5-7c1a2f8e9d60"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  200.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("5e1b9a4d-2c83-4f71-a6d8-0b7c5e1f2a94"), UserId = geronymakis.Id, ProductId = MastercardId, From = "1254 2333 3444 5555",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = -156.78m  },
                new UserTransaction { TransactionId = new Guid("a1d7c5e9-3b46-4e28-b0f2-9c5a7d1e6f83"), UserId = geronymakis.Id, ProductId = MastercardId, From = "1254 2333 3444 5555",                  To = "Spotify",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = -9.99m    },
                new UserTransaction { TransactionId = new Guid("0c8e2a5d-7f31-4b64-a9d1-2e5c7b3f8a40"), UserId = geronymakis.Id, ProductId = MastercardId, From = "1254 2333 3444 5555",                  To = "OTE Internet",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = -34.90m   },
                new UserTransaction { TransactionId = new Guid("f2a5d1c7-8b94-4e53-b6d0-1c7a9e2f5b68"), UserId = geronymakis.Id, ProductId = MastercardId, From = "1254 2333 3444 5555",                  To = "IKEA Athens",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = -312.00m  },
                // Visa Debit — day-to-day spending
                new UserTransaction { TransactionId = new Guid("4d9a1e6c-5b27-4f80-a3d2-7c1e5f8b9a34"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "6732 1212 6782 2333",                        To = "ISAP Metro Ticket",                 TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = -2.50m    },
                new UserTransaction { TransactionId = new Guid("8c5f2a1d-6e73-4b49-b0d7-3a9c1e5f2d64"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "6732 1212 6782 2333",                        To = "Local Bakery",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = -8.50m    },
                new UserTransaction { TransactionId = new Guid("1e7c4a9d-2b58-4f31-a6d0-5c8e2f7b1a93"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "6732 1212 6782 2333",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -22.30m   }
            });
        }

        // kotrotsos — Current Account x2, Saving Account, Visa Debit
        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null)
        {
            await UpsertTransactionsAsync(new List<UserTransaction>
            {
                // Current Account — one-off utility payment
                new UserTransaction { TransactionId = new Guid("e8d1a4c7-5b92-4f20-a6d3-2c7e1f5b9a48"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 4555 1111 6789 1234 7890 543", To = "DEI Power",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = -78.20m   },
                // Visa Debit — one-off spending
                new UserTransaction { TransactionId = new Guid("3a8d1f5c-6e27-4b40-b9d2-1f7c5a8e2d64"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "5673 4567 1241 4523",                        To = "Starbucks",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = -6.50m    },
                new UserTransaction { TransactionId = new Guid("b1c7e4a9-5d38-4f62-a3d0-8e2b5c1f7a94"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "5673 4567 1241 4523",                        To = "EasyBus",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = -3.50m    },
                new UserTransaction { TransactionId = new Guid("5d2a9c1e-7b46-4e83-b0f5-2c1a7e9d6f38"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "5673 4567 1241 4523",                        To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = -54.99m   },
                new UserTransaction { TransactionId = new Guid("f9b1d5a2-3e74-4f68-a2d1-6c5e8b7f1a40"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "5673 4567 1241 4523",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -18.60m   }
            });
        }

        // billGates — all products, 2 years of transactions
        var billGates = await _userManager.FindByNameAsync("billGates");
        if (billGates is not null)
        {
            await UpsertTransactionsAsync(new List<UserTransaction>
            {
                // June 2024
                new() { TransactionId = new Guid("c3e95ef9-f883-4f66-8d40-e8e410d8cd42"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("8fa7f733-cc88-4644-99a3-d23c25f971de"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("899dedff-e126-4160-bc29-8f821199cc68"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("48f233df-25d4-4347-bb82-afa71095c59f"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("280cd23a-a149-404c-9f05-6016886212d7"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("e009c5f2-595d-4560-99c6-379da4102a0f"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Harrods Food Hall",                 TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -650.00m },
                new() { TransactionId = new Guid("cc7845f5-a6f1-4b90-97e3-a720188628f8"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // July 2024
                new() { TransactionId = new Guid("7d86434e-3e60-47b2-96e8-9d71476383d4"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("9ad894d0-a961-49de-ad3d-6a0f78a93969"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("0a18aab7-539a-4373-9dd5-820139b3cb1f"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("3d9cf2f9-ff5e-402f-8eb4-c434048e82dc"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("fbb82f04-8e68-4fca-b092-6cc2f74f6162"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("7b4adeb8-0e50-4fdb-a364-5a0f5874eb91"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Netflix Premium",                   TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount =   -299.99m },
                // August 2024
                new() { TransactionId = new Guid("4337304a-e5b1-4d9a-9641-25de63d50c41"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("cc2b1442-8008-4430-9eff-d398862c77e0"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("f61255d0-9aca-4d15-bbf8-eef40390f983"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("d963dc98-a586-46ae-ac2c-723bb398fa7b"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("b9be22d6-43f5-4bb5-afec-dceec867c913"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("b2fcc27f-69ab-42f7-99c0-0f221917a833"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Uber Executive",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -185.00m },
                // September 2024
                new() { TransactionId = new Guid("28e44273-ecbc-4031-8c32-26acc0144039"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("5e4fb859-17be-4ead-b984-1582090627dc"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("1f45b6f0-09c1-4581-966f-341da41fd189"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("a2e1b0e9-920d-4c6d-ba7f-3027a0c4d853"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("caae11e6-9ed8-4b48-b6be-80d8f976e3db"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("ec0f221e-25ba-4077-ae36-abf197413d7f"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "COSMOTE Business",                  TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -340.00m },
                new() { TransactionId = new Guid("14d84abd-a8fc-4fd0-823c-dbed611767aa"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // October 2024
                new() { TransactionId = new Guid("019ad6a2-7aff-4cd7-bfe0-bea6ea83642d"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("2adf7395-92e2-477e-9056-4ee4bc57cfb6"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("217b68e8-cb02-4e3d-bc42-1668374c6502"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("701cd51f-c268-4253-bc6e-6c36cd25e621"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("443172b1-3800-4154-b9e4-2ea8191e6964"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("b9ffd7b2-1a80-4758-8230-a6082dccc0e7"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "IKEA Premium",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -1250.00m },
                // November 2024
                new() { TransactionId = new Guid("bb571eb0-8c6e-4130-ab9d-6b8d3ac38980"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("a0be6037-2838-4b02-bdda-db2fef16991e"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("2b65106e-ba70-4c0b-a614-31c18f8526ac"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("389b3680-362c-412a-9db2-e8f542d4aaf6"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("c22f92a4-7592-4716-8e83-736ef0294208"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("fe4e904c-efc2-4ddd-8e5f-b4a9359d118c"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Fine Dining Athens",                TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -220.00m },
                // December 2024
                new() { TransactionId = new Guid("7d883e4f-bff7-4f88-976e-dc5c84be6e62"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("f1a33ca4-cd85-496f-8249-209d21c4482b"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("86202d22-7c6b-4e83-b3a9-af408361a640"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("339f3327-a61a-4ee7-8a60-bb98f701f5a4"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("1bad49f0-f50f-4d70-bed2-ac4722109510"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("2e6849c9-a93a-4e4a-bcea-0abf1b759c20"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Renovation Works Ltd",              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -3500.00m },
                new() { TransactionId = new Guid("c172c101-9b79-400e-b0a3-4852bf693109"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // January 2025
                new() { TransactionId = new Guid("33abb00c-384c-455e-8869-b9a35897035b"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("4aeb638d-2fa3-4500-a20f-948e3ec33a9c"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("5a516b5f-550a-4110-8ac4-34ae649a78a5"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("c3c4f996-d74b-4a4e-b477-c966fc6b2b88"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("ce857c9b-b1ea-4eb4-a982-61a7c287b253"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("1cd874b0-721f-4bed-80b8-5b2ba49b3bd7"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Whole Foods Market",                TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -580.00m },
                // February 2025
                new() { TransactionId = new Guid("4f75891c-aafa-4902-b95f-c427eecf208d"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("2311d811-f6f6-4823-9605-05fd33802e0f"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("bc8538a2-6334-4357-a81f-917de35892f8"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("ea310b25-abd7-44da-a804-5bedc946bf6c"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("a8ef8cd2-dd79-488f-bf16-5dfdbda363f1"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("67e37b1c-1a5b-466c-bb3c-d5d5f827eb49"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Odeon Cinema VIP",                  TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount =   -180.00m },
                // March 2025
                new() { TransactionId = new Guid("9ac9715e-9765-45aa-a442-8eac17a7b7a3"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("90ed5ec5-5d31-4ac6-9a24-9600ab967a6d"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("7bebf7c2-23cb-42cd-a716-e675c31785ff"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("93fd8153-87bd-458f-85b3-47c9a7edf82b"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("01096079-50e5-45bc-a617-4525b5a0face"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("4dd50f3d-dc5d-454d-a5d8-848aa2c0b3fb"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "DEI Business",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -410.00m },
                new() { TransactionId = new Guid("c44bcfbd-025d-4393-83da-a31f9df1528f"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // April 2025
                new() { TransactionId = new Guid("02aac746-f15e-4a6a-bee0-ede21bc1d605"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("9f6897b2-e50a-42e7-b56a-326a056a1ad6"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("37939484-25e7-42e9-8a46-f46bdc10ddd6"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("51e87b5c-051c-42ca-bee4-6871a7e63894"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("741d2ca7-e95c-4fd5-b9cd-27b931d28537"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("2154267a-6b28-467e-b0b8-ad4ac1433b55"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Interior Design Co.",               TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =   -890.00m },
                // May 2025
                new() { TransactionId = new Guid("7a8fd2e6-02ea-4473-b7a6-a56ffd39d5e1"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("0f1e56c0-75b3-43e0-8076-77a5c87c41a7"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("2b662e5f-19c9-4572-a121-539d14ab6b84"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("65ec8b0b-05e6-408d-a371-4ea1b1f7a78c"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("7e6b5896-d691-4259-9dd9-d5731903f949"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("2b20a843-36b4-4bb1-ae3f-5c696c4fbc1b"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Uber Executive",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -165.00m },
                // June 2025
                new() { TransactionId = new Guid("30b8cf74-8dfc-4f40-a48d-6c13c881ee3e"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("dbdc653d-945a-4017-a0c7-71bbe5e43abb"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("86491244-9de5-4c15-877a-a53066a6e986"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("7997cf59-0fea-40ae-8345-a8c69b19b2f8"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("cd9c2b4b-abae-4ea6-a5ca-52271c6ad26a"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("e70c2e9b-ae32-4474-b14e-dc155b1af19c"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Fine Dining Mykonos",               TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -720.00m },
                new() { TransactionId = new Guid("2fed3401-76c1-42d4-a839-3e4d67d5fa60"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // July 2025
                new() { TransactionId = new Guid("d9c55295-a14e-4b51-9e42-8d9e3f518472"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("1130ff65-d565-45ef-9a0b-7fd1eb00387b"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("e787a536-e4a9-4e3c-8d13-80b44696d469"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("52082c6e-5633-4ed9-ae2c-5a34f295bc25"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("285438bb-5d5e-4bbc-a5cf-5f4f5243c963"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("e34ee279-30e1-48f8-83f7-30c724d0b1c1"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Steam Games",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount =   -450.00m },
                // August 2025
                new() { TransactionId = new Guid("e0453656-4aea-4dad-a69b-977de9c24a6a"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("de194324-c79b-438c-85af-6d298635aff7"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("80ba92a6-248b-4261-ab3b-2c2da8915b9b"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("eadc2140-467f-49a1-9175-786c6f52a2cd"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("ff2235d2-f577-415c-9326-76de619d1c29"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("659b3f74-b28d-4007-ab5c-5d5d301dd881"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "British Airways",                   TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount =  -1250.00m },
                // September 2025
                new() { TransactionId = new Guid("86311f7a-4559-40cc-bb45-11e1c594e791"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("e5148ece-f66f-4dc3-8bb7-b6e273666a10"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("bd5efa24-d3c0-489c-8495-6b224ed310e7"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("3f41be48-bb99-451b-a05d-b8918620c4a5"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("f3bf9c91-2e17-4dca-a775-c0be39a05796"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("6388f494-8c65-42d9-8119-8dbd54b3900d"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "OTE Business",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -385.00m },
                new() { TransactionId = new Guid("dc101156-1794-4e95-bbdb-98c2c528d416"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // October 2025
                new() { TransactionId = new Guid("73a8f8b6-a753-4a8a-b6bb-b5197f9c173f"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("43d75621-fdd1-48af-a774-3ab41b919b5d"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("1ef61fd2-ab9b-4d33-8471-6eca8b1527fb"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("dc324088-5b57-479f-8ee9-886e068c1ea2"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("0115ee02-8da3-43c4-aef3-d3f629477514"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("8104df15-2012-4b75-9f79-e579d65cf355"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Selfridges Shopping",               TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -1100.00m },
                // November 2025
                new() { TransactionId = new Guid("9e3d9af8-44ee-4745-a42b-a6a4616e9f45"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("41473e5b-1c95-4ca9-8791-0fb20f6970fb"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("e87db019-f7d8-466c-bcc2-872a9062fdec"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("55d842a7-5801-453b-8963-e1f043531dd6"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("c42c9107-0c6d-4603-aea0-4191f18233a0"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("86d39d96-65f4-4e3b-932a-d637369f34f6"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Nobu Restaurant",                   TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -265.00m },
                // December 2025
                new() { TransactionId = new Guid("5bf61928-b7b4-4c67-a869-b0c19bae1363"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("3182befb-c05c-462b-9b77-8aa829690698"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("fe9c5539-ce8d-40c4-a17d-cfedd55a67c2"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("921ccd7f-97ac-4350-a0be-5bf8877182d6"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("0416136c-eb08-42f7-a5b2-953ddcd92217"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("0a80f11f-acb1-4021-9a49-f2bdcd755b27"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Christmas Renovation",              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -4200.00m },
                new() { TransactionId = new Guid("e59f0ddb-3a9a-404a-9c74-505ae3be5f8e"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // January 2026
                new() { TransactionId = new Guid("df7a050e-b3f6-44e5-a68c-4eadb506cd6a"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("2e41dc1a-cf42-4d2c-8ffe-5680ab243a99"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("52cce902-08af-451b-86e9-f5be9ae1e045"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("9b385307-44fd-49cc-a44d-358eb9cb8428"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("d9bd9d47-2352-4506-82c9-b3be56f11e5c"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("adbfd05b-5e07-41b7-9d08-cb9369d0f13d"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Whole Foods Premium",               TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -610.00m },
                // February 2026
                new() { TransactionId = new Guid("c31f13a7-42e0-48a6-b7c2-5a75f1e60a5d"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("bc73c35c-f0b8-424e-8c8e-48d3ebe5a7a9"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("5a93cd46-6ef7-48a5-991a-79d19168fe64"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("aeb87716-7bb3-408c-9584-7ddbc539cbbc"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("aa5b45e5-3869-4bda-91ad-9093b25509a6"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("eb44d712-8be8-48f8-91e6-5b28e2a4efaf"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Spotify Premium Family",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount =   -220.00m },
                // March 2026
                new() { TransactionId = new Guid("18297933-272d-4a57-a419-82e4e71ca50d"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("d750c2c3-1c05-4289-808c-a20a22df67c8"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("7642e9b0-3796-48b3-bf53-41d8e2b45c0b"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("eed4d0ab-f042-4128-96de-38702aae2e1f"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("6c1a5f4c-83ac-4c4c-80a7-96a32adf44e3"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("087aeeca-683a-4bd5-ad1e-313695c7bcb4"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "DEI Premium",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount =   -395.00m },
                new() { TransactionId = new Guid("3e3ca661-8adf-4f04-a017-37fdf76a9cd3"), UserId = billGates.Id, ProductId = CyberInsId,     From = "GR13 0110 2250 0000 0012 3456 789", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -1200.00m },
                // April 2026
                new() { TransactionId = new Guid("64fab85c-bab0-4f3e-b339-34d90ad453ec"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("6f081576-4cd1-4cd2-97fb-0c9733be4e97"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("9d62ff8d-b136-420d-986f-f31cdea87bf7"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("af6933ba-b1a9-4a27-a330-24a0550c3e8f"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("83c6f664-0be5-46bd-80cd-4b73fa04d664"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("01b53a63-5b58-4d26-96e5-0cb8d6608972"), UserId = billGates.Id, ProductId = MastercardId,   From = "4916 2345 6789 0123",                  To = "Design Studio Athens",              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =   -960.00m },
                // May 2026
                new() { TransactionId = new Guid("e94b0139-e00d-4094-b154-dca536115808"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "Microsoft Corp",                     To = "GR13 0110 2250 0000 0012 3456 789", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  35000.00m },
                new() { TransactionId = new Guid("478bcb28-72dd-47cf-82b6-141056619f63"), UserId = billGates.Id, ProductId = MortgageLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount =  -8500.00m },
                new() { TransactionId = new Guid("96c8481a-1f93-488b-bd2a-40d068f5b081"), UserId = billGates.Id, ProductId = PersonalLoanId, From = "GR13 0110 2250 0000 0012 3456 789", To = "Personal Finance Account",          TransactionType = TransactionType.Loan,     TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount =  -2000.00m },
                new() { TransactionId = new Guid("e25ccfc8-8d4c-4981-87e9-0f83434b4632"), UserId = billGates.Id, ProductId = CurrentAccId,   From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = -10000.00m },
                new() { TransactionId = new Guid("84a9d871-bede-48b5-943f-bee3c3dd838f"), UserId = billGates.Id, ProductId = SavingAccId,    From = "GR13 0110 2250 0000 0012 3456 789", To = "GR13 0140 3250 0000 0023 4567 890", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  10000.00m },
                new() { TransactionId = new Guid("2b728273-05f6-46fc-b110-8d629991c7e3"), UserId = billGates.Id, ProductId = VisaDebitId,    From = "4539 1488 0343 6467",                        To = "Athens Central Market",             TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount =   -185.00m }
            });
        }

        // ── Monthly transactions Jun 2024 → May 2026 ─────────────────────────────
        var allGeneratedDates = new Dictionary<Guid, DateTimeOffset>();

        if (komliki is not null)
        {
            var tpl = new (Guid, string?, string?, TransactionType, TransactionCategory, TransactionDirection, decimal, int)[]
            {
                (MastercardId, "1234 8255 7654 5733", "AB Supermarket",                      TransactionType.Payment,  TransactionCategory.Food,          TransactionDirection.Outgoing,  -95.00m,  3),
                (MastercardId, "1234 8255 7654 5733", "Netflix",                             TransactionType.Payment,  TransactionCategory.Entertainment, TransactionDirection.Outgoing,  -13.00m,  9),
                (MastercardId, "1234 8255 7654 5733", "Shell Station",                       TransactionType.Payment,  TransactionCategory.Transport,     TransactionDirection.Outgoing,  -65.00m, 16),
                (MastercardId, "1234 8255 7654 5733", "DEI Electric",                        TransactionType.Payment,  TransactionCategory.Utilities,     TransactionDirection.Outgoing,  -90.00m, 22),
                (CurrentAccId, "Employer GR",          "GR13 1122 3344 5566 7788 9900 112",  TransactionType.Transfer, TransactionCategory.Other,         TransactionDirection.Incoming, 2200.00m,  1),
            };
            var r = GenerateMonthlyTransactions(komliki.Id, "txn-k", tpl);
            await UpsertTransactionsAsync(r.Transactions);
            foreach (var kv in r.Dates) allGeneratedDates[kv.Key] = kv.Value;
        }

        if (tzachristas is not null)
        {
            var tpl = new (Guid, string?, string?, TransactionType, TransactionCategory, TransactionDirection, decimal, int)[]
            {
                (CurrentAccId,   "Employer SA",                       "GR13 5678 2392 1690 9372 1847 123", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,  5000.00m,  1),
                (MortgageLoanId, "GR13 5678 2392 1690 9372 1847 123", "Alpha Bank Mortgage",               TransactionType.Loan,     TransactionCategory.Housing,   TransactionDirection.Outgoing,  -850.00m,  5),
                (MastercardId,   "5555 5555 5555 4444",                  "Sklavenitis",                       TransactionType.Payment,  TransactionCategory.Food,      TransactionDirection.Outgoing,  -215.00m,  8),
                (CurrentAccId,   "GR13 5678 2392 1690 9372 1847 123", "GR13 1234 2222 6773 9421 5342 280", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Outgoing, -1500.00m, 12),
                (SavingAccId,    "GR13 5678 2392 1690 9372 1847 123", "GR13 1234 2222 6773 9421 5342 280", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,  1500.00m, 12),
                (CurrentAccId,   "GR13 5678 3421 9275 1235 1095 912", "COSMOTE Business",                  TransactionType.Payment,  TransactionCategory.Utilities, TransactionDirection.Outgoing,  -145.00m, 25),
            };
            var r = GenerateMonthlyTransactions(tzachristas.Id, "txn-tz", tpl);
            await UpsertTransactionsAsync(r.Transactions);
            foreach (var kv in r.Dates) allGeneratedDates[kv.Key] = kv.Value;
        }

        if (geronymakis is not null)
        {
            var tpl = new (Guid, string?, string?, TransactionType, TransactionCategory, TransactionDirection, decimal, int)[]
            {
                (CurrentAccId, "Employer Ltd",                       "GR13 6723 9388 6371 7319 1422 846", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,  1800.00m,  2),
                (CurrentAccId, "GR13 6723 9388 6371 7319 1422 846", "Landlord GR",                       TransactionType.Transfer, TransactionCategory.Housing,   TransactionDirection.Outgoing,  -650.00m,  7),
                (CurrentAccId, "GR13 6723 9388 6371 7319 1422 846", "GR13 5678 1111 5784 1235 1095 732", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Outgoing,  -400.00m, 11),
                (SavingAccId,  "GR13 6723 9388 6371 7319 1422 846", "GR13 5678 1111 5784 1235 1095 732", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,   400.00m, 11),
                (MastercardId, "1254 2333 3444 5555",                  "Sklavenitis",                       TransactionType.Payment,  TransactionCategory.Food,      TransactionDirection.Outgoing,  -125.00m, 16),
                (VisaDebitId,  "6732 1212 6782 2333",                        "ISAP Metro",                        TransactionType.Payment,  TransactionCategory.Transport, TransactionDirection.Outgoing,   -15.00m, 22),
            };
            var r = GenerateMonthlyTransactions(geronymakis.Id, "txn-ge", tpl);
            await UpsertTransactionsAsync(r.Transactions);
            foreach (var kv in r.Dates) allGeneratedDates[kv.Key] = kv.Value;
        }

        if (kotrotsos is not null)
        {
            var tpl = new (Guid, string?, string?, TransactionType, TransactionCategory, TransactionDirection, decimal, int)[]
            {
                (CurrentAccId, "Company GR",                         "GR13 4555 1111 6789 1234 7890 543", TransactionType.Transfer, TransactionCategory.Other,         TransactionDirection.Incoming,  3500.00m,  1),
                (CurrentAccId, "GR13 4555 1111 6789 1234 7890 543", "Landlord Properties",               TransactionType.Transfer, TransactionCategory.Housing,       TransactionDirection.Outgoing,  -800.00m,  8),
                (CurrentAccId, "GR13 4555 1111 6789 1234 7890 543", "GR13 9845 1230 4567 8901 2345 678", TransactionType.Transfer, TransactionCategory.Other,         TransactionDirection.Outgoing, -1000.00m, 12),
                (SavingAccId,  "GR13 4555 1111 6789 1234 7890 543", "GR13 9845 1230 4567 8901 2345 678", TransactionType.Transfer, TransactionCategory.Other,         TransactionDirection.Incoming,  1000.00m, 12),
                (VisaDebitId,  "5673 4567 1241 4523",                        "Cinema Athens",                     TransactionType.Payment,  TransactionCategory.Entertainment, TransactionDirection.Outgoing,   -15.00m, 18),
            };
            var r = GenerateMonthlyTransactions(kotrotsos.Id, "txn-ko", tpl);
            await UpsertTransactionsAsync(r.Transactions);
            foreach (var kv in r.Dates) allGeneratedDates[kv.Key] = kv.Value;
        }

        var kafousis = await _userManager.FindByNameAsync("kafousis");
        if (kafousis is not null)
        {
            var tpl = new (Guid, string?, string?, TransactionType, TransactionCategory, TransactionDirection, decimal, int)[]
            {
                (CurrentAccId,   "Employer GR",                       "GR13 7821 4532 1098 7654 3210 987", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,  4200.00m,  1),
                (PersonalLoanId, "GR13 7821 4532 1098 7654 3210 987", "Personal Finance",                  TransactionType.Loan,     TransactionCategory.Other,     TransactionDirection.Outgoing,  -450.00m,  5),
                (MastercardId,   "4716 2837 5948 1023",                  "Sklavenitis",                       TransactionType.Payment,  TransactionCategory.Food,      TransactionDirection.Outgoing,  -185.00m, 10),
                (VisaDebitId,    "4539 7812 3456 9087",                        "Metro Athens",                      TransactionType.Payment,  TransactionCategory.Transport, TransactionDirection.Outgoing,   -40.00m, 15),
                (CurrentAccId,   "GR13 7821 4532 1098 7654 3210 987", "GR13 6543 8921 4567 2345 8901 234", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Outgoing,  -800.00m, 20),
                (SavingAccId,    "GR13 7821 4532 1098 7654 3210 987", "GR13 6543 8921 4567 2345 8901 234", TransactionType.Transfer, TransactionCategory.Other,     TransactionDirection.Incoming,   800.00m, 20),
            };
            var r = GenerateMonthlyTransactions(kafousis.Id, "txn-ka", tpl);
            await UpsertTransactionsAsync(r.Transactions);
            foreach (var kv in r.Dates) allGeneratedDates[kv.Key] = kv.Value;
        }

        try
        {
            await FixTransactionDatesAsync(allGeneratedDates);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FixTransactionDatesAsync failed; transactions saved but historical dates may be inaccurate.");
        }

        try
        {
            await FixBillGatesTransactionDatesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FixBillGatesTransactionDatesAsync failed; transactions were saved but historical dates may be inaccurate.");
        }
    }

    private async Task UpsertUserAsync(string userName, string email, string firstName, string lastName, string password)
    {
        var existing = await _userManager.FindByNameAsync(userName);
        if (existing is null)
        {
            var user = new ApplicationUser { UserName = userName, Email = email, FirstName = firstName, LastName = lastName };
            await _userManager.CreateAsync(user, password);
        }
        else
        {
            existing.FirstName = firstName;
            existing.LastName  = lastName;
            existing.Email     = email;
            await _userManager.UpdateAsync(existing);
        }
    }

    private async Task UpsertTransactionsAsync(List<UserTransaction> transactions)
    {
        var ids = transactions.Select(t => t.TransactionId).ToList();
        var existing = await _context.UserTransactions
            .Where(ut => ids.Contains(ut.TransactionId))
            .ToDictionaryAsync(ut => ut.TransactionId);

        foreach (var transaction in transactions)
        {
            if (existing.TryGetValue(transaction.TransactionId, out var tracked))
                tracked.Amount = transaction.Amount;
            else
                _context.UserTransactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
    }

    private static Guid DeterministicGuid(string seed)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static (List<UserTransaction> Transactions, Dictionary<Guid, DateTimeOffset> Dates) GenerateMonthlyTransactions(
        string userId,
        string prefix,
        IReadOnlyList<(Guid ProductId, string? From, string? To, TransactionType Type, TransactionCategory Cat, TransactionDirection Dir, decimal Amount, int Day)> templates)
    {
        var transactions = new List<UserTransaction>();
        var dates        = new Dictionary<Guid, DateTimeOffset>();

        for (var m = 0; m < 24; m++)
        {
            var totalMonths = 5 + m;
            var year  = 2024 + totalMonths / 12;
            var month = totalMonths % 12 + 1;

            for (var i = 0; i < templates.Count; i++)
            {
                var t  = templates[i];
                var id = DeterministicGuid($"{prefix}-{year:D4}{month:D2}-{i:D2}");
                transactions.Add(new UserTransaction
                {
                    TransactionId        = id,
                    UserId               = userId,
                    ProductId            = t.ProductId,
                    From                 = t.From,
                    To                   = t.To,
                    TransactionType      = t.Type,
                    TransactionCategory  = t.Cat,
                    TransactionDirection = t.Dir,
                    Amount               = t.Amount,
                });
                dates[id] = new DateTimeOffset(year, month, t.Day, 9, 0, 0, TimeSpan.Zero);
            }
        }

        return (transactions, dates);
    }

    private async Task FixTransactionDatesAsync(Dictionary<Guid, DateTimeOffset> dates)
    {
        foreach (var (id, date) in dates)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE UserTransactions SET Created = {0}, LastModified = {1} WHERE TransactionId = {2}",
                date, date, id);
    }

    private async Task FixBillGatesTransactionDatesAsync()
    {
        var dates = new Dictionary<Guid, DateTimeOffset>
        {
            // Jun 2024
            { new Guid("c3e95ef9-f883-4f66-8d40-e8e410d8cd42"), new DateTimeOffset(2024,  6,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("8fa7f733-cc88-4644-99a3-d23c25f971de"), new DateTimeOffset(2024,  6,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("899dedff-e126-4160-bc29-8f821199cc68"), new DateTimeOffset(2024,  6,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("48f233df-25d4-4347-bb82-afa71095c59f"), new DateTimeOffset(2024,  6, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("280cd23a-a149-404c-9f05-6016886212d7"), new DateTimeOffset(2024,  6, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e009c5f2-595d-4560-99c6-379da4102a0f"), new DateTimeOffset(2024,  6, 15, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("cc7845f5-a6f1-4b90-97e3-a720188628f8"), new DateTimeOffset(2024,  6, 20, 9, 0, 0, TimeSpan.Zero) },
            // Jul 2024
            { new Guid("7d86434e-3e60-47b2-96e8-9d71476383d4"), new DateTimeOffset(2024,  7,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("9ad894d0-a961-49de-ad3d-6a0f78a93969"), new DateTimeOffset(2024,  7,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("0a18aab7-539a-4373-9dd5-820139b3cb1f"), new DateTimeOffset(2024,  7,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("3d9cf2f9-ff5e-402f-8eb4-c434048e82dc"), new DateTimeOffset(2024,  7, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("fbb82f04-8e68-4fca-b092-6cc2f74f6162"), new DateTimeOffset(2024,  7, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("7b4adeb8-0e50-4fdb-a364-5a0f5874eb91"), new DateTimeOffset(2024,  7, 20, 9, 0, 0, TimeSpan.Zero) },
            // Aug 2024
            { new Guid("4337304a-e5b1-4d9a-9641-25de63d50c41"), new DateTimeOffset(2024,  8,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("cc2b1442-8008-4430-9eff-d398862c77e0"), new DateTimeOffset(2024,  8,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("f61255d0-9aca-4d15-bbf8-eef40390f983"), new DateTimeOffset(2024,  8,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("d963dc98-a586-46ae-ac2c-723bb398fa7b"), new DateTimeOffset(2024,  8, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("b9be22d6-43f5-4bb5-afec-dceec867c913"), new DateTimeOffset(2024,  8, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("b2fcc27f-69ab-42f7-99c0-0f221917a833"), new DateTimeOffset(2024,  8, 25, 9, 0, 0, TimeSpan.Zero) },
            // Sep 2024
            { new Guid("28e44273-ecbc-4031-8c32-26acc0144039"), new DateTimeOffset(2024,  9,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("5e4fb859-17be-4ead-b984-1582090627dc"), new DateTimeOffset(2024,  9,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("1f45b6f0-09c1-4581-966f-341da41fd189"), new DateTimeOffset(2024,  9,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("a2e1b0e9-920d-4c6d-ba7f-3027a0c4d853"), new DateTimeOffset(2024,  9, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("caae11e6-9ed8-4b48-b6be-80d8f976e3db"), new DateTimeOffset(2024,  9, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("ec0f221e-25ba-4077-ae36-abf197413d7f"), new DateTimeOffset(2024,  9, 18, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("14d84abd-a8fc-4fd0-823c-dbed611767aa"), new DateTimeOffset(2024,  9, 20, 9, 0, 0, TimeSpan.Zero) },
            // Oct 2024
            { new Guid("019ad6a2-7aff-4cd7-bfe0-bea6ea83642d"), new DateTimeOffset(2024, 10,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2adf7395-92e2-477e-9056-4ee4bc57cfb6"), new DateTimeOffset(2024, 10,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("217b68e8-cb02-4e3d-bc42-1668374c6502"), new DateTimeOffset(2024, 10,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("701cd51f-c268-4253-bc6e-6c36cd25e621"), new DateTimeOffset(2024, 10, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("443172b1-3800-4154-b9e4-2ea8191e6964"), new DateTimeOffset(2024, 10, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("b9ffd7b2-1a80-4758-8230-a6082dccc0e7"), new DateTimeOffset(2024, 10, 12, 9, 0, 0, TimeSpan.Zero) },
            // Nov 2024
            { new Guid("bb571eb0-8c6e-4130-ab9d-6b8d3ac38980"), new DateTimeOffset(2024, 11,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("a0be6037-2838-4b02-bdda-db2fef16991e"), new DateTimeOffset(2024, 11,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2b65106e-ba70-4c0b-a614-31c18f8526ac"), new DateTimeOffset(2024, 11,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("389b3680-362c-412a-9db2-e8f542d4aaf6"), new DateTimeOffset(2024, 11, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("c22f92a4-7592-4716-8e83-736ef0294208"), new DateTimeOffset(2024, 11, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("fe4e904c-efc2-4ddd-8e5f-b4a9359d118c"), new DateTimeOffset(2024, 11,  8, 9, 0, 0, TimeSpan.Zero) },
            // Dec 2024
            { new Guid("7d883e4f-bff7-4f88-976e-dc5c84be6e62"), new DateTimeOffset(2024, 12,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("f1a33ca4-cd85-496f-8249-209d21c4482b"), new DateTimeOffset(2024, 12,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("86202d22-7c6b-4e83-b3a9-af408361a640"), new DateTimeOffset(2024, 12,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("339f3327-a61a-4ee7-8a60-bb98f701f5a4"), new DateTimeOffset(2024, 12, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("1bad49f0-f50f-4d70-bed2-ac4722109510"), new DateTimeOffset(2024, 12, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2e6849c9-a93a-4e4a-bcea-0abf1b759c20"), new DateTimeOffset(2024, 12, 22, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("c172c101-9b79-400e-b0a3-4852bf693109"), new DateTimeOffset(2024, 12, 20, 9, 0, 0, TimeSpan.Zero) },
            // Jan 2025
            { new Guid("33abb00c-384c-455e-8869-b9a35897035b"), new DateTimeOffset(2025,  1,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("4aeb638d-2fa3-4500-a20f-948e3ec33a9c"), new DateTimeOffset(2025,  1,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("5a516b5f-550a-4110-8ac4-34ae649a78a5"), new DateTimeOffset(2025,  1,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("c3c4f996-d74b-4a4e-b477-c966fc6b2b88"), new DateTimeOffset(2025,  1, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("ce857c9b-b1ea-4eb4-a982-61a7c287b253"), new DateTimeOffset(2025,  1, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("1cd874b0-721f-4bed-80b8-5b2ba49b3bd7"), new DateTimeOffset(2025,  1, 14, 9, 0, 0, TimeSpan.Zero) },
            // Feb 2025
            { new Guid("4f75891c-aafa-4902-b95f-c427eecf208d"), new DateTimeOffset(2025,  2,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2311d811-f6f6-4823-9605-05fd33802e0f"), new DateTimeOffset(2025,  2,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("bc8538a2-6334-4357-a81f-917de35892f8"), new DateTimeOffset(2025,  2,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("ea310b25-abd7-44da-a804-5bedc946bf6c"), new DateTimeOffset(2025,  2, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("a8ef8cd2-dd79-488f-bf16-5dfdbda363f1"), new DateTimeOffset(2025,  2, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("67e37b1c-1a5b-466c-bb3c-d5d5f827eb49"), new DateTimeOffset(2025,  2, 17, 9, 0, 0, TimeSpan.Zero) },
            // Mar 2025
            { new Guid("9ac9715e-9765-45aa-a442-8eac17a7b7a3"), new DateTimeOffset(2025,  3,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("90ed5ec5-5d31-4ac6-9a24-9600ab967a6d"), new DateTimeOffset(2025,  3,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("7bebf7c2-23cb-42cd-a716-e675c31785ff"), new DateTimeOffset(2025,  3,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("93fd8153-87bd-458f-85b3-47c9a7edf82b"), new DateTimeOffset(2025,  3, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("01096079-50e5-45bc-a617-4525b5a0face"), new DateTimeOffset(2025,  3, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("4dd50f3d-dc5d-454d-a5d8-848aa2c0b3fb"), new DateTimeOffset(2025,  3,  9, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("c44bcfbd-025d-4393-83da-a31f9df1528f"), new DateTimeOffset(2025,  3, 20, 9, 0, 0, TimeSpan.Zero) },
            // Apr 2025
            { new Guid("02aac746-f15e-4a6a-bee0-ede21bc1d605"), new DateTimeOffset(2025,  4,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("9f6897b2-e50a-42e7-b56a-326a056a1ad6"), new DateTimeOffset(2025,  4,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("37939484-25e7-42e9-8a46-f46bdc10ddd6"), new DateTimeOffset(2025,  4,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("51e87b5c-051c-42ca-bee4-6871a7e63894"), new DateTimeOffset(2025,  4, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("741d2ca7-e95c-4fd5-b9cd-27b931d28537"), new DateTimeOffset(2025,  4, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2154267a-6b28-467e-b0b8-ad4ac1433b55"), new DateTimeOffset(2025,  4, 21, 9, 0, 0, TimeSpan.Zero) },
            // May 2025
            { new Guid("7a8fd2e6-02ea-4473-b7a6-a56ffd39d5e1"), new DateTimeOffset(2025,  5,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("0f1e56c0-75b3-43e0-8076-77a5c87c41a7"), new DateTimeOffset(2025,  5,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2b662e5f-19c9-4572-a121-539d14ab6b84"), new DateTimeOffset(2025,  5,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("65ec8b0b-05e6-408d-a371-4ea1b1f7a78c"), new DateTimeOffset(2025,  5, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("7e6b5896-d691-4259-9dd9-d5731903f949"), new DateTimeOffset(2025,  5, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2b20a843-36b4-4bb1-ae3f-5c696c4fbc1b"), new DateTimeOffset(2025,  5, 18, 9, 0, 0, TimeSpan.Zero) },
            // Jun 2025
            { new Guid("30b8cf74-8dfc-4f40-a48d-6c13c881ee3e"), new DateTimeOffset(2025,  6,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("dbdc653d-945a-4017-a0c7-71bbe5e43abb"), new DateTimeOffset(2025,  6,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("86491244-9de5-4c15-877a-a53066a6e986"), new DateTimeOffset(2025,  6,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("7997cf59-0fea-40ae-8345-a8c69b19b2f8"), new DateTimeOffset(2025,  6, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("cd9c2b4b-abae-4ea6-a5ca-52271c6ad26a"), new DateTimeOffset(2025,  6, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e70c2e9b-ae32-4474-b14e-dc155b1af19c"), new DateTimeOffset(2025,  6, 16, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2fed3401-76c1-42d4-a839-3e4d67d5fa60"), new DateTimeOffset(2025,  6, 20, 9, 0, 0, TimeSpan.Zero) },
            // Jul 2025
            { new Guid("d9c55295-a14e-4b51-9e42-8d9e3f518472"), new DateTimeOffset(2025,  7,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("1130ff65-d565-45ef-9a0b-7fd1eb00387b"), new DateTimeOffset(2025,  7,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e787a536-e4a9-4e3c-8d13-80b44696d469"), new DateTimeOffset(2025,  7,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("52082c6e-5633-4ed9-ae2c-5a34f295bc25"), new DateTimeOffset(2025,  7, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("285438bb-5d5e-4bbc-a5cf-5f4f5243c963"), new DateTimeOffset(2025,  7, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e34ee279-30e1-48f8-83f7-30c724d0b1c1"), new DateTimeOffset(2025,  7, 23, 9, 0, 0, TimeSpan.Zero) },
            // Aug 2025
            { new Guid("e0453656-4aea-4dad-a69b-977de9c24a6a"), new DateTimeOffset(2025,  8,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("de194324-c79b-438c-85af-6d298635aff7"), new DateTimeOffset(2025,  8,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("80ba92a6-248b-4261-ab3b-2c2da8915b9b"), new DateTimeOffset(2025,  8,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("eadc2140-467f-49a1-9175-786c6f52a2cd"), new DateTimeOffset(2025,  8, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("ff2235d2-f577-415c-9326-76de619d1c29"), new DateTimeOffset(2025,  8, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("659b3f74-b28d-4007-ab5c-5d5d301dd881"), new DateTimeOffset(2025,  8, 11, 9, 0, 0, TimeSpan.Zero) },
            // Sep 2025
            { new Guid("86311f7a-4559-40cc-bb45-11e1c594e791"), new DateTimeOffset(2025,  9,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e5148ece-f66f-4dc3-8bb7-b6e273666a10"), new DateTimeOffset(2025,  9,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("bd5efa24-d3c0-489c-8495-6b224ed310e7"), new DateTimeOffset(2025,  9,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("3f41be48-bb99-451b-a05d-b8918620c4a5"), new DateTimeOffset(2025,  9, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("f3bf9c91-2e17-4dca-a775-c0be39a05796"), new DateTimeOffset(2025,  9, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("6388f494-8c65-42d9-8119-8dbd54b3900d"), new DateTimeOffset(2025,  9, 19, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("dc101156-1794-4e95-bbdb-98c2c528d416"), new DateTimeOffset(2025,  9, 20, 9, 0, 0, TimeSpan.Zero) },
            // Oct 2025
            { new Guid("73a8f8b6-a753-4a8a-b6bb-b5197f9c173f"), new DateTimeOffset(2025, 10,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("43d75621-fdd1-48af-a774-3ab41b919b5d"), new DateTimeOffset(2025, 10,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("1ef61fd2-ab9b-4d33-8471-6eca8b1527fb"), new DateTimeOffset(2025, 10,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("dc324088-5b57-479f-8ee9-886e068c1ea2"), new DateTimeOffset(2025, 10, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("0115ee02-8da3-43c4-aef3-d3f629477514"), new DateTimeOffset(2025, 10, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("8104df15-2012-4b75-9f79-e579d65cf355"), new DateTimeOffset(2025, 10, 14, 9, 0, 0, TimeSpan.Zero) },
            // Nov 2025
            { new Guid("9e3d9af8-44ee-4745-a42b-a6a4616e9f45"), new DateTimeOffset(2025, 11,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("41473e5b-1c95-4ca9-8791-0fb20f6970fb"), new DateTimeOffset(2025, 11,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e87db019-f7d8-466c-bcc2-872a9062fdec"), new DateTimeOffset(2025, 11,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("55d842a7-5801-453b-8963-e1f043531dd6"), new DateTimeOffset(2025, 11, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("c42c9107-0c6d-4603-aea0-4191f18233a0"), new DateTimeOffset(2025, 11, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("86d39d96-65f4-4e3b-932a-d637369f34f6"), new DateTimeOffset(2025, 11,  7, 9, 0, 0, TimeSpan.Zero) },
            // Dec 2025
            { new Guid("5bf61928-b7b4-4c67-a869-b0c19bae1363"), new DateTimeOffset(2025, 12,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("3182befb-c05c-462b-9b77-8aa829690698"), new DateTimeOffset(2025, 12,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("fe9c5539-ce8d-40c4-a17d-cfedd55a67c2"), new DateTimeOffset(2025, 12,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("921ccd7f-97ac-4350-a0be-5bf8877182d6"), new DateTimeOffset(2025, 12, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("0416136c-eb08-42f7-a5b2-953ddcd92217"), new DateTimeOffset(2025, 12, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("0a80f11f-acb1-4021-9a49-f2bdcd755b27"), new DateTimeOffset(2025, 12, 20, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e59f0ddb-3a9a-404a-9c74-505ae3be5f8e"), new DateTimeOffset(2025, 12, 20, 9, 0, 0, TimeSpan.Zero) },
            // Jan 2026
            { new Guid("df7a050e-b3f6-44e5-a68c-4eadb506cd6a"), new DateTimeOffset(2026,  1,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2e41dc1a-cf42-4d2c-8ffe-5680ab243a99"), new DateTimeOffset(2026,  1,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("52cce902-08af-451b-86e9-f5be9ae1e045"), new DateTimeOffset(2026,  1,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("9b385307-44fd-49cc-a44d-358eb9cb8428"), new DateTimeOffset(2026,  1, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("d9bd9d47-2352-4506-82c9-b3be56f11e5c"), new DateTimeOffset(2026,  1, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("adbfd05b-5e07-41b7-9d08-cb9369d0f13d"), new DateTimeOffset(2026,  1, 13, 9, 0, 0, TimeSpan.Zero) },
            // Feb 2026
            { new Guid("c31f13a7-42e0-48a6-b7c2-5a75f1e60a5d"), new DateTimeOffset(2026,  2,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("bc73c35c-f0b8-424e-8c8e-48d3ebe5a7a9"), new DateTimeOffset(2026,  2,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("5a93cd46-6ef7-48a5-991a-79d19168fe64"), new DateTimeOffset(2026,  2,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("aeb87716-7bb3-408c-9584-7ddbc539cbbc"), new DateTimeOffset(2026,  2, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("aa5b45e5-3869-4bda-91ad-9093b25509a6"), new DateTimeOffset(2026,  2, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("eb44d712-8be8-48f8-91e6-5b28e2a4efaf"), new DateTimeOffset(2026,  2, 19, 9, 0, 0, TimeSpan.Zero) },
            // Mar 2026
            { new Guid("18297933-272d-4a57-a419-82e4e71ca50d"), new DateTimeOffset(2026,  3,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("d750c2c3-1c05-4289-808c-a20a22df67c8"), new DateTimeOffset(2026,  3,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("7642e9b0-3796-48b3-bf53-41d8e2b45c0b"), new DateTimeOffset(2026,  3,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("eed4d0ab-f042-4128-96de-38702aae2e1f"), new DateTimeOffset(2026,  3, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("6c1a5f4c-83ac-4c4c-80a7-96a32adf44e3"), new DateTimeOffset(2026,  3, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("087aeeca-683a-4bd5-ad1e-313695c7bcb4"), new DateTimeOffset(2026,  3,  8, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("3e3ca661-8adf-4f04-a017-37fdf76a9cd3"), new DateTimeOffset(2026,  3, 20, 9, 0, 0, TimeSpan.Zero) },
            // Apr 2026
            { new Guid("64fab85c-bab0-4f3e-b339-34d90ad453ec"), new DateTimeOffset(2026,  4,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("6f081576-4cd1-4cd2-97fb-0c9733be4e97"), new DateTimeOffset(2026,  4,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("9d62ff8d-b136-420d-986f-f31cdea87bf7"), new DateTimeOffset(2026,  4,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("af6933ba-b1a9-4a27-a330-24a0550c3e8f"), new DateTimeOffset(2026,  4, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("83c6f664-0be5-46bd-80cd-4b73fa04d664"), new DateTimeOffset(2026,  4, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("01b53a63-5b58-4d26-96e5-0cb8d6608972"), new DateTimeOffset(2026,  4, 22, 9, 0, 0, TimeSpan.Zero) },
            // May 2026
            { new Guid("e94b0139-e00d-4094-b154-dca536115808"), new DateTimeOffset(2026,  5,  1, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("478bcb28-72dd-47cf-82b6-141056619f63"), new DateTimeOffset(2026,  5,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("96c8481a-1f93-488b-bd2a-40d068f5b081"), new DateTimeOffset(2026,  5,  5, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("e25ccfc8-8d4c-4981-87e9-0f83434b4632"), new DateTimeOffset(2026,  5, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("84a9d871-bede-48b5-943f-bee3c3dd838f"), new DateTimeOffset(2026,  5, 10, 9, 0, 0, TimeSpan.Zero) },
            { new Guid("2b728273-05f6-46fc-b110-8d629991c7e3"), new DateTimeOffset(2026,  5, 10, 9, 0, 0, TimeSpan.Zero) },
        };

        foreach (var (id, date) in dates)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE UserTransactions SET Created = {0}, LastModified = {0} WHERE TransactionId = {1}", date, id);
    }
}
