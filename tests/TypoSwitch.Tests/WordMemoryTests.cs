using TypoSwitch;
using Xunit;

namespace TypoSwitch.Tests;

public class WordMemoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ts-mem-" + Guid.NewGuid().ToString("N"));

    public WordMemoryTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch { /* temp cleanup */ }
    }

    [Fact]
    public void LearnsExceptionAfterRepeatedUndos()
    {
        var memory = new WordMemory(_dir);

        Assert.Equal(MemoryUpdate.Tracked, memory.RecordUndo("ghbdtn"));
        Assert.Equal(MemoryUpdate.Tracked, memory.RecordUndo("ghbdtn"));
        Assert.Equal(MemoryUpdate.Tracked, memory.RecordUndo("ghbdtn"));
        Assert.Equal(MemoryUpdate.Learned, memory.RecordUndo("ghbdtn"));
        Assert.Contains("ghbdtn", memory.LearnedExceptions);
        Assert.Equal(MemoryUpdate.None, memory.RecordUndo("ghbdtn"));
    }

    [Fact]
    public void LearnsWordAfterRepeatedTyping()
    {
        const string word = "blorpt";
        var memory = new WordMemory(_dir);

        for (var i = 1; i < WordMemory.DefaultWordLearnAfter; i++)
            Assert.Equal(MemoryUpdate.Tracked, memory.RecordKeptWord(word, minLength: 3));

        Assert.Equal(MemoryUpdate.Learned, memory.RecordKeptWord(word, minLength: 3));
        Assert.Contains(word, memory.LearnedWords);
        Assert.Equal(MemoryUpdate.None, memory.RecordKeptWord(word, minLength: 3));
    }

    [Fact]
    public void DoesNotTrackDictionaryWords()
    {
        var memory = new WordMemory(_dir);
        Assert.Equal(MemoryUpdate.None, memory.RecordKeptWord("hello", minLength: 3));
        Assert.Equal(MemoryUpdate.None, memory.RecordKeptWord("привет", minLength: 3));
        Assert.Empty(memory.LearnedWords);
    }

    [Fact]
    public void PersistsLearnedData()
    {
        var first = new WordMemory(_dir);
        for (var i = 0; i < WordMemory.DefaultWordLearnAfter; i++)
            first.RecordKeptWord("blorpt", minLength: 3);
        for (var i = 0; i < WordMemory.DefaultUndoLearnAfter; i++)
            first.RecordUndo("asdfgh");
        first.Save();

        var loaded = new WordMemory(_dir);
        Assert.Contains("blorpt", loaded.LearnedWords);
        Assert.Contains("asdfgh", loaded.LearnedExceptions);
    }

    [Fact]
    public void ClearRemovesLearnedData()
    {
        var memory = new WordMemory(_dir);
        for (var i = 0; i < WordMemory.DefaultUndoLearnAfter; i++)
            memory.RecordUndo("zxcvbn");
        memory.Save();
        Assert.True(memory.HasLearned);

        memory.Clear();
        Assert.False(memory.HasLearned);
        Assert.Empty(memory.LearnedExceptions);

        var loaded = new WordMemory(_dir);
        Assert.False(loaded.HasLearned);
    }
}
