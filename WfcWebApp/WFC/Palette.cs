
using System.ComponentModel;
using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{


public class Palette
{
    public bool Wrap = true;
    public bool RotationalSymmetry = true;
    public int ConvSize = 2;

    private IndexedImage PaletteImage = default!;
    private PatternEncodingTrie EncodingTrie = new();
    
    // Maps (pattern index, direction) -> matching pattern set
    private Dictionary<(int, int), SparsePatternSet> PatternSetCache = new();

    public int Width => PaletteImage.Width;
    public int Height => PaletteImage.Height;

    private List<PatternView> PatternIndexer = new();

    private int _patternCount = -1;
    public int PatternCount {
        get {
            if (_patternCount == -1) {
                _patternCount = EncodingTrie.CountUnique();
            }
            return _patternCount;
        }
    }

    public void SetImage(IndexedImage image) {
        PaletteImage = image;
    }

    public void SetParams(int convSize, bool wrap, bool rotate) {
        ConvSize = convSize;
        Wrap = wrap;
        RotationalSymmetry = rotate;
    }

    public void Preprocess() {
        EncodingTrie.Clear();
        PatternIndexer.Clear();
        int reduce = Wrap ? 0 : ConvSize-1;
        for (int y = 0; y < Height - reduce; y++) {
            for (int x = 0; x < Width - reduce; x++) {
                // make shared structures
                SharedPatternData[] sharedData = new SharedPatternData[4];

                var view = new PalettePatternView(this, (x, y), ConvSize, 0, sharedData);
                (PatternView leafPattern, bool is_new) = EncodingTrie.GetOrAddPattern(view);
                if (is_new == true) {
                    sharedData[0] = new(PatternIndexer.Count);
                    PatternIndexer.Add(leafPattern);
                }
                leafPattern.AddWeight();

                if (is_new == true) {
                    // if the 0 rotation pattern is new to the trie, we need to add the other 3 rotations
                    for (int r = 1; r < 4; r++) {
                        view = new PalettePatternView(this, (x, y), ConvSize, r, sharedData);
                        (leafPattern, is_new) = EncodingTrie.GetOrAddPattern(view);
                        if (is_new == true) {
                            // if this rotated pattern is a new symmetry, add it to the indexer
                            sharedData[r] = new(PatternIndexer.Count);
                            PatternIndexer.Add(leafPattern);
                        } else {
                            // if this pattern already existed as a symmetry
                            // copy the data reference from the original symmetry
                            sharedData[r] = sharedData[leafPattern.Rotation];
                        }
                    }
                }
            }
        }
        //EncodingTrie.PrintContents();
    }

    public IEnumerable<(int index, int weight)> AllUniqueWeightedPatterns() {
        for (int i = 0; i < PatternIndexer.Count; i++) {
            yield return (i, GetWeightFromIndex(i));
        }
    }

    public IEnumerable<int> MatchingPatterns(int patternIndex, int direction) {
        PatternView template = GetPatternFromIndex(patternIndex);
        foreach (int index in EncodingTrie.MatchingPatterns(template, direction)) {
            PatternView pattern = GetPatternFromIndex(index);
            if (!RotationalSymmetry && pattern.GetWeight() == 0) continue;
            yield return index;
        }
    }

    public SparsePatternSet MatchingPatternSet(int patternIndex, int direction) {
        if (!PatternSetCache.ContainsKey((patternIndex, direction))) {
            SparsePatternSet newSet = new();
            foreach (int index in MatchingPatterns(patternIndex, direction)) {
                newSet.Add(index);
            }
            PatternSetCache[(patternIndex, direction)] = newSet;
        }
        return PatternSetCache[(patternIndex, direction)];
    }

    public PatternView GetPatternFromIndex(int index) {
        if (index >= PatternIndexer.Count) {
            throw new IndexOutOfRangeException($"Attempt to access pattern {index} with only {PatternIndexer.Count} available.");
        }
        return PatternIndexer[index];
    }

    public int GetWeightFromIndex(int index) {
        PatternView p = GetPatternFromIndex(index);
        return RotationalSymmetry ? p.TotalWeight : p.GetWeight(0);
    }

    public int GetPixel(int x, int y) {
        (x, y) = BoundaryCheck(x, y);
        return PaletteImage.GetPixelId(x, y);
    }

    

    public Color GetColor(int pixelId) {
        return PaletteImage.GetColorFromId(pixelId);
    }

    private (int, int) BoundaryCheck(int x, int y) {
        if (Wrap) {
            x = (x % Width + Width) % Width;
            y = (y % Height + Height) % Height;
        } else if (x < 0 || x >= Width || y < 0 || y >= Height) {
            throw new IndexOutOfRangeException($"({x}, {y})");
        }
        return (x, y);
    } 

}

}