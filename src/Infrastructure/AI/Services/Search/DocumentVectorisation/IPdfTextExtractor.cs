using System.IO;
using System.Threading.Tasks;

namespace AiAdvisor.Infrastructure.AI.Services;

public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts plain text from a PDF stream.
    /// </summary>
    /// <param name="pdfStream">Stream containing PDF data.</param>
    /// <returns>Extracted text.</returns>
    Task<string> ExtractTextAsync(Stream pdfStream);
}
