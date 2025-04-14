namespace WfcWebApp.Utils
{
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
}