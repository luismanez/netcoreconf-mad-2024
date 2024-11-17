// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;

namespace Demo;

#pragma warning disable SKEXP0001
public class Search
{
    private readonly IVectorStoreRecordCollection<string, Paragraph> _collection;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;
    private readonly ILogger<Search> _logger;

    public Search(
        IVectorStore vectorStore,
        ITextEmbeddingGenerationService embeddingGenerationService,
        ILogger<Search> logger
    )
    {
        var collection = vectorStore.GetCollection<string, Paragraph>("romeoandjuliet");
        collection.CreateCollectionIfNotExistsAsync().GetAwaiter().GetResult();
        _collection = collection;
        _embeddingGenerationService = embeddingGenerationService;
        _logger = logger;
    }

    [KernelFunction("Search")]
    [Description("Search for a document similar to the given query")]
    public async Task<string> SearchAsync(string query)
    {
        var vectorSearch = _collection as Microsoft.Extensions.VectorData.IVectorizedSearch<Paragraph>;
        var searchVector = await _embeddingGenerationService.GenerateEmbeddingAsync(query);
        var searchResult =
            await vectorSearch!.VectorizedSearchAsync(
                searchVector, new() { Top = 1 }
            ).Result.Results.ToListAsync();

        _logger.LogInformation("Got search result:\n{searchResult}", searchResult);
        return searchResult.FirstOrDefault()!.Record.Text;
    }
}

#pragma warning restore SKEXP0001
