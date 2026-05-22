using System.Text.Json;
using System.Text.Json.Serialization;
using AiAdvisor.Application;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

public class ProductRecomendationAgent :IProductRecomendationAgent
{
    private readonly IFinancialDataAgent _financialDataAgent;
    private readonly IChatService _chatService;
    
    private readonly IFinancialDocumentsSearchAgent _financialDocumentsSearchAgent;
    private readonly IUser _user;
    private readonly ILogger<ProductRecomendationAgent> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ProductRecomendationAgent(
        IFinancialDataAgent financialDataAgent,
        IFinancialDocumentsSearchAgent financialDocumentsSearchAgent,
        IChatService chatService,
        IUser user,
        ILogger<ProductRecomendationAgent> logger)
    {
        _financialDataAgent = financialDataAgent;
        _financialDocumentsSearchAgent = financialDocumentsSearchAgent;
        _chatService        = chatService;
        _user               = user;
        _logger             = logger;
    }

    public async Task<List<ProductRecomendationDto>> GetProductRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        _logger.LogInformation("Generating AI insights for user {UserId}", userId);

        var to  = DateTimeOffset.UtcNow;
        var from = new DateTimeOffset(to.Year, to.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, from, to, cancellationToken);

        var documentSearchResults = await _financialDocumentsSearchAgent.GetSearchResultsAsync(cancellationToken);

        var systemPrompt = """
            You are a financial product recommendation agent working for a bank.

            You must recommend the best bank product(s) for the user based strictly on the provided system context, which includes:
            - User profile data (e.g., age, income, preferences, risk level)
            - Transaction history (spending behavior, patterns, recurring payments)
            - Existing bank products owned by the user
            - Available bank product offers (each with product_id, product_name, description, eligibility criteria, benefits, fees/interest rates, and redirect_url)

            TASK
            - Analyze the user's financial situation and behavior.
            - Identify needs and match them to the most suitable available products.
            - Rank products by relevance (best match first).
            - Only recommend products explicitly provided in the input (never invent products).
            - Ensure the user meets eligibility criteria.
            - Avoid recommending products the user already owns.

            DECISION GUIDELINES
            - Prioritize maximum user benefit (cost savings, rewards, interest gains, financial protection).
            - Use transaction patterns to infer intent (e.g., travel, savings behavior, loans, investments).
            - If multiple products are similar, choose the one with better overall value.
            - If no product is suitable, return no actions.

            STRICT RULES
            - Do NOT invent or assume any products.
            - Do NOT expose internal reasoning or scoring.
            - Do NOT include sensitive user data in the output.
            - Output MUST be valid JSON only (no markdown, no explanations, no extra text).

            OUTPUT FORMAT
            Return ONLY this JSON structure:

            {
            "message": "short user-friendly explanation (max 2–3 sentences)",
            "actions": [
                {
                "ProductName": "string",
                "RedirectUri": "/products/...",
                "Reason": "short explanation why this product fits the user"
                }
            ]
            }

            MESSAGE RULES
            - Keep message short, clear, and user-friendly.
            - No financial jargon unless necessary.
            - If no suitable product is found:
            - actions must be []
            - message should briefly explain why no recommendation is available
            """;

        var userMessage = $"User financial data:\n\n{financialContext}\n\n Available products:\n\n{BankProducts}\n\n \n\n related documents:\n\n{documentSearchResults}\n\nBased on this information, recommend the best product(s) for the user with a clear explanation.";

        var response = await _chatService.SendAsync(userMessage, systemPrompt, cancellationToken);

        _logger.LogInformation("Received insights response for user {UserId} \n\n Response: \n{Response}", userId, response);

        return ParseResult(response);
    }

    private List<ProductRecomendationDto> ParseResult(string response)
    {
        try
        {
            var json = response.Trim();

            // Strip markdown code fences if the model wraps the JSON
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end   = json.LastIndexOf("```");
                if (end > start)
                    json = json[start..end].Trim();
            }

            // Try to deserialize as a wrapper object with "message" and "actions"
            var wrapper = JsonSerializer.Deserialize<ProductRecommendationResponse>(json, JsonOptions);
            if (wrapper?.Actions != null)
            {
                return wrapper.Actions;
            }

            // Fall back to direct array deserialization
            var result = JsonSerializer.Deserialize<List<ProductRecomendationDto>>(json, JsonOptions);
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse product recommendations JSON from AI response");
            return [];
        }
    }

    private class ProductRecommendationResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("actions")]
        public List<ProductRecomendationDto>? Actions { get; set; }
    }


