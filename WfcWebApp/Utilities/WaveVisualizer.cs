using WfcWebApp.Wfc;

namespace WfcWebApp.Utils
{

public static class WaveVisualizer
{
    public static void RenderToImage(BoundedWave wave, Palette palette, SwatchImage image)
    {
        image.Clear();
        for (int y = 0; y < wave.Height; y++) {
            for (int x = 0; x < wave.Width; x++) {
                SparsePatternSet patterns = wave.AccessPatternSet(x, y);
                if (patterns.IsUnobserved) {
                    image.AddColorToPosition(x, y, Color.Gray);
                }
                foreach (int idx in patterns) {
                    PalettePatternView pattern = (PalettePatternView)palette.GetPatternFromIndex(idx);
                    for (int r = 0; r < palette.ConvSize; r++) {
                        for (int c = 0; c < palette.ConvSize; c++) {
                            (int a, int b) = wave.WrapPosition(c+x, r+y);
                            Color color = pattern.GetColor(a, b);
                            image.AddColorToPosition(a, b, color);
                        }
                    }
                }
            }
        }
    }

    public static void RenderToEntropy(Generator generator, SwatchImage image)
    {
        image.Clear();
        Dictionary<(int, int), int> savedEntropy = new();
        int maxObservedEntropy = 0;
        for (int y = 0; y < generator.Wave.Height; y++) {
            for (int x = 0; x < generator.Wave.Width; x++) {
                SparsePatternSet patterns = generator.Wave.AccessPatternSet(x, y);
                if (patterns.IsUnobserved) {
                    image.AddColorToPosition(x, y, Color.Gray);
                } else if (patterns.IsCollapsed) {
                    image.AddColorToPosition(x, y, Color.LightGray);
                } else if (patterns.IsContradiction) {
                    image.AddColorToPosition(x, y, Color.DarkGray);
                } else {
                    int entropy = generator.GetEntropy(x, y);
                    savedEntropy[(x, y)] = entropy;
                    if (entropy > maxObservedEntropy) {
                        maxObservedEntropy = entropy;
                    }
                }
            }
        }

        foreach (var kvp in savedEntropy) {
            (int x, int y) = kvp.Key;
            float percent =  kvp.Value / (float)maxObservedEntropy;
            Color rainbow = Color.Rainbow(percent);
            image.AddColorToPosition(x, y, rainbow);
        }
    }
}


}