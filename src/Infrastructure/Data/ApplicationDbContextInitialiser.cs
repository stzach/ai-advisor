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
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000001"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Amazon Fresh",     TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 85.50m  },
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000002"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Netflix",           TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 12.99m  },
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000003"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Shell Gas Station", TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 65.00m  },
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000004"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "IKEA",              TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 245.00m },
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000005"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "DEI Electric Bill", TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 94.80m  },
                new UserTransaction { TransactionId = new Guid("10000001-0000-0000-0000-000000000006"), UserId = komliki.Id, ProductId = MastercardId, From = "1234 8255 7654 5733", To = "Apple Store",       TransactionType = TransactionType.Payment, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 999.00m }
            );
        }

        // tzachristas — Current Account x2, Saving Account, Mortgage Loan, Cyber Insurance, Mastercard
        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null && !_context.UserTransactions.Any(ut => ut.UserId == tzachristas.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Account — salary in, outgoing transfers
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000001"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "Employer SA",                        To = "GR13 5678 2392 1690 9372 1847 123", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  5000.00m },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000002"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 2000.00m },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000003"), UserId = tzachristas.Id, ProductId = CurrentAccId,   From = "GR13 5678 3421 9275 1235 1095 912", To = "Utility Co.",                       TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 145.00m  },
                // Saving Account — transfer in from current
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000004"), UserId = tzachristas.Id, ProductId = SavingAccId,    From = "GR13 5678 2392 1690 9372 1847 123", To = "GR13 1234 2222 6773 9421 5342 280", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  2000.00m },
                // Mortgage Loan — three monthly installments
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000005"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000006"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000007"), UserId = tzachristas.Id, ProductId = MortgageLoanId, From = "GR13 5678 2392 1690 9372 1847 123", To = "Alpha Bank Mortgage",               TransactionType = TransactionType.Loan,    TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 850.00m  },
                // Cyber Insurance — annual premium
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000008"), UserId = tzachristas.Id, ProductId = CyberInsId,     From = "GR13 5678 2392 1690 9372 1847 123", To = "Cyber Shield Insurance",            TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 120.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000009"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 210.00m  },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000010"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Uber",                              TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 25.50m   },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000011"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Steam Games",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 59.99m   },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000012"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "COSMOTE Internet",                  TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 34.90m   },
                new UserTransaction { TransactionId = new Guid("20000002-0000-0000-0000-000000000013"), UserId = tzachristas.Id, ProductId = MastercardId,   From = "Mastercard *4444",                  To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 189.00m  }
            );
        }

        // geronymakis — Current Account, Saving Account x2, Mastercard, Visa Debit
        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null && !_context.UserTransactions.Any(ut => ut.UserId == geronymakis.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Account — salary in, rent and savings transfers out
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000001"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "Employer Ltd",                       To = "GR13 6723 9388 6371 7319 1422 846", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  1800.00m },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000002"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "GR13 6723 9388 6371 7319 1422 846", To = "Landlord GR",                       TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 650.00m  },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000003"), UserId = geronymakis.Id, ProductId = CurrentAccId, From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 500.00m  },
                // Saving Accounts — transfers in
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000004"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 5678 1111 5784 1235 1095 732", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  500.00m  },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000005"), UserId = geronymakis.Id, ProductId = SavingAccId,  From = "GR13 6723 9388 6371 7319 1422 846", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  200.00m  },
                // Mastercard — spending across all categories
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000006"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "Sklavenitis",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 156.78m  },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000007"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "Spotify",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 9.99m    },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000008"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "OTE Internet",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 34.90m   },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000009"), UserId = geronymakis.Id, ProductId = MastercardId, From = "Mastercard *5555",                  To = "IKEA Athens",                       TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 312.00m  },
                // Visa Debit — day-to-day spending
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000010"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "ISAP Metro Ticket",                 TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 2.50m    },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000011"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "Local Bakery",                      TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 8.50m    },
                new UserTransaction { TransactionId = new Guid("30000003-0000-0000-0000-000000000012"), UserId = geronymakis.Id, ProductId = VisaDebitId,  From = "Visa *2333",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 22.30m   }
            );
        }

        // kotrotsos — Current Account x2, Saving Account, Visa Debit
        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null && !_context.UserTransactions.Any(ut => ut.UserId == kotrotsos.Id))
        {
            _context.UserTransactions.AddRange(
                // Current Accounts — salary in, rent and savings transfers out
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000001"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "Company GR",                         To = "GR13 4555 1111 6789 1234 7890 543", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  3500.00m },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000002"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 4555 1111 6789 1234 7890 543", To = "Landlord Properties",               TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 800.00m  },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000003"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 6732 2323 1480 1780 9263 092", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 1000.00m },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000004"), UserId = kotrotsos.Id, ProductId = CurrentAccId, From = "GR13 4555 1111 6789 1234 7890 543", To = "DEI Power",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Utilities,     TransactionDirection = TransactionDirection.Outgoing, Amount = 78.20m   },
                // Saving Account — transfer in
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000005"), UserId = kotrotsos.Id, ProductId = SavingAccId,  From = "GR13 6732 2323 1480 1780 9263 092", To = "GR13 7842 1234 9876 2637 1835 892", TransactionType = TransactionType.Transfer, TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Incoming, Amount =  1000.00m },
                // Visa Debit — spending across all categories
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000006"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Cinema Athens",                     TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Entertainment, TransactionDirection = TransactionDirection.Outgoing, Amount = 15.00m   },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000007"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Starbucks",                         TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Food,          TransactionDirection = TransactionDirection.Outgoing, Amount = 6.50m    },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000008"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "EasyBus",                           TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Transport,     TransactionDirection = TransactionDirection.Outgoing, Amount = 3.50m    },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000009"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Hardware Store",                    TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Housing,       TransactionDirection = TransactionDirection.Outgoing, Amount = 54.99m   },
                new UserTransaction { TransactionId = new Guid("40000004-0000-0000-0000-000000000010"), UserId = kotrotsos.Id, ProductId = VisaDebitId,  From = "Visa *4523",                        To = "Pharmacy",                          TransactionType = TransactionType.Payment,  TransactionCategory = TransactionCategory.Other,         TransactionDirection = TransactionDirection.Outgoing, Amount = 18.60m   }
            );
        }

        await _context.SaveChangesAsync();
    }
}
