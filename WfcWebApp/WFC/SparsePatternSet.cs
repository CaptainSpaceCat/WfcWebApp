using System.Numerics;

namespace WfcWebApp.Wfc
{
public class SparsePatternSet
{
    private bool _unobserved = true;
    public bool IsUnobserved { get {return _unobserved;} }
    public bool IsCollapsed { get {return !_unobserved && Count == 1;}}
    public bool IsContradiction { get {return !_unobserved && Count == 0;}}

    private Dictionary<int, ulong> chunks = new(); // Maps chunk indices to their bitmasks

    //TODO instead of pop count, iterate over the indices, and get the weight of each active pattern, sum the WEIGHTS
    // needs to access the mapping from index -> PatternView
    public int Count => _unobserved ? int.MaxValue : chunks.Values.Sum(chunk => BitOperations.PopCount(chunk));

    public void Add(int patternIndex)
    {
        int chunkIndex = patternIndex / 64;
        int bitIndex = patternIndex % 64;

        if (!chunks.ContainsKey(chunkIndex))
            chunks[chunkIndex] = 0;

        chunks[chunkIndex] |= 1UL << bitIndex;
        _unobserved = false;
    }

    public void Clear() {
        chunks.Clear();
        _unobserved = true;
    }

    public bool Contains(int patternIndex)
    {
        int chunkIndex = patternIndex / 64;
        int bitIndex = patternIndex % 64;

        return chunks.TryGetValue(chunkIndex, out ulong chunk) && ((chunk & (1UL << bitIndex)) != 0);
    }

    public void UnionWith(SparsePatternSet other)
    {
        if (_unobserved) {
            foreach (var kvp in other.chunks)
                chunks[kvp.Key] = kvp.Value;
        } else {
            foreach (var kvp in other.chunks)
            {
                if (chunks.ContainsKey(kvp.Key))
                    chunks[kvp.Key] |= kvp.Value;
                else
                    chunks[kvp.Key] = kvp.Value;
            }
        }
        _unobserved = false;
    }

    public void IntersectWith(SparsePatternSet other)
    {
        var keysToRemove = new List<int>();

        foreach (var kvp in chunks)
        {
            if (other.chunks.TryGetValue(kvp.Key, out ulong otherChunk))
            {
                chunks[kvp.Key] &= otherChunk;

                if (chunks[kvp.Key] == 0)
                    keysToRemove.Add(kvp.Key);
            }
            else
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (int key in keysToRemove)
            chunks.Remove(key);
        
        if (_unobserved) {
            foreach (var kvp in other.chunks)
                chunks[kvp.Key] = kvp.Value;
        }

        _unobserved = false;
    }

    public IEnumerator<int> GetEnumerator() {
        foreach (var kvp in chunks) {
            int base_index = kvp.Key * 64;
            ulong mask = kvp.Value;
            while (mask != 0) {
                yield return base_index + BitOperations.TrailingZeroCount(mask);
                mask &= mask - 1; //clear lowest active bit
            }
        }
    }
}

}