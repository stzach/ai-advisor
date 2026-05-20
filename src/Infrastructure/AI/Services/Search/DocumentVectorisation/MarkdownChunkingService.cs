using System.Text.RegularExpressions;
using AiAdvisor.Infrastructure.AI.Models;

namespace AiAdvisor.Infrastructure.AI.Services;

/// <summary>
/// Service for parsing and chunking markdown documents into manageable pieces
/// while preserving document structure and respecting token limits.
/// </summary>
public interface IMarkdownChunkingService
{
    /// <summary>
    /// Reads a markdown file and chunks it by sections.
    /// </summary>
    /// <param name="filePath">Full path to the markdown file</param>
    /// <param name="maxTokensPerChunk">Maximum tokens per chunk (approximate, ~4 chars per token)</param>
    /// <param name="overlapTokens">Number of tokens to overlap between chunks</param>
    /// <returns>List of document chunks with metadata</returns>
    Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkDocumentAsync(
        string filePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50);

    /// <summary>
    /// Chunks markdown content (as string) instead of reading from file.
    /// </summary>
    Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkContentAsync(
        string content,
        string sourceFileName,
        string sourceFilePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50);
}

/// <summary>
/// Implementation of markdown chunking service.
/// </summary>
public class MarkdownChunkingService : IMarkdownChunkingService
{
    private const int CharsPerToken = 4; // Rough estimate for token counting

    public async Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkDocumentAsync(
        string filePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Document not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileName(filePath);

        return await ChunkContentAsync(content, fileName, filePath, maxTokensPerChunk, overlapTokens);
    }

    public async Task<IReadOnlyList<(string Content, DocumentMetadata Metadata)>> ChunkContentAsync(
        string content,
        string sourceFileName,
        string sourceFilePath,
        int maxTokensPerChunk = 500,
        int overlapTokens = 50)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<(string, DocumentMetadata)>();
        }

        await Task.CompletedTask; // Allow async context

        var chunks = new List<(string Content, DocumentMetadata Metadata)>();

        // Split by heading levels (## or ###)
        var sectionPattern = @"^(#{2,3})\s+(.+)$";
        var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        var currentSection = "";
        var currentHeadingLevel = 0;
        var sectionContent = new List<string>();
        var chunkIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = Regex.Match(line, sectionPattern, RegexOptions.Multiline);

            if (match.Success)
            {
                // We found a new heading - process accumulated content from previous section
                if (sectionContent.Count > 0)
                {
                    var sectionText = string.Join(Environment.NewLine, sectionContent).Trim();
                    if (!string.IsNullOrWhiteSpace(sectionText))
                    {
                        CreateChunksFromSection(
                            sectionText,
                            currentSection,
                            currentHeadingLevel,
                            sourceFileName[..sourceFileName.LastIndexOf('.')],
                            sourceFilePath,
                            ref chunkIndex,
                            maxTokensPerChunk,
                            overlapTokens,
                            chunks);
                    }
                }

                // Start new section
                currentSection = match.Groups[2].Value;
                currentHeadingLevel = match.Groups[1].Value.Length - 1; // 2 hashes = level 1, 3 hashes = level 2
                sectionContent = new List<string> { line };
            }
            else
            {
                sectionContent.Add(line);
            }
        }

        // Process last section
        if (sectionContent.Count > 0)
        {
            var sectionText = string.Join(Environment.NewLine, sectionContent).Trim();
            if (!string.IsNullOrWhiteSpace(sectionText))
            {
                CreateChunksFromSection(
                    sectionText,
                    currentSection,
                    currentHeadingLevel,
                    sourceFileName,
                    sourceFilePath,
                    ref chunkIndex,
                    maxTokensPerChunk,
                    overlapTokens,
                    chunks);
            }
        }

        // Update total chunks count for all chunks
        var totalChunks = chunks.Count;
        for (int i = 0; i < chunks.Count; i++)
        {
            var (conten, metadata) = chunks[i];
            chunks[i] = (conten, metadata with { TotalChunks = totalChunks });
        }

        return chunks;
    }

    private void CreateChunksFromSection(
        string sectionContent,
        string sectionHeading,
        int headingLevel,
        string sourceFileName,
        string sourceFilePath,
        ref int chunkIndex,
        int maxTokensPerChunk,
        int overlapTokens,
        List<(string Content, DocumentMetadata Metadata)> chunks)
    {
        var maxChars = maxTokensPerChunk * CharsPerToken;
        var overlapChars = overlapTokens * CharsPerToken;

        // If section is small, add as single chunk
        if (sectionContent.Length <= maxChars)
        {
            var tokenCount = EstimateTokens(sectionContent);
            var metadata = new DocumentMetadata
            {
                ChunkId = $"{Path.GetFileNameWithoutExtension(sourceFileName)}_{chunkIndex}",
                SourceFileName = sourceFileName,
                SourceFilePath = sourceFilePath,
                SectionHeading = sectionHeading,
                HeadingLevel = headingLevel,
                ChunkIndex = chunkIndex,
                TokenCount = tokenCount,
                IndexedAt = DateTime.UtcNow
            };

            chunks.Add((sectionContent, metadata));
            chunkIndex++;
            return;
        }

        // Split large sections into overlapping chunks
        int offset = 0;
        while (offset < sectionContent.Length)
        {
            var endIndex = Math.Min(offset + maxChars, sectionContent.Length);

            // Try to break at sentence boundary
            if (endIndex < sectionContent.Length)
            {
                var lastPeriod = sectionContent.LastIndexOf('.', endIndex);
                var lastNewline = sectionContent.LastIndexOf('\n', endIndex);
                var breakPoint = Math.Max(lastPeriod, lastNewline);

                if (breakPoint > offset + (maxChars / 2)) // Only use if reasonable break point exists
                {
                    endIndex = breakPoint + 1;
                }
            }

            var chunk = sectionContent[offset..endIndex].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                var tokenCount = EstimateTokens(chunk);
                var metadata = new DocumentMetadata
                {
                    ChunkId = $"{Path.GetFileNameWithoutExtension(sourceFileName)}#{chunkIndex}",
                    SourceFileName = sourceFileName,
                    SourceFilePath = sourceFilePath,
                    SectionHeading = sectionHeading,
                    HeadingLevel = headingLevel,
                    ChunkIndex = chunkIndex,
                    TokenCount = tokenCount,
                    IndexedAt = DateTime.UtcNow
                };

                chunks.Add((chunk, metadata));
                chunkIndex++;
            }

            // Move offset with overlap
            offset = endIndex - overlapChars;
            if (offset <= 0) break;
        }
    }

    /// <summary>
    /// Rough estimate of token count (typically ~4 characters per token)
    /// </summary>
    private static int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / (double)CharsPerToken);
    }
}
