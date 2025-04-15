
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
                // make a shared weights int array[4]
                int[] sharedWeights = new int[4];
                patternIndex++; // increment the pattern index by default, it will be decremented if a new pattern isn't added

                for (int r = 0; r < 4; r++) { // for each cardinal direction
                    // make a new PalettePatternView object referencing this palette, position (x, y), rotation, and size
                    // assign the same weights array to the new palettepatternview (and the others in their loops)
                    var view = new PalettePatternView(this, new SharedIndex(patternIndex), (x, y), ConvSize, r, sharedWeights);

                    // navigate the pattern in the current direction, traversing the encoding trie, addorcreatechild
                    if (!EncodingTrie.TryAddNewPattern(view, r)) {
                        // if the pattern already existed in the trie, it's guaranteed that the other 3 rotations of it will be there too
                        // thus we can break here, out of the r = 0 to 4 inner loop, and continue from the next (x,y)
                        patternIndex--; //decrement because we did not add a new pattern
                        break;
                    } else if (r == 0) {
                        // if the pattern didn't exist in the trie before, it does now
                        // and we're guaranteed to add the other 3 rotations in the next 3 inner loops
                        
                        // add 1 to the new pattern's weight from rotation 0
                        // this will implicitly be reflected in the patterns for the other 4 rotations
                        view.AddWeight(0);
                    }

                    // successfully adding a new pattern means we must map to that pattern
                    PatternIndexer.Add(view);
                }
            }
        }
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

    

    public Color GetColor(int x, int y) {
        (x, y) = BoundaryCheck(x, y);
        return PaletteImage.GetColor(x, y);
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