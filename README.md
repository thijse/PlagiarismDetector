[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

# Plagiarism Dectector
A sample showing how Vector comparison can be used to easily and effectively detect plagiarismm using a  simple in-memory vector store 

The repository contains three main projects:
- Memory Vector Store project, which focuses on storing vectors in memory;
- Chunk Creator project, which extracts vectors from PDF files;
-Plagiarism project, which demonstrates how to perform similarity searches using the stored vectors and use OpenAI . Each project has its own set of code and resources, allowing you to explore and understand the implementation details.

 This code is based on the code in [MemoryVectorStore](https://github.com/thijse/MemoryVectorStore)


## Code example

First we need to make chunks both PDFs and build the embedding vectors

```cs
//  OpenAI service that we are going to use for embedding
_openAiService = new OpenAIService(new OpenAiOptions()  {ApiKey = apiKey });

// Set up a MemoryVector database, to be filled with chunks of documents
// including an embedding vector of 1536 dimensions
// Also included is a callback that embeds any text item into a vector
_vectorCollection = new MemoryVectorDB.VectorDB<Chunk>(1536, ChunkEmbedingAsync);

// Get text fom pdf 
_document = PdfTextExtractor.GetText(documentPath);

// Generate sentences of max 50 words
_chunkGenerator = new ChunkGenerator(50,  _document);

// Loop through chunks
foreach (var chunk in _chunkGenerator.GetChunk())
{
    // Add the source reference to the chunk
    chunk.Source = documentPath;

    // Add the chunk to the vector store
    await _vectorCollection.AddAsync(chunk);

    // We remove the text from the chunk to safe memory:
    // we just need the vector, start index, length and source
    // so we can recover the the chunk from the original document later
    chunk.Text = null!;
}
```

Now we can find the best matching sentences between both documents

```cs
var vectorObjects1 = _embedding1.VectorCollection.VectorObjects;
var vectorObjects2 = _embedding2.VectorCollection.VectorObjects;

// Find the closest matching vectors between the 2 documents
var bestMatches    = FindNearestSorted(vectorObjects1, vectorObjects2, 100);

// And here they are
foreach (var item in bestMatches)
{
    ShowMatch(true, item.Value.Item1, item.Value.Item2);
}
```
Note that the FindNearestSorted is just a brute-force comparison of the (normalized) dot products between the query vector and all chunk vectors. For larger vector stores,  a database should be used that implements an indexing system for efficient nearest neighbour searches  [using something like this library](https://github.com/curiosity-ai/hnsw-sharp)

However, not all closely matching vectors might constitute plagiarism. Luckily we have the LLM to compare the highest ranking vector in-porducts

```cs
// Format the query to post to the LLM:
foreach (var item in bestMatches)
{
	var isPlagiarism = await FormulateComparisonAsync(item.Value.Item1, item.Value.Item2);
}
```

