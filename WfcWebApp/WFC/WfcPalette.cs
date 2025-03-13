namespace WfcWebApp.Wfc
{


public class WfcPalette : IPatternSource {
    private int[,] paletteData;
    public int Width, Height;

    public bool Wrap = false;

    public readonly ColorMapping colorMapping = new();

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

    public int GetBitmask(Vector2I pos) {
        if (Wrap) {
                                     // handle negatives
            pos.X = ((pos.X % Width) + Width) % Width;
            pos.Y = ((pos.Y % Height) + Height) % Height;
        }
        return paletteData[pos.X, pos.Y];
    }

    public void SetBitmask(Vector2I pos, int mask) {
        throw new InvalidOperationException("The WFC Palette is immutable, cannot set bitmasks here.");
    }

    public ReferenceView GetReferenceView(Vector2I pos, int size, int rotation=0){
        ReferenceView view = new ReferenceView(this, pos, size);
        view.rotation = rotation;
        return view;
    }

    public ReferenceView GetReferenceView(){
        ReferenceView view = new ReferenceView(this, Vector2I.Zero, Width);
        return view;
    }
}


public class ColorMapping {
    const int MAX_UNIQUE_COLORS = 32;
    public int Count = 0;

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