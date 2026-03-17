namespace Lander.src.Modules.MachineLearning.Services;

public class SimpleEmbeddingService
{
    private const int EmbeddingDimensions = 128;
    
    public float[] GenerateEmbedding(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[EmbeddingDimensions];
        
        var embedding = new float[EmbeddingDimensions];
        var words = text.ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, 
                   StringSplitOptions.RemoveEmptyEntries);
        
        var wordFrequency = new Dictionary<string, int>();
        foreach (var word in words)
        {
            if (word.Length < 3) continue; 
            
            wordFrequency[word] = wordFrequency.GetValueOrDefault(word, 0) + 1;
        }
        
        foreach (var (word, frequency) in wordFrequency)
        {
            var hash = Math.Abs(word.GetHashCode() % EmbeddingDimensions);
            embedding[hash] += frequency;
        }
        
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
                embedding[i] /= magnitude;
        }
        
        return embedding;
    }
    
    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embeddings must have same dimensions");
        
        return a.Zip(b, (x, y) => x * y).Sum();
    }
    
    public List<(T item, float score)> FindMostSimilar<T>(
        float[] queryEmbedding,
        IEnumerable<(T item, float[] embedding)> items,
        int topN = 10)
    {
        return items
            .Select(x => (x.item, score: CosineSimilarity(queryEmbedding, x.embedding)))
            .OrderByDescending(x => x.score)
            .Take(topN)
            .ToList();
    }
}
