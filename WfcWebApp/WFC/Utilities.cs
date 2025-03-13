using System.Drawing;

namespace WfcWebApp.Wfc
{

public static class MathUtils
{

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

public abstract class ReferenceView {
	public int size;
    public int rotation = 0;
    private Vector2I vec = new(); //re-used to avoid re-declaring all the time

	public int GetBitmask(Vector2I pos) {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= size || pos.Y >= size) {
            throw new IndexOutOfRangeException($"Can't access position {pos} in reference view of size {size}.");
        }
        RotateSelf(pos, rotation);
        return GetMaskInternal(vec);
    }

    protected abstract int GetMaskInternal(Vector2I pos);


    private void RotateSelf(Vector2I pos, int r) {
        switch (r % 4)
        {
            case 3: //->
                vec.X = size - 1 - pos.Y;
                vec.Y = pos.X;
                break;
            case 2: // V
                vec.X = size - 1 - pos.X;
                vec.Y = size - 1 - pos.Y;
                break;
            case 1: // <-
                vec.X = pos.Y;
                vec.Y = size - 1 - pos.X;
                break;
            default: // ^
                vec.X = pos.X;
                vec.Y = pos.Y;
                break;
        }
    }

}

}