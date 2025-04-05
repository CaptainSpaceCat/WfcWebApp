using System.Diagnostics;
using System.Numerics;

namespace WfcWebApp.Wfc
{


public class PatternEncodingTrie
{

    private TrieNode root = new();

    public bool AddPattern(Pattern pattern) {
        TrieNode curr = root;
        foreach (int tileId in pattern) {
            curr = curr.GetOrAddChild(tileId);
        }
        return curr.AddLeaf(pattern);
    }

    public void InitializeWeight() {
        // recursively init the weights of the nodes in the trie
        root.InitializeWeight();
    }

    // Iterates through every pattern that would fit on top of the template with offset 1 in the specified direction (0 is up, 1 is right, ect)
    // if a match rotation is specified, will only return patterns that were sampled with the same rotation
    public IEnumerable<Pattern> MatchingPatterns(Pattern template, int direction) {
        Pattern rotatedTemplate = template.GetRotatedCopy(2 - direction);
        // do the search
        int i = 0;
        TrieNode? curr = root;
        foreach (int tileId in rotatedTemplate) {
            if (rotatedTemplate.Size > i++) {
                continue; //skip the first row of the convolution
            }
            curr = curr.GetChild(tileId);
            if (curr == null) {
                yield break; // if we find null on the way through this pattern, we know we have a contradiction
            }
        }
        // curr should now point to a treenode containing only patterns that would match the above template
        // we can just recursively iterate down the tree
        foreach (Pattern pattern in TraversePatterns(curr)) {
            yield return pattern.GetRotatedCopy(direction - 2);
        }

        
    }

    private IEnumerable<Pattern> TraversePatterns(TrieNode node) {
        if (node.Leaf != null) {
            yield return node.Leaf;
        } else {
            foreach (TrieNode child in node.GetAllChildren()) {
                foreach (Pattern pattern in TraversePatterns(child)) {
                    yield return pattern;
                }
            }
        }
    }

    public Pattern GetRandomPattern() {
        // sample a ticket index from 0 to full trie's weight
        int ticket = RandomUtils.Random.Next(root.Weight);
        
        return RandomPatternHelper(root, ticket);
    }
    private Pattern RandomPatternHelper(TrieNode node, int ticket) {
        if (node.Leaf != null) {
            return node.Leaf;
        }
        int cumulative_weight = 0;
        foreach (TrieNode child in node.GetAllChildren()) {
            cumulative_weight += child.Weight;
            if (cumulative_weight > ticket) {
                return RandomPatternHelper(child, ticket - cumulative_weight + child.Weight);
            }
        }
        throw new Exception("Failed to traverse pattern during weighted random sample");
    }
    
    public void Clear() {
        //TODO delete tree manually, but for now just mark the whole thing for GC
        root = new TrieNode();
    }

    public int CountWeight() {
        return root.Weight;
    }

    public int CountUnique() {
        return root.CountLeaves();
    }


    private class TrieNode
    {
        private Dictionary<int, TrieNode>? Children = null;
        
        public Pattern? Leaf = null;

        public int Weight { get; private set; }

        public void InitializeWeight() {
            if (Leaf != null) {
                Weight = Leaf.Weight;
            } else {
                int total = 0;
                foreach (TrieNode child in Children.Values) {
                    child.InitializeWeight();
                    total += child.Weight;
                }
                Weight = total;
            }
        }

        public TrieNode GetOrAddChild(int childIndex) {
            if (Children == null) {
                Children = new Dictionary<int, TrieNode>();
            }
            if (Children.TryGetValue(childIndex, out TrieNode child)) {
                return child;
            }
            Children[childIndex] = new TrieNode();
            return Children[childIndex];
        }

        public TrieNode? GetChild(int childIndex) {
            if (Children != null  && Children.TryGetValue(childIndex, out TrieNode child)) {
                return child;
            }
            return null;
        }

        public IEnumerable<TrieNode> GetAllChildren() {
            if (Children != null) {
                foreach (TrieNode child in Children.Values) {
                    yield return child;
                }
            }
        }

        // Called once we fully search along a pattern and get to it's node in the Trie
        // Creates a new Leaf if none exists, and increments its weight either way
        public bool AddLeaf(Pattern pattern) {
            bool new_leaf = false;
            if (Leaf == null) {
                Leaf = pattern;
                new_leaf = true;
            }
            //add weight to this leaf (pattern)
            Leaf.Weight++;
            return new_leaf;
        }

        public int CountLeaves() {
            if (Leaf != null) {
                return 1;
            }
            int total = 0;
            foreach (TrieNode child in GetAllChildren()) {
                total += child.CountLeaves();
            }
            return total;
        }
    }

    public class TrieLeaf
    {
        public Pattern Pattern;

        public TrieLeaf(Pattern p) {
            Pattern = p;
        }

        public void AddWeight() {
            Pattern.Weight++;
        }
    }

}


public interface IPatternSource {
    int GetValue(Vector2I index);
    void SetValue(Vector2I index, int mask);
}

public class Pattern {
    public int Rotation { get; }
    public Vector2I Origin { get; }
    public int Size { get; }
    public bool Wrap = false;
    private IPatternSource PatternSource;
    public int Weight;

