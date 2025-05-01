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

    public bool UnionWith(SparsePatternSet other)
    {   if (other.IsUnobserved) {
            // union with another unobserved set is unobserved
            bool was_unobserved = _unobserved;
            Clear();
            return !was_unobserved;
        }

        bool changed = false;
        if (_unobserved) {
            foreach (var kvp in other.chunks)
                chunks[kvp.Key] = kvp.Value;
                _unobserved = false;
                return true;
        } else {
            foreach (var kvp in other.chunks)
            {
                if (chunks.ContainsKey(kvp.Key)) {
                    ulong prev_chunk = chunks[kvp.Key];
                    chunks[kvp.Key] |= kvp.Value;
                    changed |= prev_chunk != chunks[kvp.Key];
                } else {
                    chunks[kvp.Key] = kvp.Value;
                    changed = true;
                }
            }
        }
        return changed;
    }

    public bool IntersectWith(SparsePatternSet other)
    {
        if (other.IsUnobserved) {
            // if the other set is unobserved, the intersection doesn't change anything
            return false;
        } else if (_unobserved) {
            foreach (var kvp in other.chunks)
                chunks[kvp.Key] = kvp.Value;
            
            _unobserved = false;
            return true;
        }

        var keysToRemove = new List<int>();

        bool changed = false;
        foreach (var kvp in chunks)
        {
            if (other.chunks.TryGetValue(kvp.Key, out ulong otherChunk))
            {
                ulong prev_chunk = chunks[kvp.Key];
                chunks[kvp.Key] &= otherChunk;
                changed |= prev_chunk != chunks[kvp.Key];

                if (chunks[kvp.Key] == 0)
                    keysToRemove.Add(kvp.Key);
            }
            else
            {
                keysToRemove.Add(kvp.Key);
                changed = true;
            }
        }

        foreach (int key in keysToRemove)
            chunks.Remove(key);
        
        return changed;
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

    public void Observe() {
        _unobserved = false;
    }

    public SparsePatternSet Copy()
    {
        var copy = new SparsePatternSet();
        copy.chunks = new Dictionary<int, ulong>(chunks);
        copy._unobserved = _unobserved;
        return copy;
    }
}

}