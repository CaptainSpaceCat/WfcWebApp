
using System.ComponentModel;
using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{


public class Palette
{
    public bool Wrap = true;
    public bool RotationalSymmetry = true;
    public int ConvSize = 3;

    private IndexedImage PaletteImage = default!;
    private PatternEncodingTrie EncodingTrie = new();

    public int Width => PaletteImage.Width;
    public int Height => PaletteImage.Height;

    private List<PatternView> PatternIndexer = new();

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
        int patternIndex = -1;
        int reduce = Wrap ? 0 : ConvSize-1;
        for (int y = 0; y < Height - reduce; y++) {
            for (int x = 0; x < Width - reduce; x++) {
                // make shared structures
                int[] sharedWeights = new int[4];
                // increment the pattern index by default, it will be decremented if a new pattern isn't added
                SharedIndex sharedIndex = new(++patternIndex);

                var view = new PalettePatternView(this, sharedIndex, (x, y), ConvSize, 0, sharedWeights);
                if (!EncodingTrie.TryAddNewPattern(view)) {
                        // if the pattern already existed in the trie at rotation 0,
                        // it's guaranteed that the other 3 rotations of it will be there too
                        patternIndex--; //decrement because we did not add any new patterns
                } else {
                    PatternIndexer.Add(view);
                    for (int r = 1; r < 4; r++) { // for each of the other 3 cardinal directions
                        // make a new PalettePatternView object referencing this palette, position (x, y), rotation, and size
                        // assign the same weights array to the new palettepatternview (and the others in their loops)
                        view = new PalettePatternView(this, sharedIndex, (x, y), ConvSize, r, sharedWeights);
                        EncodingTrie.TryAddNewPattern(view);
                        PatternIndexer.Add(view);
                    }
                }
            }
        }
        EncodingTrie.PrintContents();
    }

    public IEnumerable<(int index, int weight)> AllUniqueWeightedPatterns() {
        for (int i = 0; i < PatternIndexer.Count; i++) {
            yield return (i, GetWeightFromIndex(i));
        }
    }

    public IEnumerable<int> MatchingPatterns(int patternIndex, int direction) {
        PatternView template = GetPatternFromIndex(patternIndex);
        foreach (int index in EncodingTrie.MatchingPatterns(template, direction)) {
            yield return index;
        }
    }

    public PatternView GetPatternFromIndex(int index) {
        if (index >= PatternIndexer.Count) {
            throw new IndexOutOfRangeException($"Attempt to access pattern {index} with only {PatternIndexer.Count} available.");
        }
        return PatternIndexer[index];
    }

    public int GetWeightFromIndex(int index) {
        PatternView p = GetPatternFromIndex(index);
        return RotationalSymmetry ? p.TotalWeight : p.SingleWeight;
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
            throw new IndexOutOfRangeException();
        }
        return (x, y);
    } 

}

}