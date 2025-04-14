using System.Collections;
using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{


public class Wave 
{
    private Dictionary<(int, int), SparsePatternSet> WaveData = new();
    protected virtual SparsePatternSet AccessPatternSet(int x, int y) {
        // Trying to access patterns at all will generate unobserved entries in the wave
        var key = (x, y);
        if (!WaveData.TryGetValue(key, out var patternSet)) {
            patternSet = new SparsePatternSet();
            WaveData[key] = patternSet;
        }
        return patternSet;
    }

    public void Clear() {
        WaveData.Clear();
    }

    public IEnumerable<int> EnumeratePatternsAtPosition(int x, int y) {
        foreach (int patternIndex in AccessPatternSet(x, y)) {
            yield return patternIndex;
        }
    }
}

public class BoundedWave : Wave
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Resize(int w, int h) {
        Width = w;
        Height = h;
    }

    protected override SparsePatternSet AccessPatternSet(int x, int y) {
        //wrap the input around the boundary and access from there
        x = (x % Width + Width) % Width;
        y = (y % Height + Height) % Height;
        return base.AccessPatternSet(x, y);
    }
}


}