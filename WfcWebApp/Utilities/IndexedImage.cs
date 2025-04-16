using System.Text.Json.Serialization;

namespace WfcWebApp.Utils
{
public class IndexedImage
{
    // Raw format for interop, always preserved
	public int[][] PixelIdGrid { get; set; } = default!;
    public Dictionary<int, string> IdToColorRaw { get; set; } = new();

    public int Width => PixelIdGrid != null ? PixelIdGrid[0].Length : 0;
    public int Height => PixelIdGrid != null ? PixelIdGrid.Length : 0;

	// Lazy-converted map for C# use
	[JsonIgnore]
	private Dictionary<int, Color>? _idToColorCache;

	[JsonIgnore]
	public Dictionary<int, Color> IdToColor
	{
		get
		{
			_idToColorCache ??= IdToColorRaw.ToDictionary(
				p => p.Key,
				p => Color.FromHex(p.Value)
			);
			return _idToColorCache;
		}
	}

    [JsonIgnore]
    public IEnumerable<string> AllColorHexCodes => IdToColor.Values.Select(c => c.ToString());

    [JsonIgnore]
    public int Count => IdToColorRaw.Count;

	public void ResetColorCache() => _idToColorCache = null;

    public int GetPixelId(int x, int y) {
        return PixelIdGrid[y][x];
    }

    public Color GetColor(int x, int y) {
        return IdToColor[GetPixelId(x, y)];
    }

    public Color GetColorFromId(int id) {
        return IdToColor[id];
    }

	public void AddColor(Color color)
	{
        int id = Count + 1;
		IdToColorRaw[id] = color.ToString(); // store the string version
		ResetColorCache(); // in case it already existed
	}

    public static IndexedImage CreateBlank(int width, int height)
    {
        // Create a grid filled with ID = 2 (white)
        var grid = new int[height][];
        for (int y = 0; y < height; y++)
        {
            grid[y] = Enumerable.Repeat(2, width).ToArray();
        }

        var outputImage = new IndexedImage
        {
            PixelIdGrid = grid,
            IdToColorRaw = new Dictionary<int, string>()
        };

        outputImage.AddColor(new Color(0, 0, 0, 255));
        outputImage.AddColor(new Color(255, 255, 255, 255));
        outputImage.AddColor(new Color(255, 0, 0, 255));
        outputImage.AddColor(new Color(0, 255, 0, 255));
        outputImage.AddColor(new Color(0, 0, 255, 255));

        return outputImage;
    }

}

}