using WfcWebApp.Wfc;

namespace WfcWebApp.Utils
{

public abstract class WaveRenderer
{
    public abstract void RenderToImage(BoundedWave wave, Palette palette, SwatchImage image);
}

public class VisualWaveRenderer : WaveRenderer
{
    public override void RenderToImage(BoundedWave wave, Palette palette, SwatchImage image)
    {
        image.Clear();
        for (int y = 0; y < wave.Height; y++) {
            for (int x = 0; x < wave.Width; x++) {
                SparsePatternSet patterns = wave.AccessPatternSet(x, y);
                if (patterns.IsUnobserved) {
                    image.AddColorToPosition(y, x, Color.Gray);
                }
                foreach (int idx in patterns) {
                    PatternView pattern = palette.GetPatternFromIndex(idx);
                    for (int r = 0; r < palette.ConvSize; r++) {
                        for (int c = 0; c < palette.ConvSize; c++) {
                            Color color = palette.GetColor(c, r);
                            image.AddColorToPosition(r, c, color);
                        }
                    }
                }
            }
        }
    }

    public override void RenderToEntropy(BoundedWave wave, SwatchImage image)
    {
        image.Clear();
        for (int y = 0; y < wave.Height; y++) {
            for (int x = 0; x < wave.Width; x++) {
                SparsePatternSet patterns = wave.AccessPatternSet(x, y);
                if (patterns.IsUnobserved) {
                    image.AddColorToPosition(y, x, Color.Gray);
                    //TODO finish this
                }
            }
        }
    }
}

public class EntropyWaveRenderer : WaveRenderer
{
    public override void RenderToImage(BoundedWave wave, Palette palette, SwatchImage image)
    {
        for (int y = 0; y < wave.Height; y++) {
            for (int x = 0; x < wave.Width; x++) {
                SparsePatternSet patterns = wave.AccessPatternSet(x, y);
                if (patterns.IsUnobserved) {
                    
                }
            }
        }
    }
}

}