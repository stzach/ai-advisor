using System.IO;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace AiAdvisor.Infrastructure.AI.Services;

public class PdfPigPdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream)
    {
        if (pdfStream == null) return Task.FromResult(string.Empty);

        using var document = PdfDocument.Open(pdfStream);
        var sb = new StringBuilder();
        int pageIndex = 1;

        foreach (var page in document.GetPages())
        {
            sb.AppendLine($"--- Page {pageIndex} ---");
            sb.AppendLine(page.Text);
            sb.AppendLine();
            pageIndex++;
        }

        return Task.FromResult(sb.ToString());
    }
}
