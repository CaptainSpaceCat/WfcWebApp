using System.Drawing;
using System.Numerics;
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
                yield return new Vector2I(x, y);
            }
        }
    }

    public static bool IsPointInCircle(Vector2I point, Circle boundary)
	{
		return (point - boundary.Center).LengthSquared() <= boundary.RadiusSquared;
	}

    public static IEnumerable<int> GetActiveBitsFast(ulong bitmask)
    {
        while (bitmask != 0)
        {
            int index = BitOperations.TrailingZeroCount(bitmask); // Find index of lowest set bit
            yield return index;
            bitmask &= (bitmask - 1); // Clear the lowest set bit
        }
    }
}

public struct Circle
{
    public Vector2I Center { get; set; }
    public float Radius { get; set; }

    public readonly float RadiusSquared {
        get {
            return Radius * Radius;
        }
    }

    public Circle(Vector2I center, float radius) {
        Center = center;
        Radius = radius;
    }
}

public struct Vector2I
{
    public int X { get; set; }
    public int Y { get; set; }

    public static Vector2I Zero = new();
    public static Vector2I One = new(1,1);
    public static Vector2I Up = new(0,-1);
    public static Vector2I Down = new(0,1);
    public static Vector2I Left = new(-1,0);
    public static Vector2I Right = new(1,0);

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

    public override string ToString()
	{
		return $"({X},{Y})";
	}

    public static IEnumerable<Vector2I> Neighbors(Vector2I pos) {
        yield return pos + Vector2I.Up;
        yield return pos + Vector2I.Right;
        yield return pos + Vector2I.Down;
        yield return pos + Vector2I.Left;
    }

    public int DirectionTo(Vector2I other) {
        Vector2I diff = other - this;
        if (diff == Vector2I.Up) {
            return 0;
        }
        if (diff == Vector2I.Right) {
            return 1;
        }
        if (diff == Vector2I.Down) {
            return 2;
        }
        if (diff == Vector2I.Left) {
            return 3;
        }
        return -1;
    }
}

public struct ColorInt
{
    public int R, G, B, A;

    public ColorInt(int r, int g, int b, int a) {
        R=r;
        G=g;
        B=b;
        A=a;
    }

    public static ColorInt operator +(ColorInt a, ColorInt b) {
        return new ColorInt(a.R + b.R,a.G + b.G, a.B + b.B, a.A + b.A);
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



public class BitmaskWindow
{
    public int Size { get; }

    private int[,] data;

    public BitmaskWindow(int _Size, int init_mask = 0)
    {
        Size = _Size;
        data = new int[Size, Size];
        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                data[x,y] = init_mask;
            }
        }
    }

    public int Get(Vector2I pos) {
        return data[pos.X, pos.Y];
    }

    public void AppendAND(Pattern view) {
        if (view.Size != Size) {
            throw new IndexOutOfRangeException("Size of view does not match Size of window.");
        }
        Vector2I pos = new();
        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                pos.X = x;
                pos.Y = y;
                data[x,y] &= view.GetValue(pos);
            }
        }
    }

    public void AppendOR(Pattern view) {
        if (view.Size != Size) {
            throw new IndexOutOfRangeException("Size of view does not match Size of window.");
        }
        Vector2I pos = new();
        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                pos.X = x;
                pos.Y = y;
                data[x,y] |= view.GetValue(pos);
            }
        }
    }

    public bool AnyEqual(byte mask) {
        for (int x = 0; x < Size; x++) {
            for (int y = 0; y < Size; y++) {
                if (data[x,y] == mask) {
                    return true;
                }
            }
        }
        return false;
    }


}



public class PatternMask
{
	private ulong[] bits;

	public PatternMask(int Size)
	{
		bits = new ulong[(Size + 63) / 64]; // Round up
	}

	public void Set(int index)
	{
		bits[index / 64] |= (1UL << (index % 64));
	}

	public void Clear(int index)
	{
		bits[index / 64] &= ~(1UL << (index % 64));
	}

	public bool Get(int index)
	{
		return (bits[index / 64] & (1UL << (index % 64))) != 0;
	}

	public void And(PatternMask other)
	{
		for (int i = 0; i < bits.Length; i++)
			bits[i] &= other.bits[i];
	}

    public int GetEntropy()
    {
        // count up and return the number of 1's in the mask
        int count = 0;
        foreach (ulong value in bits)
        {
            count += BitOperations.PopCount(value);
        }
        return count;
    }

}


}