static string BankProducts = """

Products
Mastercard Credit Card
A flexible credit card product for everyday spending, online purchases, and travel. Includes a credit limit and supports short-term borrowing with statement repayment.
Best for: customers who want flexible credit access and travel-friendly payment options.
Redirect URL: /products/mastercard-credit-card

Visa Debit Card
A debit card linked directly to the customer’s current account. Used for purchases, ATM withdrawals, and payments without carrying a credit balance.
Best for: customers who prefer pay-now convenience and want to avoid credit.
Redirect URL: /products/visa-debit-card

Saving Account
An interest-bearing account for saving money over time. Ideal for building a reserve, earning modest returns, and keeping funds available for future goals.
Best for: savers who want easy access plus interest earnings on short-to-medium savings.
Redirect URL: /products/saving-account

Cyber Insurance
Insurance coverage designed to protect against online fraud, identity theft, cyber attacks, and digital account compromise.
Best for: digitally active customers and small business owners who handle sensitive information online.
Redirect URL: /products/cyber-insurance

Mortgage Loan
A long-term loan product for home purchases or property refinancing. Includes a repayment schedule, interest charges, and secured financing against real estate.
Best for: homebuyers and property investors planning to finance residential real estate.
Redirect URL: /products/mortgage-loan

Personal Loan
An unsecured loan for personal expenses such as renovation, education, or large purchases. Repayment typically occurs over a fixed term with a set interest rate.
Best for: individuals needing fast access to cash for a well-defined personal project.
Redirect URL: /products/personal-loan

Fixed Deposit Account
A time-bound savings product offering higher interest rates in exchange for committing funds for a set term. Ideal for secure, low-risk wealth growth.
Best for: conservative investors and customers with a lump sum to lock away safely.
Redirect URL: /products/fixed-deposit-account

Premium Savings Account
A high-interest savings account for customers with larger balances. Offers priority service, enhanced digital tools, and bonus interest tiers.
Best for: affluent savers who want higher yields and premium banking service.
Redirect URL: /products/premium-savings-account

Student Account
A low-cost current account tailored for students. Includes fee waivers, budgeting tools, and simple access to student loans or scholarships.
Best for: university and college students building their first financial relationship.
Redirect URL: /products/student-account

Travel Rewards Credit Card
A credit card designed for frequent travelers. Earns points or miles on airfare, hotels, and overseas spending while offering travel protection.
Best for: frequent flyers and vacation planners who want travel perks and rewards.
Redirect URL: /products/travel-rewards-credit-card

Home Insurance
Insurance protection for a residential property. Covers damage from fire, theft, natural events, and liability for guests.
Best for: homeowners and landlords seeking protection for their property and household.
Redirect URL: /products/home-insurance

Car Loan
A vehicle financing product for new or used cars. Includes structured payments, optional balloon payment, and flexible terms.
Best for: car buyers who want to finance a vehicle purchase with stable monthly payments.
Redirect URL: /products/car-loan

Investment Account
A brokerage-style account for investing in stocks, bonds, ETFs, and managed funds. Includes market research tools and portfolio tracking.
Best for: active investors and DIY traders who want direct market access.
Redirect URL: /products/investment-account

Retirement Plan
A long-term savings plan to build retirement income. Offers tax-advantaged contributions, compounding growth, and payout planning.
Best for: professionals focused on building retirement savings over many years.
Redirect URL: /products/retirement-plan

Business Account
A dedicated account for small businesses and freelancers. Supports invoicing, merchant services, payroll, and cash flow management.
Best for: entrepreneurs and small business owners needing separate business banking.
Redirect URL: /products/business-account

Business Credit Line
A revolving line of credit for business operating expenses. Provides flexible access to working capital with interest only on amounts used.
Best for: businesses that require ongoing cash flow support and flexible borrowing.
Redirect URL: /products/business-credit-line

Digital Wallet
A mobile payments product for storing cards, transferring money, and paying with a phone or wearable device.
Best for: tech-savvy customers who want fast, contactless payment options.
Redirect URL: /products/digital-wallet

Expense Management Service
A digital service that categorizes spending, tracks budgets, and helps customers manage subscriptions and bills.
Best for: budget-conscious customers and small business owners who want stronger expense control.
Redirect URL: /products/expense-management-service

Wealth Management
A full-service investment advisory product offering portfolio construction, tax planning, and personalized wealth strategies.
Best for: wealthy individuals and high-net-worth customers seeking tailored financial planning.
Redirect URL: /products/wealth-management

Mutual Fund Investment
A pooled investment product that buys stocks and bonds on behalf of investors, with professional fund management.
Best for: long-term investors who want diversified exposure without managing individual securities.
Redirect URL: /products/mutual-fund-investment

ETF Portfolio
A portfolio of exchange-traded funds that can be traded like stocks and provides diversified exposure to markets.
Best for: cost-conscious investors seeking flexibility and broad market access.
Redirect URL: /products/etf-portfolio

Private Equity Product
A specialized investment opportunity focused on non-public companies and venture funding for growth-stage businesses.
Best for: accredited investors and private banking clients with a higher risk tolerance.
Redirect URL: /products/private-equity-product

Real Estate Investment Trust (REIT)
A pooled investment vehicle that owns income-generating real estate properties and distributes rental income to investors.
Best for: investors seeking real estate exposure with regular income and portfolio diversification.
Redirect URL: /products/reit

ESG Investment Product
An investment product focused on companies with strong environmental, social, and governance practices.
Best for: socially responsible investors who want to align their portfolio with sustainability goals.
Redirect URL: /products/esg-investment-product

Structured Note
A custom investment product combining bonds and derivatives to provide a tailored return profile tied to specific market outcomes.
Best for: sophisticated investors looking for customized risk/reward exposure.
Redirect URL: /products/structured-note

Wealth Preservation Account
A conservative investment bucket designed to preserve capital while delivering modest returns and liquidity.
Best for: retirees and wealth preservation clients who prioritize capital safety.
Redirect URL: /products/wealth-preservation-account
""";
}