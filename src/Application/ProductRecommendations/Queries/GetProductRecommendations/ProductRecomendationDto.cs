namespace AiAdvisor.Application;

public class ProductRecomendationDto
{
    public string ProductName    { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string Reason     { get; init; } = string.Empty;
    public string Prompt  { get; init; } = string.Empty;
}
