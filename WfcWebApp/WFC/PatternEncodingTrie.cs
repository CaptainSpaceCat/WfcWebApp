using System.Diagnostics;
using System.Numerics;

namespace WfcWebApp.Wfc
{


public class PatternEncodingTrie
{
    private TrieNode root = new();

    public bool TryAddNewPattern(PatternView pattern) {
        TrieNode curr = root;
        foreach (int pixelId in pattern.Values(0)) {
            curr = curr.GetOrAddChild(pixelId);
        }
        if (curr.HasLeaf) {
            // this pattern already exists in the tree. Hooray!
            // increment its weight
            curr.Leaf.AddWeight(pattern.Rotation);

            // note that this only works because
                // A: the other 3 rotations of this pattern stored in the trie share this pattern's weights
                // B: returning false here causes the pattern-adding code to skip adding weight to the other 3 rotations
                // which avoids quadruple counting weight
                // These two combined mean the weights should always match up across rotations of the same pattern
            return false;
        }
        curr.Leaf = pattern;
        curr.Leaf.AddWeight(0);
        return true;
    }

    

    // Iterates through every pattern that would fit on top of the template with offset 1 in the specified direction (0 is up, 1 is right, ect)
    // if a match rotation is specified, will only return patterns that were sampled with the same rotation
    public IEnumerable<int> MatchingPatterns(PatternView template, int direction) {
        TrieNode? curr = root;
        foreach (int tileId in template.ValuesSkippingFirstRow(direction)) {
            curr = curr.GetChild(tileId);
            if (curr == null) {
                yield break; // if we find null on the way through this pattern, we know we have a contradiction
                //TODO throw a warning, or resolve the contradiction
                // for now ignoring it will suffice
            }
        }
        // curr should now point to a treenode containing only patterns that would match the above template
        // we can just recursively iterate down the tree
        foreach (PatternView pattern in TraversePatterns(curr)) {
            yield return pattern.GetPatternIndex(-direction);
        }
    }

    private IEnumerable<PatternView> TraversePatterns(TrieNode node) {
        if (node.Leaf != null) {
            yield return node.Leaf;
        } else {
            foreach (TrieNode child in node.GetAllChildren()) {
                foreach (var pattern in TraversePatterns(child)) {
                    yield return pattern;
                }
            }
        }
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


    public void PrintContents() {
        PrintNode(root, "");
    }

    private void PrintNode(TrieNode node, string indent) {
        if (node.HasLeaf) {
            Console.WriteLine($"{indent}Leaf → PatternIndex: {node.Leaf.GetPatternIndex(0)}, Weights: {string.Join(",", Enumerable.Range(0, 4).Select(r => node.Leaf.GetWeight(r)))}");
        }
        if (node.Children != null) {
            foreach (var kvp in node.Children) {
                Console.WriteLine($"{indent}TileID: {kvp.Key}");
                PrintNode(kvp.Value, indent + "  ");
            }
        }
    }



    private class TrieNode
    {
        public Dictionary<int, TrieNode>? Children = null;
        
        public PatternView Leaf = null!;
        public bool HasLeaf => Leaf != null;

        public int Weight { get; private set; }

        private int nextBitIndex = 0;

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
}


}




/*
    public TrieLeaf GetRandomPattern() {
        // sample a ticket index from 0 to full trie's weight
        int ticket = RandomUtils.Random.Next(root.Weight);
        
        return RandomPatternHelper(root, ticket);
    }
    private TrieLeaf RandomPatternHelper(TrieNode node, int ticket) {
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
    */