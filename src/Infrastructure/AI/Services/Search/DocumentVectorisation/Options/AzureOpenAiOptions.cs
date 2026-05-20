using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace AiAdvisor.Infrastructure.AI.Services.Options;

public class AzureOpenAiOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [ConfigurationKeyName("Model")]
    public string EmbeddingDeployment { get; set; } = string.Empty;
}