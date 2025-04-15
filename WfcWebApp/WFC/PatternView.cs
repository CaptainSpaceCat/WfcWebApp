using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{
public abstract class PatternView
{
    public (int, int) Origin { get; protected set; }
    public int Size { get; protected set; }
    public int Rotation { get; protected set; }

    protected SharedIndex _internalIndex;

    public int GetPatternIndex(int r) {
        return _internalIndex[Rotation + r];
    }

    protected int WrapR(int r) {
        return (r % 4 + 4) % 4;
    }

    protected readonly int[] _weights; // shared reference
    private int? _totalWeight = null;

    // Lazy evaluate the total weight, save in backing field
    // This works because the first time this gets called is after Preprocess() is done
    public int TotalWeight {
        get {
            if (_totalWeight == null) {
                int sum = 0;
                foreach (int w in _weights)
                    sum += w;
                _totalWeight = sum;
            }
            return _totalWeight.Value;
        }
    }

    public int SingleWeight {
        get { return _weights[0]; }
    }

    protected PatternView(SharedIndex index, (int, int) origin, int size, int rotation, int[] weights)
    {
        if (weights.Length != 4)
            throw new ArgumentException("Weights array must have exactly 4 elements.");

        Origin = origin;
        Size = size;
        Rotation = rotation;
        _internalIndex = index;
        _weights = weights;
    }

    // Iterates over all the values in the pattern view, from left to right, top to bottom
    // rotated in the provided direction
    public IEnumerable<int> Values(int direction = 0)
    {
        return ValuesFastInternal(direction, false);
    }

    // Used when searching the encoding trie for neighboring patterns that fit
    public IEnumerable<int> ValuesSkippingFirstRow(int direction = 0)
    {
        return ValuesFastInternal(direction, true);
    }

    private IEnumerable<int> ValuesFastInternal(int direction, bool skipFirstRow)
    {
        int s = Size;

        for (int y = skipFirstRow ? 1 : 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                MapRotation(x, y, direction, out int rx, out int ry);
                yield return Read(rx, ry);
            }
        }
    }

    private void MapRotation(int x, int y, int rotation, out int rx, out int ry)
    {
        int s = Size;
        rotation = WrapR(rotation);
        switch (rotation)
        {
            case 1:
                rx = y;
                ry = s - 1 - x;
                break;
            case 2:
                rx = s - 1 - x;
                ry = s - 1 - y;
                break;
            case 3:
                rx = s - 1 - y;
                ry = x;
                break;
            default:
                rx = x;
                ry = y;
                break;
        }
    }
    // To be implemented by subclasses
    protected abstract int Read(int x, int y);
    protected abstract void Write(int x, int y, int value);
    public abstract PatternView GetRotatedCopy(int offset);

    public void AddWeight(int r) {
        r = WrapR(r + Rotation);
        _weights[r]++;
    }
}


public class PalettePatternView : PatternView
{
    private readonly Palette _source;
    
    public PalettePatternView(Palette source, SharedIndex index, (int, int) origin, int size, int rotation, int[] weights)
        : base(index, origin, size, rotation, weights)
    {
        _source = source;
    }

    protected override int Read(int x, int y) {
        return _source.GetPixel(x + Origin.Item1, y + Origin.Item2);
    }

    protected override void Write(int x, int y, int value) {
        throw new Exception("Writing is disabled in IndexedImagePattern.");
    }

    // Gets a new patternView that shares this view's source, internal shared index, and weights.
    // The other parameters are copied directly, except for rotation which gets the offset added to it
    // This works because the shared index along with rotation are the values needed to calculate the correct mapping index
    public override PatternView GetRotatedCopy(int offset) {
        return new PalettePatternView(_source, _internalIndex, Origin, Size, WrapR(Rotation + offset), _weights);
    }

}

public class SharedIndex {
	private readonly int _index;

	public SharedIndex(int index = 0) {
		_index = index;
	}

	public int this[int r] {
		get {
			int rotation = ((r % 4) + 4) % 4;
			return _index * 4 + rotation;
		}
	}
}


}
