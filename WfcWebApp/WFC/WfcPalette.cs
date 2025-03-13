namespace WfcWebApp.Wfc
{


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
            if ((mask & (1 << i)) != 0 && maskToColor.TryGetValue(mask, out ColorRGBA c)) {
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