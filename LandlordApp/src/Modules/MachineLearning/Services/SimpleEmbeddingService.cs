namespace Lander.src.Modules.MachineLearning.Services;

/// <summary>
/// .NET 10 Feature: Vector embeddings for semantic search
/// Simple TF-IDF-like approach without external dependencies
/// </summary>
public class SimpleEmbeddingService
{
    private const int EmbeddingDimensions = 128;
    
    /// <summary>
    /// Generate a vector embedding from text using simple hash-based approach
    /// </summary>
    public float[] GenerateEmbedding(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[EmbeddingDimensions];
        
        var embedding = new float[EmbeddingDimensions];
        var words = text.ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, 
                   StringSplitOptions.RemoveEmptyEntries);
        
        // Simple TF-IDF-like approach
        var wordFrequency = new Dictionary<string, int>();
        foreach (var word in words)
        {
            if (word.Length < 3) continue; // Skip short words
            
            wordFrequency[word] = wordFrequency.GetValueOrDefault(word, 0) + 1;
        }
        
        // Map words to embedding dimensions using hash
        foreach (var (word, frequency) in wordFrequency)
        {
            var hash = Math.Abs(word.GetHashCode() % EmbeddingDimensions);
            embedding[hash] += frequency;
        }
        
        // Normalize to unit vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
                embedding[i] /= magnitude;
        }
        
        return embedding;
    }
    
    /// <summary>
    /// Calculate cosine similarity between two embeddings
    /// Returns value between -1 and 1, where 1 means identical
    /// </summary>
    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embeddings must have same dimensions");
        
        return a.Zip(b, (x, y) => x * y).Sum();
    }
    
    /// <summary>
    /// Find most similar items from a list
    /// </summary>
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
