using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WfcWebApp.Wfc
{



public static class TimerUtility
{
	private static readonly Dictionary<string, Stopwatch> Timers = new();

	public static void StartTimer(string timerName)
	{
		if (!Timers.ContainsKey(timerName))
		{
			Timers[timerName] = new Stopwatch();
		}
		else
		{
			Timers[timerName].Reset();
		}

		Timers[timerName].Start();
	}

	public static void StopTimer(string timerName)
	{
		if (Timers.TryGetValue(timerName, out var stopwatch))
		{
			stopwatch.Stop();
		}
		else
		{
			throw new ArgumentException($"Timer '{timerName}' does not exist. Did you forget to start it?");
		}
	}

    public static void ContinueTimer(string timerName)
	{
		if (!Timers.ContainsKey(timerName))
		{
			throw new ArgumentException($"Timer '{timerName}' does not exist. Did you forget to start it?");
		}
		else
		{
			Timers[timerName].Start();
		}
	}

	public static TimeSpan GetElapsed(string timerName)
	{
		if (Timers.TryGetValue(timerName, out var stopwatch))
		{
			return stopwatch.Elapsed;
		}
		else
		{
			throw new ArgumentException($"Timer '{timerName}' does not exist. Did you forget to start it?");
		}
	}

	public static void PrintElapsed(string timerName)
	{
		var elapsed = GetElapsed(timerName);
		Console.WriteLine($"Timer '{timerName}' elapsed time: {elapsed.TotalMilliseconds} ms");
	}

	public static void RemoveTimer(string timerName)
	{
		if (Timers.ContainsKey(timerName))
		{
			Timers.Remove(timerName);
		}
	}
}


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

    public static float Lerp(float a, float min, float max) {
        a = Math.Clamp(a, 0, 1);
        return min + (max-min)*a;
    }
    public static float InverseLerp(float a, float min, float max) {
        a = Math.Clamp(a, min, max);
        return (a - min) / (max - min);
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

    public static Vector2I operator %(Vector2I a, Vector2I b) {
        return new Vector2I(a.X % b.X, a.Y % b.Y);
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

public struct ColorRGBA
{
    public byte R, G, B, A;

    public ColorRGBA(byte r, byte g, byte b, byte a) {
        R=r;
        G=g;
        B=b;
        A=a;
    }

    public static ColorRGBA Lerp(ColorRGBA a, ColorRGBA b, float t)
	{
		return new ColorRGBA(
			(byte)(a.R + (b.R - a.R) * t),
			(byte)(a.G + (b.G - a.G) * t),
			(byte)(a.B + (b.B - a.B) * t),
			(byte)(a.A + (b.A - a.A) * t)
		);
	}

    public override string ToString()
    {
        return $"Color({R}, {G}, {B}, {A})";
    }
}

public class ColorGradient
{


	private readonly List<(float t, ColorRGBA color)> stops = new();

	public void Add(ColorRGBA color, float t)
	{
		t = Math.Clamp(t, 0f, 1f);

		// Keep stops sorted by t
		int index = stops.FindIndex(stop => t < stop.t);
		if (index >= 0)
			stops.Insert(index, (t, color));
		else
			stops.Add((t, color));
	}

	public void Clear()
	{
		stops.Clear();
	}

	public ColorRGBA Sample(float t)
	{
		if (stops.Count == 0)
			throw new InvalidOperationException("Gradient is empty.");

		t = Math.Clamp(t, 0f, 1f);

		// Handle edge cases
		if (t <= stops[0].t) return stops[0].color;
		if (t >= stops[^1].t) return stops[^1].color;

		// Binary search for the two surrounding stops
		for (int i = 0; i < stops.Count - 1; i++)
		{
			var (t0, c0) = stops[i];
			var (t1, c1) = stops[i + 1];

			if (t >= t0 && t <= t1)
			{
				float alpha = (t - t0) / (t1 - t0);
				return ColorRGBA.Lerp(c0, c1, alpha);
			}
		}

		// Shouldn't happen if above logic is correct
		return stops[^1].color;
	}

    public static readonly ColorGradient Rainbow = CreateRainbow();

    private static ColorGradient CreateRainbow()
    {
        var gradient = new ColorGradient();
        gradient.Add(new ColorRGBA(255, 0, 0, 255), 0.0f);
        gradient.Add(new ColorRGBA(255, 127, 0, 255), 0.17f);
        gradient.Add(new ColorRGBA(255, 255, 0, 255), 0.33f);
        gradient.Add(new ColorRGBA(0, 255, 0, 255), 0.5f);
        gradient.Add(new ColorRGBA(0, 0, 255, 255), 0.66f);
        gradient.Add(new ColorRGBA(75, 0, 130, 255), 0.83f);
        gradient.Add(new ColorRGBA(143, 0, 255, 255), 1.0f);
        return gradient;
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


public class Swatch
{
    public int Width, Height;

    private int[,,] colorData;
    private int[,] weights;
    public readonly bool Wrap;

    public Swatch(int w, int h, bool wrap = false) {
        Width = w;
        Height = h;
        colorData = new int[w,h,4];
        weights = new int[w,h];
        Wrap = wrap;
    }

    public ColorRGBA GetColorAt(int x, int y) {
        if (weights[x,y] == 0) {
            return new ColorRGBA();
        }
        byte R = (byte)(colorData[x,y,0]/weights[x,y]);
        byte G = (byte)(colorData[x,y,1]/weights[x,y]);
        byte B = (byte)(colorData[x,y,2]/weights[x,y]);
        byte A = (byte)(colorData[x,y,3]/weights[x,y]);
        return new ColorRGBA(R, G, B, A);
    }

    public void PaintPattern(Pattern pattern, Vector2I pos, ColorMapping colorMap) {
        for (int x = 0; x < pattern.Size; x++) {
            for (int y = 0; y < pattern.Size; y++) {
                int mask = pattern.GetValue(new Vector2I(x, y));
                ColorRGBA color = colorMap.MaskToColor(mask);
                Vector2I offset = new Vector2I(x + pos.X, y + pos.Y);
                if (offset.X < 0 || offset.Y < 0 || offset.X >= Width || offset.Y >= Height) {
                    if (!Wrap) {
                        //skip painting it if it's gonna be off-screen
                        continue;
                    }
                    offset.X = ((offset.X % Width) + Width) % Width;
                    offset.Y = ((offset.Y % Height) + Height) % Height;
                }
                colorData[offset.X, offset.Y, 0] += color.R;
                colorData[offset.X, offset.Y, 1] += color.G;
                colorData[offset.X, offset.Y, 2] += color.B;
                colorData[offset.X, offset.Y, 3] += color.A;
                weights[offset.X, offset.Y]++;
            }
        }
    }

    public ImageDataRaw OutputToImage() {
        ImageDataRaw output = new(Width, Height);
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                output.SetPixel(x, y, GetColorAt(x, y));
            }
        }
        return output;
    }
}

}