    public int Index;

    public Pattern(IPatternSource _Source, Vector2I _Origin, int _Size, int _Index, int _Rotation = 0, int _Weight = 0) {
        PatternSource = _Source;
        Origin = _Origin;
        Size = _Size;
        Rotation = _Rotation;
        Weight = _Weight;
        Index = _Index;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        Vector2I pos = new();
        for (int y = 0; y < Size; y++) {
            for (int x = 0; x < Size; x++) {
                pos.X = x;
                pos.Y = y;
                hash.Add(GetValue(pos)); 
            }
        }
        return hash.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Pattern other) {
            Vector2I pos = new();
            for (int y = 0; y < Size; y++) {
                for (int x = 0; x < Size; x++) {
                    pos.X = x;
                    pos.Y = y;
                    if (GetValue(pos) != other.GetValue(pos)) {
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    public Pattern GetRotatedCopy(int amount) {
        int newRotation = (((Rotation + amount) % 4) + 4) % 4;
        return new Pattern(PatternSource, Origin, Size, Index, newRotation, Weight);
    }

    public int GetValue(Vector2I pos) {
        if (Wrap) {
            int x = ((pos.X % Size) + Size) % Size;
            int y = ((pos.Y % Size) + Size) % Size;
            pos = new Vector2I(x, y);
        } else if (pos.X < 0 || pos.Y < 0 || pos.X >= Size || pos.Y >= Size) {
            throw new IndexOutOfRangeException($"Can't access position {pos} in reference view of Size {Size}.");
        }
        return PatternSource.GetValue(GetRotatedVector(pos, Rotation) + Origin);
    }

    public void SetValue(Vector2I pos, int mask) {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= Size || pos.Y >= Size) {
            throw new IndexOutOfRangeException($"Can't access position {pos} in reference view of Size {Size}.");
        }
        PatternSource.SetValue(GetRotatedVector(pos, Rotation) + Origin, mask);
    }

    public IEnumerator<int> GetEnumerator()
	{
        Vector2I pos = new();
		for (int y = 0; y < Size; y++) {
            for (int x = 0; x < Size; x++) {
                pos.X = x;
                pos.Y = y;
                yield return GetValue(pos);
            }
        }
	}

    public int GetEntropy(Vector2I pos) {
        int mask = GetValue(pos);
		return System.Numerics.BitOperations.PopCount((uint)mask);
	}

    private Vector2I GetRotatedVector(Vector2I pos, int r) {
        Vector2I storedVector = new();
        switch (r % 4)
        {
            case 3: //->
                storedVector.X = Size - 1 - pos.Y;
                storedVector.Y = pos.X;
                break;
            case 2: // V
                storedVector.X = Size - 1 - pos.X;
                storedVector.Y = Size - 1 - pos.Y;
                break;
            case 1: // <-
                storedVector.X = pos.Y;
                storedVector.Y = Size - 1 - pos.X;
                break;
            default: // ^
                storedVector.X = pos.X;
                storedVector.Y = pos.Y;
                break;
        }
        return storedVector;
    }

    public override string ToString()
    {
        string line = "";
        Vector2I pos = new();
        for (int y = 0; y < Size; y++) {
            for (int x = 0; x < Size; x++) {
                pos.X = x;
                pos.Y = y;
                line += GetValue(pos) + "   ";
            }
            line += "\n";
        }
        return line;
    }

}


public class PatternSet
{
    private bool _unobserved = true;
    public bool IsUnobserved { get {return _unobserved;} }
    private HashSet<Pattern> internalSet = new();

    public int Count => internalSet.Count;

    public void Add(Pattern pattern) {
        internalSet.Add(pattern);
        _unobserved = false;
    }

    public void Clear() {
        internalSet.Clear();
        _unobserved = true;
    }

    public void UnionWith(PatternSet other) {
        internalSet.UnionWith(other.internalSet);
        _unobserved = false;
    }
    public void UnionWith(IEnumerable<Pattern> other) {
        internalSet.UnionWith(other);
        _unobserved = false;
    }

    public void IntersectWith(PatternSet other) {
        internalSet.IntersectWith(other.internalSet);
        _unobserved = false;
    }
    public void IntersectWith(IEnumerable<Pattern> other) {
        internalSet.IntersectWith(other);
        _unobserved = false;
    }

    public IEnumerator<Pattern> GetEnumerator() {
        foreach (Pattern p in internalSet) {
            yield return p;
        }
    }
}

public class SparsePatternSet
{
    private bool _unobserved = true;
    public bool IsUnobserved { get {return _unobserved;} }

    private Dictionary<int, ulong> chunks = new(); // Maps chunk indices to their bitmasks

    public int Count => chunks.Values.Sum(chunk => BitOperations.PopCount(chunk));

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
        foreach (var kvp in other.chunks)
        {
            if (chunks.ContainsKey(kvp.Key))
                chunks[kvp.Key] |= kvp.Value;
            else
                chunks[kvp.Key] = kvp.Value;
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
