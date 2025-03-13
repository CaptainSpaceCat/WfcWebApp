using System.Drawing;
using System.Runtime.InteropServices;

namespace WfcWebApp.Wfc
{

public static class MathUtils
{
    public static IEnumerable<Vector2I> GridEnumerator(int w, int h) {
        Vector2I pos = new();
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                pos.X=x;
                pos.Y=y;
                yield return pos;
            }
        }
    }

    public static bool IsPointInCircle(Vector2I point, Vector2I center, float radius)
	{
		return (point - center).LengthSquared() <= radius * radius;
	}
}


public class Vector2I
{
    public int X { get; set; }
    public int Y { get; set; }

    public static Vector2I Zero = new();
    public static Vector2I One = new(1,1);

    public Vector2I()
    {
        X = Y = 0;
    }

    public Vector2I(int x, int y)
    {
        X = x;
        Y = y;
    }

    public float LengthSquared()
    {
        return X*X + Y*Y;
    }

    public static Vector2I operator +(Vector2I a, Vector2I b)
    {
        return new Vector2I(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2I operator *(Vector2I v, int scalar)
    {
        return new Vector2I(v.X * scalar, v.Y * scalar);
    }

    public static Vector2I operator -(Vector2I a, Vector2I b)
    {
        return new Vector2I(a.X - b.X, a.Y - b.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2I other && X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Vector2I a, Vector2I b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Vector2I a, Vector2I b)
    {
        return !a.Equals(b);
    }


}

public struct ColorRGBA
{
    public byte R, G, B, A;

    public ColorRGBA(byte r, byte g, byte b, byte a) {
        R=r;
        G=g;
        B=b;
        A=a;
    }
}

public class ImageDataRaw
{
    public int Width { get; set; }
    public int Height { get; set; }
    // PixelData is a 1d array of length Width * Height * 4
    public List<byte> PixelData { get; set; } = new();

    public ImageDataRaw() {}

    public ImageDataRaw(int w, int h) {
        Width = w;
        Height = h;
        PixelData.Clear();
        for (int i = 0; i < Width * Height * 4; i++) {
            PixelData.Add((byte)255);
        }
    }

    public ColorRGBA GetPixel(int x, int y)
    {
        int i = (y * Width + x) * 4;
        return new ColorRGBA(PixelData[i], PixelData[i+1], PixelData[i+2], PixelData[i+3]);
    }

    public void SetPixel(int x, int y, ColorRGBA color)
    {
        int i = (y * Width + x) * 4;
        PixelData[i] = color.R;
        PixelData[i + 1] = color.G;
        PixelData[i + 2] = color.B;
        PixelData[i + 3] = color.A;
    }

    public IEnumerable<ColorRGBA> GetAllColors()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                yield return GetPixel(x, y);
            }
        }
    }
}

public interface IPatternSource {
    int GetBitmask(Vector2I index);
    void SetBitmask(Vector2I index, int mask);
}

public class ReferenceView {
    public int rotation = 0;
    public Vector2I origin;
    public int size;
    private IPatternSource source;
    //private Vector2I storedVector = new(); //re-used to avoid re-declaring all the time

    public ReferenceView(IPatternSource _source, Vector2I _origin, int _size) {
        source = _source;
        origin = _origin;
        size = _size;
    }

	public int GetBitmask(Vector2I pos) {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= size || pos.Y >= size) {
            throw new IndexOutOfRangeException($"Can't access position {pos} in reference view of size {size}.");
        }
        return source.GetBitmask(GetRotatedVector(pos, rotation) + origin);
    }

    public void SetBitmask(Vector2I pos, int mask) {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= size || pos.Y >= size) {
            throw new IndexOutOfRangeException($"Can't access position {pos} in reference view of size {size}.");
        }
        source.SetBitmask(GetRotatedVector(pos, rotation) + origin, mask);
    }

    public int GetEntropy(Vector2I pos) {
        int mask = GetBitmask(pos);
		return System.Numerics.BitOperations.PopCount((uint)mask);
	}

    private Vector2I GetRotatedVector(Vector2I pos, int r) {
        Vector2I storedVector = new();
        switch (r % 4)
        {
            case 3: //->
                storedVector.X = size - 1 - pos.Y;
                storedVector.Y = pos.X;
                break;
            case 2: // V
                storedVector.X = size - 1 - pos.X;
                storedVector.Y = size - 1 - pos.Y;
                break;
            case 1: // <-
                storedVector.X = pos.Y;
                storedVector.Y = size - 1 - pos.X;
                break;
            default: // ^
                storedVector.X = pos.X;
                storedVector.Y = pos.Y;
                break;
        }
        return storedVector;
    }

}

public class BitmaskWindow
{
    public int size { get; }

    private int[,] data;

    public BitmaskWindow(int _size, int init_mask = 0)
    {
        size = _size;
        data = new int[size, size];
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                data[x,y] = init_mask;
            }
        }
    }

    public int Get(Vector2I pos) {
        return data[pos.X, pos.Y];
    }

    public void AppendAND(ReferenceView view) {
        if (view.size != size) {
            throw new IndexOutOfRangeException("Size of view does not match size of window.");
        }
        Vector2I pos = new();
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                pos.X = x;
                pos.Y = y;
                data[x,y] &= view.GetBitmask(pos);
            }
        }
    }

    public void AppendOR(ReferenceView view) {
        if (view.size != size) {
            throw new IndexOutOfRangeException("Size of view does not match size of window.");
        }
        Vector2I pos = new();
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                pos.X = x;
                pos.Y = y;
                data[x,y] |= view.GetBitmask(pos);
            }
        }
    }

    public bool AnyEqual(byte mask) {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                if (data[x,y] == mask) {
                    return true;
                }
            }
        }
        return false;
    }


}

}