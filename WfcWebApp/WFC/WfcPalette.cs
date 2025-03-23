using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace WfcWebApp.Wfc
{


public class WfcPalette : IPatternSource {
    private int[,] paletteData;
    public int Width, Height;

    public int ConvSize = 3;

    public bool Wrap = true;
    public bool RotationalSymmetry = true;

    public readonly ColorMapping colorMapping = new();

    
	private PatternEncodingTrie? PatternTrie;
    private PatternEncodingTrie?[] DirectionalTries = new PatternEncodingTrie?[4];

    public IEnumerable<Pattern> EnumerateMatchingPatterns(Pattern template, int direction) {
        if (RotationalSymmetry) { //easy case, all patterns are stored in one big trie
            foreach (Pattern pattern in PatternTrie.MatchingPatterns(template, direction)) {
                //Console.WriteLine(pattern);
                yield return pattern;
            }
        } else { //slightly harder case, patterns are stored in separate tries based on sampling direction
            foreach (Pattern pattern in DirectionalTries[direction].MatchingPatterns(template, direction)) {
                yield return pattern;
            }
        }
    }

    public Vector2I CountPatterns() {
        if (RotationalSymmetry) {
            if (PatternTrie != null) {
                return new Vector2I(PatternTrie.CountUnique(), PatternTrie.CountWeight());
            }
        } else {
            if (DirectionalTries[0] != null) {
                return new Vector2I(DirectionalTries[0].CountUnique(), DirectionalTries[0].CountWeight());
            }
        }
        return Vector2I.Zero;
    }


    public WfcPalette(ImageDataRaw fromImage) {
        Width = fromImage.Width;
        Height = fromImage.Height;
        paletteData = new int[Width, Height];
        colorMapping.FromImageData(fromImage);

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                paletteData[x,y] = colorMapping.ColorToMask(fromImage.GetPixel(x, y));
            }
        }
    }

    private void ClearTries() {
        if (PatternTrie != null) {
            PatternTrie.Clear();
        }
        for (int i = 0; i < 4; i++) {
            if (DirectionalTries[i] != null) {
                DirectionalTries[i].Clear();
            }
        }
    }

    public void Preprocess() {
        ClearTries();
        if (RotationalSymmetry) {
            if (PatternTrie == null) {
                PatternTrie = new();
            }
            //initialize encoding tree
            foreach(Pattern pattern in GetAllPatterns()) {
                for (int r = 0; r < 4; r++) {
                    // the "rotated copy" isn't actually a deep copy of the underlying data,
                    // rather a copy of the reference view object with a new rotation
                    PatternTrie.AddPattern(pattern.GetRotatedCopy(r));
                }
            }
            // recursively init the weights of the nodes in the trie (at every level)
            PatternTrie.InitializeWeight();
        } else {
            for (int r = 0; r < 4; r++) {
                if (DirectionalTries[r] == null) {
                    DirectionalTries[r] = new();
                }
            }
            foreach(Pattern pattern in GetAllPatterns()) {
                for (int r = 0; r < 4; r++) {
                    Pattern rotatedPattern = pattern.GetRotatedCopy(r);
                    DirectionalTries[rotatedPattern.Rotation].AddPattern(rotatedPattern);
                }
            }
            for (int r = 0; r < 4; r++) {
                DirectionalTries[r].InitializeWeight();
            }
        }
        
        
    }

    public int GetValue(Vector2I pos) {
        if (Wrap) {
                                     // handle negatives
            pos.X = ((pos.X % Width) + Width) % Width;
            pos.Y = ((pos.Y % Height) + Height) % Height;
        }
        return paletteData[pos.X, pos.Y];
    }

    public void SetValue(Vector2I pos, int mask) {
        throw new InvalidOperationException("The WFC Palette is immutable, cannot set values here.");
    }

    public IEnumerable<Pattern> GetAllPatterns() {
        Vector2I pos = new();
        int reduce = Wrap ? 0 : ConvSize-1;
        for (int y = 0; y < Height - reduce; y++) {
            for (int x = 0; x < Width - reduce; x++) {
                pos.X = x;
                pos.Y = y;
                yield return GetPattern(pos, ConvSize); //the encoding tree handles rotations internally
            }
        }
    }

    public Pattern GetPattern(Vector2I pos, int size, int rotation=0){
        return new Pattern(this, pos, size, rotation);
    }

    public Pattern GetRandomPattern() {
        if (RotationalSymmetry) {
            return PatternTrie.GetRandomPattern();
        } else {
            return DirectionalTries[0].GetRandomPattern();
        }
    }

}


public class ColorMapping {
    const int MAX_UNIQUE_COLORS = 32;
    public int Count = 0;

    public ColorRGBA DefaultColor => MaskToColor(~0);

    private readonly Dictionary<int, ColorRGBA> maskToColor = new();
    private readonly Dictionary<ColorRGBA, int> colorToMask = new();

    // Accepts an ImageDataRaw object containing a bunch of pixels
    // Generates a bidirectional map from unique color to assigned bitmask
    // Bitmasks are assigned in order of which color shows up first in the image
    // Maximum of 32 unique color types are allowed, will return false if there are too many
    public void FromImageData(ImageDataRaw imageData) {
        colorToMask.Clear();
        maskToColor.Clear();
        foreach (ColorRGBA color in imageData.GetAllColors()) {
            if (!colorToMask.ContainsKey(color)) {
                if (Count < MAX_UNIQUE_COLORS) {
                    // if we still have room in the palette, add the color <-> mask pair
                    int mask = 1 << Count;
                    colorToMask[color] = mask;
                    maskToColor[mask] = color;
                }
                // keep counting no matter what, so we can see how many excess colors there are, if any
                Count++;
            }
        }
    }

    // after populating from image, call this function to see if the image didn't exceed the max unique colors
    public bool IsValid() {
        return Count <= MAX_UNIQUE_COLORS;
    }

    public ColorRGBA MaskToColor(int mask) {
        if (mask == 0) {
            // This mask is a contradiction!
            // Let's choose to render these as bright red.
            return new ColorRGBA(255, 100, 0, 255);
        }

        int r = 0, g = 0, b = 0, a = 0;
        int count = 0;
        for (int i = 0; i < MAX_UNIQUE_COLORS; i++) {
            int m = mask & (1 << i);
            if (m != 0 && maskToColor.TryGetValue(m, out ColorRGBA c)) {
                r += c.R;
                g += c.G;
                b += c.B;
                a += c.A;
                count++;
            }
        }
        if (count > 0) {
            return new ColorRGBA((byte)(r/count), (byte)(g/count), (byte)(b/count), (byte)(a/count));
        }
        throw new IndexOutOfRangeException($"Mask {mask} not found in palette.");
    }

    public int ColorToMask(ColorRGBA color) {
        if (colorToMask.TryGetValue(color, out int mask)) {
            return mask;
        }
        throw new IndexOutOfRangeException($"Color {color} not found in palette.");
    }

}

}