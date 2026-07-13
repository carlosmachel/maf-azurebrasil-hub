using System.Text.RegularExpressions;

namespace RagDemo.Core;

public static class ChunkingService
{
    private const int ChunkWordTarget = 500;
    private const int ChunkWordOverlap = 50;

    private static readonly Regex SentenceSplitter = new(@"(?<=[\.\!\?])\s+", RegexOptions.Compiled);

    public static IReadOnlyList<string> Chunk(string text)
    {
        var sentences = SentenceSplitter.Split(text.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var chunks = new List<string>();
        var current = new List<string>();
        var currentWordCount = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (currentWordCount + sentenceWordCount > ChunkWordTarget && current.Count > 0)
            {
                chunks.Add(string.Join(' ', current));
                current = CarryOverlap(current);
                currentWordCount = current.Sum(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            }

            current.Add(sentence);
            currentWordCount += sentenceWordCount;
        }

        if (current.Count > 0)
        {
            chunks.Add(string.Join(' ', current));
        }

        return chunks;
    }

    private static List<string> CarryOverlap(List<string> sentences)
    {
        var overlap = new List<string>();
        var wordCount = 0;

        for (var i = sentences.Count - 1; i >= 0 && wordCount < ChunkWordOverlap; i--)
        {
            overlap.Insert(0, sentences[i]);
            wordCount += sentences[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        return overlap;
    }
}
