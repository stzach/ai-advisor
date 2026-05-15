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
    }

    private async Task TrySeedProductsAsync()
    {
        if (_context.Products.Any())
            return;

        var products = new List<Product>
        {
            new() { ProductId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), ProductName = "Mastercard Credit Card",  ProductDescription = "Credit Card",     ProductPrice = 0, ProductType = ProductType.Card      },
            new() { ProductId = new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), ProductName = "Visa Debit Card",         ProductDescription = "Debit Card",      ProductPrice = 0, ProductType = ProductType.Card      },
            new() { ProductId = new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), ProductName = "Current Account",         ProductDescription = "Current Account", ProductPrice = 0, ProductType = ProductType.Account   },
            new() { ProductId = new Guid("d4e5f6a7-b8c9-0123-def0-234567890123"), ProductName = "Saving Account",          ProductDescription = "Saving Account",  ProductPrice = 0, ProductType = ProductType.Account   },
            new() { ProductId = new Guid("e5f6a7b8-c9d0-1234-ef01-345678901234"), ProductName = "Cyber Insurance",         ProductDescription = "Cyber Insurance", ProductPrice = 0, ProductType = ProductType.Insurance },
            new() { ProductId = new Guid("f6a7b8c9-d0e1-2345-f012-456789012345"), ProductName = "Mortgage Loan",           ProductDescription = "Mortgage Loan",   ProductPrice = 0, ProductType = ProductType.Loan      },
            new() { ProductId = new Guid("a7b8c9d0-e1f2-3456-0123-567890123456"), ProductName = "Personal Loan",           ProductDescription = "Personal Loan",   ProductPrice = 0, ProductType = ProductType.Loan      },
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    private async Task TrySeedUserProductsAsync()
    {
        var mastercardId   = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var visaDebitId    = new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901");
        var currentAccId   = new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012");
        var savingAccId    = new Guid("d4e5f6a7-b8c9-0123-def0-234567890123");
        var cyberInsId     = new Guid("e5f6a7b8-c9d0-1234-ef01-345678901234");
        var mortgageLoanId = new Guid("f6a7b8c9-d0e1-2345-f012-456789012345");

        var komliki = await _userManager.FindByNameAsync("komliki");
        if (komliki is not null && !_context.UserProducts.Any(up => up.UserId == komliki.Id))
        {
            _context.UserProducts.Add(new UserProduct
            {
                UserId           = komliki.Id,
                ProductId        = mastercardId,
                AvailableBalance = 1500,
                CardNumber       = "1234 8255 7654 5733",
                IsActive         = true
            });
        }

        var tzachristas = await _userManager.FindByNameAsync("tzachristas");
        if (tzachristas is not null && !_context.UserProducts.Any(up => up.UserId == tzachristas.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = tzachristas.Id, ProductId = currentAccId,   AvailableBalance = 27945,    AccountNumber = "GR13 5678 2392 1690 9372 1847 123", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = currentAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 3421 9275 1235 1095 912", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = savingAccId,    AvailableBalance = 69785,    AccountNumber = "GR13 1234 2222 6773 9421 5342 280", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = mortgageLoanId, AvailableBalance = 200000,   AccountNumber = "GR45 6543 3333 7832 4723 1239 931", IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = cyberInsId,     AvailableBalance = 0,                                                             IsActive = true },
                new UserProduct { UserId = tzachristas.Id, ProductId = mastercardId,   AvailableBalance = 2820,     CardNumber    = "5555 5555 5555 4444",                IsActive = true }
            );
        }

        var geronymakis = await _userManager.FindByNameAsync("geronymakis");
        if (geronymakis is not null && !_context.UserProducts.Any(up => up.UserId == geronymakis.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = geronymakis.Id, ProductId = currentAccId,  AvailableBalance = 245,      AccountNumber = "GR13 6723 9388 6371 7319 1422 846", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = savingAccId,   AvailableBalance = 347.93m,  AccountNumber = "GR13 5678 1111 5784 1235 1095 732", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = savingAccId,   AvailableBalance = 2959.51m, AccountNumber = "GR13 7842 1234 9876 2637 1835 892", IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = mastercardId,  AvailableBalance = 567,      CardNumber    = "1254 2333 3444 5555",                IsActive = true },
                new UserProduct { UserId = geronymakis.Id, ProductId = visaDebitId,   AvailableBalance = 0,        CardNumber    = "6732 1212 6782 2333",                IsActive = true }
            );
        }

        var kotrotsos = await _userManager.FindByNameAsync("kotrotsos");
        if (kotrotsos is not null && !_context.UserProducts.Any(up => up.UserId == kotrotsos.Id))
        {
            _context.UserProducts.AddRange(
                new UserProduct { UserId = kotrotsos.Id, ProductId = currentAccId, AvailableBalance = 5672.89m, AccountNumber = "GR13 4555 1111 6789 1234 7890 543", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = currentAccId, AvailableBalance = 1273.95m, AccountNumber = "GR13 6732 2323 1480 1780 9263 092", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = savingAccId,  AvailableBalance = 2959.51m, AccountNumber = "GR13 7842 1234 9876 2637 1835 892", IsActive = true },
                new UserProduct { UserId = kotrotsos.Id, ProductId = visaDebitId,  AvailableBalance = 0,        CardNumber    = "5673 4567 1241 4523",                IsActive = true }
            );
        }

        await _context.SaveChangesAsync();
    }
}
