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

        var tzachristas = new ApplicationUser { UserName = "tzachristas", Email = "tzachristas@gmail.com" };
        if (_userManager.Users.All(u => u.UserName != tzachristas.UserName))
        {
            await _userManager.CreateAsync(tzachristas, "Asdf135!");
        }

        var kafousis = new ApplicationUser { UserName = "kafousis", Email = "kafousis@gmail.com" };
        if (_userManager.Users.All(u => u.UserName != kafousis.UserName))
        {
            await _userManager.CreateAsync(kafousis, "Asdf135!");
        }

        var geronymakis = new ApplicationUser { UserName = "geronymakis", Email = "geronymakis@gmail.com" };
        if (_userManager.Users.All(u => u.UserName != geronymakis.UserName))
        {
            await _userManager.CreateAsync(geronymakis, "Asdf135!");
        }

        var kotrotsos = new ApplicationUser { UserName = "kotrotsos", Email = "kotrotsos@gmail.com" };
        if (_userManager.Users.All(u => u.UserName != kotrotsos.UserName))
        {
            await _userManager.CreateAsync(kotrotsos, "Asdf135!");
        }

        var komliki = new ApplicationUser { UserName = "komliki", Email = "christinakomliki@gmail.com" };
        if (_userManager.Users.All(u => u.UserName != komliki.UserName))
        {
            await _userManager.CreateAsync(komliki, "Asdf135!");
        }



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
        if (_context.Products.Any())
            return;

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

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    private async Task TrySeedUserProductsAsync()
    {
        var komliki = await _userManager.FindByNameAsync("komliki");
        if (komliki is not null && !_context.UserProducts.Any(up => up.UserId == komliki.Id))
        {
            _context.UserProducts.Add(new UserProduct
            {
                UserId           = komliki.Id,
                ProductId        = MastercardId,
                AvailableBalance = 1500,
                CardNumber       = "1234 8255 7654 5733",
                IsActive         = true
            });
        }

        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null && !_context.UserProducts.Any(up => up.UserId == tzachristas.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = tzachristas.Id, ProductId = CurrentAccId,   AvailableBalance = 27945,    AccountNumber = "GR13 5678 2392 1690 9372 1847 123", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = CurrentAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 3421 9275 1235 1095 912", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = SavingAccId,    AvailableBalance = 69785,    AccountNumber = "GR13 1234 2222 6773 9421 5342 280", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = MortgageLoanId, AvailableBalance = 200000,   AccountNumber = "GR45 6543 3333 7832 4723 1239 931", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = CyberInsId,     AvailableBalance = 0,                                                             IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = MastercardId,   AvailableBalance = 2820,     CardNumber    = "5555 5555 5555 4444",                IsActive = true }
            );
        }

        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null && !_context.UserProducts.Any(up => up.UserId == geronymakis.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = geronymakis.Id, ProductId = CurrentAccId,  AvailableBalance = 245,      AccountNumber = "GR13 6723 9388 6371 7319 1422 846", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = SavingAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 1111 5784 1235 1095 732", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = SavingAccId,   AvailableBalance = 2959.51m, AccountNumber = "GR13 7842 1234 9876 2637 1835 892", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = MastercardId,  AvailableBalance = 567,      CardNumber    = "1254 2333 3444 5555",                IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = VisaDebitId,   AvailableBalance = 0,        CardNumber    = "6732 1212 6782 2333",                IsActive = true }
            );
        }

        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null && !_context.UserProducts.Any(up => up.UserId == kotrotsos.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = kotrotsos.Id, ProductId = CurrentAccId, AvailableBalance = 5672.89m, AccountNumber = "GR13 4555 1111 6789 1234 7890 543", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = CurrentAccId, AvailableBalance = 1273.95m, AccountNumber = "GR13 6732 2323 1480 1780 9263 092", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = SavingAccId,  AvailableBalance = 2959.51m, AccountNumber = "GR13 7842 1234 9876 2637 1835 892", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = VisaDebitId,  AvailableBalance = 0,        CardNumber    = "5673 4567 1241 4523",                IsActive = true }
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task TrySeedUserTransactionsAsync()
    {
        // komliki — Mastercard Credit Card: Payment transactions across all categories
        var komliki = await _userManager.FindByNameAsync("komliki");
        if (komliki is not null && !_context.UserTransactions.Any(ut => ut.UserId == komliki.Id))
        {
            _context.UserTransactions.AddRange(
                new UserTransaction { TransactionId = new Guid("7b7a3f0e-4dcb-4d4a-b6a9-5f2a4f8d9c01"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Amazon Fresh",     TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 85.50m  },
                new UserTransaction { TransactionId = new Guid("1c9d5a77-8e23-4d72-a2ef-3b7d0c5e91fa"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Netflix",           TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 12.99m  },
                new UserTransaction { TransactionId = new Guid("a4f2d1c0-3e8b-45d6-9f71-2d5b7e4a8c13"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Shell Gas Station", TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 65.00m  },
                new UserTransaction { TransactionId = new Guid("6e2b1d9f-7c44-4af0-8a63-1d7e5b2c9f80"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "IKEA",              TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 245.00m },
                new UserTransaction { TransactionId = new Guid("d0f9a2c7-1b55-4e6d-b3f8-7a1c2d4e9b65"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "DEI Electric Bill", TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 94.80m  },
                new UserTransaction { TransactionId = new Guid("2a8e4c1d-9f30-4b72-8d6e-5c1a7f3b0d92"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Apple Store",       TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 999.00m }
            );
        }

        // tzachristas — Current Account x2, Saving Account, Mortgage Loan, Cyber Insurance, Mastercard
        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null && !_context.UserTransactions.Any(ut => ut.UserId == tzachristas.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Account — salary in, outgoing transfers
                new UserTransaction { TransactionId = new Guid("9d4b7e2a-5c18-4f63-a1d0-8e2b6c7f4a31"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "Employer SA",                        To = "GR13 5678 2392 1690 9372 1847 123", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  5000.00m },
                new UserTransaction { TransactionId = new Guid("3f1c8d5b-6a77-4e20-b9d4-2c5e7a1f8b63"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 2000.00m },
                new UserTransaction { TransactionId = new Guid("b5e2a7c1-0d94-4f88-9a31-6c2d7e5b1f40"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "GR13 5678 3421 9275 1235 1095 912", To = "Utility Co.",                       TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 145.00m  },
                // Saving Account — transfer in from current
                new UserTransaction { TransactionId = new Guid("4c7d1a9e-2b63-4e5f-a8d1-9b0c2f7e6a54"), UserId = tzachristas.Id, ProductId = SavingAccId,    From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  2000.00m },
                // Mortgage Loan — three monthly installments
                new UserTransaction { TransactionId = new Guid("8f3a5d2c-1e70-4b91-b6c4-3d7a9e2f5c18"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                new UserTransaction { TransactionId = new Guid("5b1e9c4d-7a22-4f63-8d0b-1c5e7a9f2d64"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                new UserTransaction { TransactionId = new Guid("c2d7a1f9-4b58-4e30-a6c5-8f1d2b7e9a43"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                // Cyber Insurance — annual premium
                new UserTransaction { TransactionId = new Guid("0e5c8a2d-9f41-4b76-b3d7-6a1c5e2f8d90"), UserId = tzachristas.Id, ProductId = CyberInsId,     From = "GR13 5678 2392 1690 9372 1847 123", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 120.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("f7a1d3c5-2b84-4e69-8c0f-5d2a7b1e9c36"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 210.00m  },
                new UserTransaction { TransactionId = new Guid("1b4e7d9a-5c20-4f81-a3d6-7e2c1f8b5a94"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Uber",                              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 25.50m   },
                new UserTransaction { TransactionId = new Guid("e3c9a5d1-6b47-4e22-b8f0-2d7a1c5e9f63"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Steam Games",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 59.99m   },
                new UserTransaction { TransactionId = new Guid("7d2a8f1c-3e95-4b64-a0d7-9c5e2b1f6a48"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "COSMOTE Internet",                  TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 34.90m   },
                new UserTransaction { TransactionId = new Guid("2f6c1a8d-4b73-4e59-b1d2-5a7c9f3e0d84"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 189.00m  }
            );
        }

        // geronymakis — Current Account, Saving Account x2, Mastercard, Visa Debit
        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null && !_context.UserTransactions.Any(ut => ut.UserId == geronymakis.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Account — salary in, rent and savings transfers out
                new UserTransaction { TransactionId = new Guid("9a5e2c7d-1b68-4f40-8d3a-6c1f7e2b5d91"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "Employer Ltd",                       To = "GR13 6723 9388 6371 7319 1422 846", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  1800.00m },
                new UserTransaction { TransactionId = new Guid("d4c1f8a2-7e39-4b75-a6d0-3f5c1b9e2a64"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "GR13 6723 9388 6371 7319 1422 846", To = "Landlord GR",                       TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 650.00m  },
                new UserTransaction { TransactionId = new Guid("6a9d3e1c-5b80-4f22-b7c1-8d2a5e7f4c93"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 500.00m  },
                // Saving Accounts — transfers in
                new UserTransaction { TransactionId = new Guid("3c7f1b5a-9d24-4e68-a2f0-1e5c8b7d6a41"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  500.00m  },
                new UserTransaction { TransactionId = new Guid("b8a2d6c1-4f57-4e90-b3d5-7c1a2f8e9d60"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  200.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("5e1b9a4d-2c83-4f71-a6d8-0b7c5e1f2a94"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 156.78m  },
                new UserTransaction { TransactionId = new Guid("a1d7c5e9-3b46-4e28-b0f2-9c5a7d1e6f83"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "Spotify",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 9.99m    },
                new UserTransaction { TransactionId = new Guid("0c8e2a5d-7f31-4b64-a9d1-2e5c7b3f8a40"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "OTE Internet",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 34.90m   },
                new UserTransaction { TransactionId = new Guid("f2a5d1c7-8b94-4e53-b6d0-1c7a9e2f5b68"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "IKEA Athens",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 312.00m  },
                // Visa Debit — day-to-day spending
                new UserTransaction { TransactionId = new Guid("4d9a1e6c-5b27-4f80-a3d2-7c1e5f8b9a34"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "ISAP Metro Ticket",                 TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 2.50m    },
                new UserTransaction { TransactionId = new Guid("8c5f2a1d-6e73-4b49-b0d7-3a9c1e5f2d64"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "Local Bakery",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 8.50m    },
                new UserTransaction { TransactionId = new Guid("1e7c4a9d-2b58-4f31-a6d0-5c8e2f7b1a93"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 22.30m   }
            );
        }

        // kotrotsos — Current Account x2, Saving Account, Visa Debit
        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null && !_context.UserTransactions.Any(ut => ut.UserId == kotrotsos.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Accounts — salary in, rent and savings transfers out
                new UserTransaction { TransactionId = new Guid("c5a1d8e2-9b44-4e67-b3f1-7d2c5a9e0f86"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "Company GR",                         To = "GR13 4555 1111 6789 1234 7890 543", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  3500.00m },
                new UserTransaction { TransactionId = new Guid("7f2d6a1c-3e80-4b52-a9d4-1c5e7b2f8a63"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 4555 1111 6789 1234 7890 543", To = "Landlord Properties",               TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 800.00m  },
                new UserTransaction { TransactionId = new Guid("2b9e5c1d-4f36-4e78-b0d2-6a1f9c5e7d84"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 6732 2323 1480 1780 9263 092", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 1000.00m },
                new UserTransaction { TransactionId = new Guid("e8d1a4c7-5b92-4f20-a6d3-2c7e1f5b9a48"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 4555 1111 6789 1234 7890 543", To = "DEI Power",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 78.20m   },
                // Saving Account — transfer in
                new UserTransaction { TransactionId = new Guid("6c1f7a2d-8e54-4b69-b3d0-5a2c9e1f7d83"), UserId = kotrotsos.Id, ProductId = SavingAccId,  From = "GR13 6732 2323 1480 1780 9263 092", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  1000.00m },
                // Visa Debit — spending across all categories
                new UserTransaction { TransactionId = new Guid("9e4a2d7c-1b63-4f85-a0d1-7c5e2b9f4a36"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Cinema Athens",                     TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 15.00m   },
                new UserTransaction { TransactionId = new Guid("3a8d1f5c-6e27-4b40-b9d2-1f7c5a8e2d64"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Starbucks",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 6.50m    },
                new UserTransaction { TransactionId = new Guid("b1c7e4a9-5d38-4f62-a3d0-8e2b5c1f7a94"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "EasyBus",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 3.50m    },
                new UserTransaction { TransactionId = new Guid("5d2a9c1e-7b46-4e83-b0f5-2c1a7e9d6f38"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 54.99m   },
                new UserTransaction { TransactionId = new Guid("f9b1d5a2-3e74-4f68-a2d1-6c5e8b7f1a40"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 18.60m   }
            );
        }

        await _context.SaveChangesAsync();
    }
}
