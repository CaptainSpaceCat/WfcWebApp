using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{
public abstract class PatternView
{
    public (int, int) Origin { get; protected set; }
    public int Size { get; protected set; }
    public int Rotation { get; protected set; }

    protected readonly SharedPatternData[] _internalData;

    public int GetIndex(int r = 0) {
        return _internalData[WrapR(Rotation + r)].Index;
    }

    public int GetWeight(int r = 0) {
        return _internalData[WrapR(r + Rotation)].Weight;
    }

    protected int WrapR(int r) {
        return (r % 4 + 4) % 4;
    }

    private int? _totalWeight = null;

    // Lazy evaluate the total weight, save in backing field
    // This works because the first time this gets called is after Preprocess() is done
    public int TotalWeight {
        get {
            if (_totalWeight == null) {
                int sum = 0;
                foreach (SharedPatternData data in _internalData)
                    sum += data.Weight;
                _totalWeight = sum;
            }
            return _totalWeight.Value;
        }
    }

    protected PatternView((int, int) origin, int size, int rotation, SharedPatternData[] data)
    {
        Origin = origin;
        Size = size;
        Rotation = rotation;
        _internalData = data;
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
                MapRotation(x, y, direction + Rotation, out int rx, out int ry);
                yield return Read(rx, ry);
            }
        }
    }

    protected void MapRotation(int x, int y, int rotation, out int rx, out int ry)
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

    public override string ToString()
    {
        string result = "\n";
        int c = 0;
        foreach (int index in Values(0)) {
            result += $"{index} ";
            if (++c == Size) {
                c = 0;
                result += "\n";
            }
        }
        result += $"Weights: [{GetWeight(0)}";
        for (int i = 1; i < 4; i++) {
            result += $", {GetWeight(i)}";
        }
        result += "]\n";
        result += $"Indexer: [{GetIndex(0)}";
        for (int i = 1; i < 4; i++) {
            result += $", {GetIndex(i)}";
        }
        result += $"]\nRotation: {Rotation}";
        return result;
    }

    public void AddWeight(int r = 0) {
        _internalData[WrapR(r + Rotation)].AddWeight();
    }
}


public class PalettePatternView : PatternView
{
    private readonly Palette _source;
    
    public PalettePatternView(Palette source, (int, int) origin, int size, int rotation, SharedPatternData[] data)
        : base(origin, size, rotation, data)
    {
        _source = source;
    }

    protected override int Read(int x, int y) {
        return _source.GetPixel(x + Origin.Item1, y + Origin.Item2);
    }

    protected override void Write(int x, int y, int value) {
        throw new Exception("Writing is disabled in IndexedImagePattern.");
    }

    public Color GetColor(int x, int y) {
        MapRotation(x, y, Rotation, out int rx, out int ry);
        int pixelId = Read(rx, ry);
        return _source.GetColor(pixelId);
    }

    public override string ToString()
    {
        return base.ToString();
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


public class SharedPatternData {

    public readonly int Index;
    public int Weight { get; protected set; }

    public SharedPatternData(int index) {
        Index = index;
    }

    public void AddWeight() {
        Weight++;
    }
}

}
