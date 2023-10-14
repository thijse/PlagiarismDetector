using DataChunker;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using System.Text;
using OpenAI.Managers;

namespace MemoryVectorDB_sample
{
    public class PlagiaryDetector
    {
        private Embedding     _embedding1;
        private Embedding     _embedding2;
        private OpenAIService _openAiService;
        public PlagiaryDetector(Embedding embedding1, Embedding embedding2, OpenAIService openAiService)
        {
            _embedding1    = embedding1;
            _embedding2    = embedding2;
            _openAiService = openAiService;
        }

        public SortedList<float, Tuple<Chunk, Chunk>> GetFindings()
        {
            var vectorObjects1 = _embedding1.VectorCollection.VectorObjects;
            var vectorObjects2 = _embedding2.VectorCollection.VectorObjects;

            var bestMatches = FindNearestSorted(vectorObjects1, vectorObjects2, 100);

            foreach (var item in bestMatches)
            {
                ShowMatch(true,item.Value.Item1, item.Value.Item2);
            }
            return bestMatches;
        }

        public async Task<SortedList<float, Tuple<Chunk, Chunk>>> GetFilteredFindingsAsync()
        {
            var vectorObjects1 = _embedding1.VectorCollection.VectorObjects;
            var vectorObjects2 = _embedding2.VectorCollection.VectorObjects;
            var bestMatches    = FindNearestSorted(vectorObjects1, vectorObjects2, 100);
            var nonMatches     = 0;

            foreach (var item in bestMatches)
            {
                var isPlagiarism = await FormulateComparisonAsync(item.Value.Item1, item.Value.Item2);
                if (isPlagiarism)
                {
                    nonMatches = 0;
                    ShowMatch(true, item.Value.Item1, item.Value.Item2);
                } else
                {
                    nonMatches++;
                    ShowMatch(false, item.Value.Item1, item.Value.Item2);
                    if (nonMatches > 5) break;
                }                
            }
            return bestMatches;
        }

        private void ShowMatch(bool isPlagiarism, Chunk chunk1, Chunk chunk2)
        {
            // Show the match if the text with the query and the text itself
            var dotProduct = DotProduct(chunk1.GetVector(), chunk2.GetVector());
            Console.WriteLine($"{(isPlagiarism?"MATCH":"NOMATCH")}: {dotProduct} - ");
            Console.WriteLine("'" + _embedding1.Document?.Text.Substring(chunk1.StartCharNo, chunk1.CharLength) ?? "" + "'");
            Console.WriteLine("vs");
            Console.WriteLine("'" + _embedding2.Document?.Text.Substring(chunk2.StartCharNo, chunk2.CharLength) ?? "" + "'");
            Console.WriteLine();
            Console.WriteLine();
        }

        public SortedList<float, Tuple<Chunk, Chunk>> FindNearestSorted(List<Chunk> vectorObjects1, List<Chunk> vectorObjects2, int noItems)
        {

            var descending = Comparer<float>.Create((a, b) => Comparer<float>.Default.Compare(b, a));

            SortedList<float, Tuple<Chunk, Chunk>> nearestObjects = new(descending);

            for (int i = 1; i < vectorObjects1.Count; i++)
            {
                for (int j = 1; j < vectorObjects2.Count; j++)
                {
                    // Find the lowest dot product of the top finds
                    float maxDotProduct = nearestObjects.Count != 0 ? nearestObjects.Last().Key : 0;
                    // check if the current object is closer than the lowest dot product in the top finds
                    float dotProduct = DotProduct(vectorObjects1[i].GetVector(), vectorObjects2[j].GetVector());

                    if (dotProduct > maxDotProduct)
                    {
                        // Add to the list
                        nearestObjects.Add(dotProduct, new Tuple<Chunk, Chunk>(vectorObjects1[i], vectorObjects2[j]));
                        // Remove the last item if the list is too long
                        if (nearestObjects.Count > noItems) nearestObjects.RemoveAt(nearestObjects.Count - 1);
                    }
                }
            }
            return nearestObjects;
        }


        public async Task<bool> FormulateComparisonAsync(Chunk chunk1, Chunk chunk2)
        {
            StringBuilder queryBuilder = new StringBuilder();

            // Basic format of the query:
            queryBuilder.AppendLine($"You a plagiarism detector Look at the following two sentences, and compare the two. Respond with the word \"plagiarism\" if the meaning is mostly similar, although wording, spelling and grammar may have been changed. Respond with the word \"dissimilar\" if the sentence is essentially different in content. Describe how both lines of text are essentially the same or different \n\n");

            queryBuilder.AppendLine("'" + _embedding1.Document?.Text.Substring(chunk1.StartCharNo, chunk1.CharLength) ?? "" + "'");
            queryBuilder.AppendLine("compared to");
            queryBuilder.AppendLine("'" + _embedding2.Document?.Text.Substring(chunk2.StartCharNo, chunk2.CharLength) ?? "" + "'");

            // Ask Completion to answer the query
            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem("Your are an AI assistant. The assistant is helpful, factual and consise."),
                        ChatMessage.FromUser(queryBuilder.ToString()),
                    },
                Model = Models.Gpt_3_5_Turbo,
            });

            // Show the answer
            if (completionResult.Successful)
            {
                var content  = completionResult.Choices.First().Message.Content;
                if (content != null && content.ToLower().Contains("dissimilar")) { return false; }
                if (content != null && content.ToLower().Contains("plagiarism")) {return true; }                
            
                Console.WriteLine($"Wrong answer {content}");
                return false;
            }
            Console.WriteLine($"No answer");
            return false;
        }

        public static float DotProduct(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }

    }
}
