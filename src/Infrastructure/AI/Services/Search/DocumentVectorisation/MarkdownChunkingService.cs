using System.Text;
using System.Text.RegularExpressions;
using AiAdvisor.Infrastructure.AI.Models;

namespace AiAdvisor.Infrastructure.AI.Services;

public interface IMarkdownChunkingService
{
    Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkDocumentAsync(
        string filePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50);

    Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkContentAsync(
        string content,
        string sourceFileName,
        string sourceFilePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50);
}

public class MarkdownChunkingService : IMarkdownChunkingService
{
    private const int CharsPerToken = 4;

    private static readonly Regex HeadingRegex =
        new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled);

    private static readonly Regex ListItemRegex =
        new(@"^(\s*[-*+]\s+.+)$", RegexOptions.Compiled);

    private static readonly Regex CodeFenceRegex =
        new(@"^```", RegexOptions.Compiled);

    public async Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkDocumentAsync(
        string filePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(filePath);

        var content = await File.ReadAllTextAsync(filePath);

        return await ChunkContentAsync(
            content,
            Path.GetFileName(filePath),
            filePath,
            maxTokensPerChunk,
            overlapTokens);
    }

    public Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkContentAsync(
        string content,
        string sourceFileName,
        string sourceFilePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<IReadOnlyList<(string, DocumentMetadata)>>(Array.Empty<(string, DocumentMetadata)>());

        var blocks = ParseBlocks(content);
        var chunks = PackBlocks(
            blocks,
            sourceFileName,
            sourceFilePath,
            maxTokensPerChunk,
            overlapTokens);

        return Task.FromResult<IReadOnlyList<(string, DocumentMetadata)>>(chunks);
    }

    #region Block model

    private record Block(string Type, string Content, int Tokens);

    #endregion

    #region Parsing

    private List<Block> ParseBlocks(string content)
    {
        var lines = content.Split(Environment.NewLine);
        var blocks = new List<Block>();

        var sb = new StringBuilder();

        bool inCode = false;
        bool inList = false;

        foreach (var line in lines)
        {
            // Code blocks (atomic)
            if (CodeFenceRegex.IsMatch(line))
            {
                if (inCode)
                {
                    sb.AppendLine(line);
                    blocks.Add(MakeBlock("code", sb.ToString()));
                    sb.Clear();
                    inCode = false;
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        blocks.Add(MakeBlock("paragraph", sb.ToString()));
                        sb.Clear();
                    }
                    sb.AppendLine(line);
                    inCode = true;
                }

                continue;
            }

            if (inCode)
            {
                sb.AppendLine(line);
                continue;
            }

            // Heading
            var headingMatch = HeadingRegex.Match(line);
            if (headingMatch.Success)
            {
                if (sb.Length > 0)
                {
                    blocks.Add(MakeBlock(inList ? "list" : "paragraph", sb.ToString()));
                    sb.Clear();
                    inList = false;
                }

                blocks.Add(MakeBlock("heading", line));
                continue;
            }

            // List items
            if (ListItemRegex.IsMatch(line))
            {
                inList = true;
                sb.AppendLine(line);
                continue;
            }

            // Paragraph break
            if (string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    blocks.Add(MakeBlock(inList ? "list" : "paragraph", sb.ToString()));
                    sb.Clear();
                    inList = false;
                }
                continue;
            }

            sb.AppendLine(line);
        }

        if (sb.Length > 0)
            blocks.Add(MakeBlock(inList ? "list" : "paragraph", sb.ToString()));

        return blocks;
    }

    private static Block MakeBlock(string type, string content)
    {
        var tokens = EstimateTokens(content);
        return new Block(type, content.Trim(), tokens);
    }

    #endregion

    #region Packing

    private List<(string Content, DocumentMetadata Metadata)> PackBlocks(
        List<Block> blocks,
        string fileName,
        string filePath,
        int maxTokens,
        int overlapTokens)
    {
        var chunks = new List<(string, DocumentMetadata)>();

        var current = new List<Block>();
        int currentTokens = 0;
        int chunkIndex = 0;

        var context = new List<string>();

        foreach (var block in blocks)
        {
            // Update heading context
            if (block.Type == "heading")
                UpdateContext(context, block.Content);

            if (currentTokens + block.Tokens > maxTokens && current.Count > 0)
            {
                chunks.Add(CreateChunk(current, context, fileName, filePath, chunkIndex++));

                current = TakeOverlap(current, overlapTokens);
                currentTokens = current.Sum(b => b.Tokens);
            }

            current.Add(block);
            currentTokens += block.Tokens;
        }

        if (current.Count > 0)
        {
            chunks.Add(CreateChunk(current, context, fileName, filePath, chunkIndex));
        }

        return chunks;
    }

    private static List<Block> TakeOverlap(List<Block> blocks, int overlapTokens)
    {
        var result = new List<Block>();
        int tokens = 0;

        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            var b = blocks[i];
            result.Insert(0, b);
            tokens += b.Tokens;

            if (tokens >= overlapTokens)
                break;
        }

        return result;
    }

    private static void UpdateContext(List<string> context, string heading)
    {
        if (context.Count == 0)
        {
            context.Add(heading);
            return;
        }

        context.Add(heading);

        if (context.Count > 4)
            context.RemoveAt(0);
    }

    private static (string Content, DocumentMetadata Metadata) CreateChunk(
        List<Block> blocks,
        List<string> context,
        string fileName,
        string filePath,
        int index)
    {
        var content = new StringBuilder();

        content.AppendLine($"Source: {fileName}");
        content.AppendLine($"Context: {string.Join(" > ", context)}");
        content.AppendLine("---");

        foreach (var b in blocks)
        {
            content.AppendLine(b.Content);
        }

        var text = content.ToString();

        return (text, new DocumentMetadata
        {
            ChunkId = $"{Path.GetFileNameWithoutExtension(fileName)}_{index}",
            SourceFileName = fileName,
            SourceFilePath = filePath,
            ChunkIndex = index,
            TokenCount = EstimateTokens(text),
            IndexedAt = DateTime.UtcNow
        });
    }

    #endregion

    #region Token estimation

    private static int EstimateTokens(string text)
        => (int)Math.Ceiling(text.Length / (double)CharsPerToken);

    #endregion
}