using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AiAdvisor.Infrastructure.AI.Services;

public class AzureSearchIndexCreator
{
    private readonly SearchIndexClient _indexClient;

    public AzureSearchIndexCreator(string endpoint, string apiKey)
    {
        _indexClient = new SearchIndexClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task CreateIndexAsync(string indexName)
    {
        var fields = new List<SearchField>
        {
            new SimpleField("Id", SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },

            new SearchableField("Content"),

            new SimpleField("DocumentId", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SimpleField("FileName", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = 3072,
                VectorSearchProfileName = "vector-profile"
            },

            new SimpleField("SourceFileName", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SimpleField("SectionHeading", SearchFieldDataType.String)
            {
                IsFilterable = true
            },

            new SimpleField("HeadingLevel", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },

            new SimpleField("ChunkIndex", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },

            new SimpleField("TotalChunks", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },

            new SimpleField("TokenCount", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },

            new SimpleField("IndexedAt", SearchFieldDataType.DateTimeOffset)
            {
                IsFilterable = true,
                IsSortable = true
            }
        };

        var vectorSearch = new VectorSearch
        {
            Algorithms =
            {
                new HnswAlgorithmConfiguration("hnsw-config")
            },
            Profiles =
            {
                new VectorSearchProfile(
                    "vector-profile",
                    "hnsw-config")
            }
        };


        var index = new SearchIndex(indexName)
        {
            Fields = fields,
            VectorSearch = vectorSearch
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);

        Console.WriteLine($"Index '{indexName}' created successfully.");
    }
}


