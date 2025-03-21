using System.Diagnostics;

namespace WfcWebApp.Wfc
{


public class PatternEncodingTrie
{

    private TrieNode root = new();

    public void AddPattern(Pattern pattern) {
        TrieNode curr = root;
        foreach (int tileId in pattern) {
            curr = curr.GetOrAddChild(tileId);
        }
        curr.AddLeaf(pattern);
    }

    // Iterates through every pattern that would fit on top of the template with offset 1 in the specified direction (0 is up, 1 is right, ect)
    // if a match rotation is specified, will only return patterns that were sampled with the same rotation
    public IEnumerable<Pattern> MatchingPatterns(Pattern template, int direction) {
        Pattern rotatedTemplate = template.GetRotatedCopy(2 - direction);
        // do the search
        int i = 0;
        TrieNode? curr = root;
        foreach (int tileId in rotatedTemplate) {
            if (rotatedTemplate.Size < i++) {
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
            yield return pattern;
        }

        
    }

    private IEnumerable<Pattern> TraversePatterns(TrieNode node) {
        if (node.Leaf != null) {
            yield return node.Leaf.Pattern;
        } else {
            foreach (TrieNode child in node.GetAllChildren()) {
                foreach (Pattern pattern in TraversePatterns(child)) {
                    yield return pattern;
                }
            }
        }
    }

    public Pattern GetRandomPattern() {
        //TODO make this sample by weight instead of uniformly
        return RandomPatternHelper(root);
    }
    private Pattern RandomPatternHelper(TrieNode node) {
        if (node.Leaf != null) {
            return node.Leaf.Pattern;
        }
        var children = node.GetAllChildren().ToList();
        if (children.Count > 0) {
            return RandomPatternHelper(RandomUtils.Choice(children));
        }
        return null;
    }
    
    public void Clear() {
        //TODO delete tree manually, but for now just mark the whole thing for GC
        root = new TrieNode();
    }

    public int CountWeight() {
        return root.CountWeight();
    }

    public int CountUnique() {
        return root.CountUnique();
    }


    private class TrieNode
    {
        private Dictionary<int, TrieNode>? Children = null;
        
        public TrieLeaf? Leaf = null;


        public int CountWeight() {
            if (Leaf != null) {
                return Leaf.Pattern.Weight;
            }
            int total = 0;
            if (Children != null) {
                foreach (TrieNode child in Children.Values) {
                    total += child.CountWeight();
                }
            }
            return total;
        }
        public int CountUnique() {
            if (Leaf != null) {
                return 1;
            }
            int total = 0;
            if (Children != null) {
                foreach (TrieNode child in Children.Values) {
                    total += child.CountUnique();
                }
            }
            return total;
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
        public void AddLeaf(Pattern pattern) {
            if (Leaf == null) {
                Leaf = new(pattern);
            }
            Leaf.AddWeight();
        }
    }

    public class TrieLeaf
    {
        public Pattern Pattern;
        public int Rotation; // store the rotation the pattern ref was in when we added this leaf
        // this needs to be done because we're storing all copies of a rotated pattern as the same Pattern object
        // that is, 4 leaves will point to the same Pattern, which itself points to a slice of the palette
        // these 4 leaves will each be hashed at a different rotation of that same pattern
        // so when reading from the leaves, we must account for this by rotating the pattern to the leaf's stored rotation

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
    public int Weight = 0;

    public Pattern(IPatternSource _Source, Vector2I _Origin, int _Size, int _Rotation = 0) {
        PatternSource = _Source;
        Origin = _Origin;
        Size = _Size;
        Rotation = _Rotation;
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
        return new Pattern(PatternSource, Origin, Size, newRotation);
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
                pos.X = x + Origin.X;
                pos.Y = y + Origin.Y;
                yield return PatternSource.GetValue(pos);
            }
        }
	}

	// Non-generic version (for compatibility)
	//IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

}


}
