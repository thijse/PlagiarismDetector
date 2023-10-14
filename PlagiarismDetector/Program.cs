using DataChunker;
using OpenAI.Managers;
using OpenAI;

namespace MemoryVectorDB_sample
{
    
    internal partial class Program
    {
        static async Task Main(string[] args)
        {
            string OpenAIkey           = File.Exists("apikey.txt") ?File.ReadAllText("apikey.txt") :""; // "API key here"; // OpenAI key
            string document1Path        = @"docs\A.pdf";                                                // PDF document 1
            string document2Path        = @"docs\B.pdf";                                                // PDF document 2 
            string document1VectorsPath = $"{document1Path}.json";                                      // Vectors of document 1 created by embedding algorithm
            string document2VectorsPath = $"{document2Path}.json";                                      // Vectors of document 2 created by embedding algorithm

            string document1TextPath    = $"{document1Path}.txt";                                       // text only document 1
            string document2TextPath    = $"{document2Path}.txt";                                       // text only document 2

            Console.WriteLine("** Starting embedding demo");
            var openAiService = new OpenAIService(new OpenAiOptions() { ApiKey = OpenAIkey });

            var embeddingDocument1 = new Embedding(openAiService);
            var embeddingDocument2 = new Embedding(openAiService);
            var PlagiarismDetector = new PlagiaryDetector(embeddingDocument1, embeddingDocument2, openAiService);

            // Create embeddings, only needed once
            if (File.Exists(document1VectorsPath) && File.Exists(document1TextPath))
            {
                Console.WriteLine("** Vectors already exist, reading previous embedding");
                // Read embedding
                embeddingDocument1.DeserializeDocumentText(document1TextPath);
                await embeddingDocument1.DeserializeVectorsAsync(document1VectorsPath);
            }
            else
            {
                Console.WriteLine("** Embedding document");
                await embeddingDocument1.WordEmbeddingAsync(document1Path, document1TextPath);
                embeddingDocument1.SerializeDocumentText(document1TextPath);
                await embeddingDocument1.SerializeVectorsAsync(document1VectorsPath);
            }

            if (File.Exists(document2VectorsPath) && File.Exists(document2TextPath))
            {
                Console.WriteLine("** Vectors already exist, reading previous embedding");
                // Read embedding
                embeddingDocument2.DeserializeDocumentText(document2TextPath);
                await embeddingDocument2.DeserializeVectorsAsync(document2VectorsPath);
            }
            else
            {
                Console.WriteLine("** Embedding document");
                await embeddingDocument2.WordEmbeddingAsync(document2Path, document2TextPath);
                embeddingDocument2.SerializeDocumentText(document2TextPath);
                await embeddingDocument2.SerializeVectorsAsync(document2VectorsPath);
            }

            var findings = await PlagiarismDetector.GetFilteredFindingsAsync();

            Console.WriteLine("** Done");
        }

 

    }
}