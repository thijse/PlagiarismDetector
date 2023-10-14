using MemoryVectorDB;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using OpenAI.Managers;
using DataChunker;

namespace MemoryVectorDB_sample
{

    public class Embedding
    {
        public VectorDB<Chunk>  VectorCollection;
        public Document?        Document;
        private OpenAIService   _openAiService;        
        private ChunkGenerator? _chunkGenerator;

        public Embedding(OpenAIService openAiService)
        {
            _openAiService = openAiService;
            //  OpenAI service that we are going to use for embedding   

            // Collection of vectors made of chunks of document
            VectorCollection = new MemoryVectorDB.VectorDB<Chunk>(100, ChunkEmbedingAsync);
        }

        public async Task WordEmbeddingAsync(string documentPath, string documentTextPath)
        {
            Document        = null;

            // Document to embed
            Document        = PdfTextExtractor.GetText(documentPath);

            // Chunk generator
            _chunkGenerator = new ChunkGenerator(200, 100, Document);

            var i = 0;
            // Get the chunks and embed them
            foreach (var chunk in _chunkGenerator.GetChunk())
            {
                Console.WriteLine($"***Chunk {i++}***");
                Console.WriteLine(chunk.Text);

                // Add the source reference
                chunk.Source = documentPath;

                // Embed the chunk
                await VectorCollection.AddAsync(chunk);

                // We clean out the text, to safe memory: we just need the vector, start index and length
                chunk.Text = null!;
            }
        }
        public async Task SerializeVectorsAsync(string fileName)
        {
            await VectorCollection.SerializeJsonAsync(fileName);
        }

        public void SerializeDocumentText(string documentTextPath)
        {
            // Write the document to disk
            if (Document == null) return;
            File.WriteAllText(documentTextPath, Document.Text);
        }

        internal void DeserializeDocumentText(string documentTextPath)
        {
            Document = new Document();
            Document.Add(File.ReadAllText(documentTextPath), "");
            Document.Source = documentTextPath;
        }

        internal async Task DeserializeVectorsAsync(string fileName)
        {
            await VectorCollection.DeserializeJsonAsync(fileName);
        }

        // Callback function for embedding in the vector database
        private async Task<Chunk?> ChunkEmbedingAsync(Chunk inputObject)
        {
            var embeddingResult = await _openAiService.Embeddings.CreateEmbedding(new EmbeddingCreateRequest()
            {
                InputAsList = new List<string> { inputObject.Text },
                Model       = Models.TextEmbeddingAdaV2
            });

            if (embeddingResult.Successful)
            {
                var value = embeddingResult.Data.FirstOrDefault()?.Embedding;
                if (value == null) return null!;
                inputObject.SetVector(value);
                return inputObject;
            }
            else { return null!; }
        }
    }
}